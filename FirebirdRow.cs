using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Puch.FirebirdHelper
{
    public abstract class FirebirdRow : INotifyPropertyChanged
    {
        protected const long NoId = -1;
        [Column]
        public long Id { get; set; }
        public FirebirdRow()
        {
            Id = NoId;
        }

        internal bool IsDbReading = false;
        internal object Table;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string fieldName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            if (!IsDbReading)
            {
                PropertyInfo pi = this.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi.GetCustomAttributes(typeof(ColumnAttribute), true).Any())  // database column
                {
                    if (!(pi.GetCustomAttributes(typeof(DatabaseAutoUpdatedAttribute), true).Any()
                       || pi.GetCustomAttributes(typeof(JoinFieldAttribute), true).Any())) // not database and read only
                        lock (PreviousFieldValues)
                        {
                            if (!PreviousFieldValues.ContainsKey(pi))
                                PreviousFieldValues[pi] = field;
                        }
                }
                Modified = true;
            }
            field = value;
            NotifyOfPropertyChange(fieldName);
            return true;
        }

        protected internal Dictionary<PropertyInfo, object> PreviousFieldValues = new Dictionary<PropertyInfo, object>();

        public virtual void Save() { Save(null); }
        public virtual void Save(FbTransaction transaction)
        {
            TableNameAttribute tableNameAttribute = (TableNameAttribute)Table.GetType().GetCustomAttributes(typeof(TableNameAttribute), false).FirstOrDefault();
            GeneratorNameAttribute generatorNameAttribute = (GeneratorNameAttribute)Table.GetType().GetCustomAttributes(typeof(GeneratorNameAttribute), false).FirstOrDefault();
            lock (PreviousFieldValues)
            {
                if (PreviousFieldValues.Count() > 0)
                {
                    PropertyInfo lastField = PreviousFieldValues.Last().Key;
                    if (tableNameAttribute != null && !string.IsNullOrWhiteSpace(tableNameAttribute.Name))
                    {
                        StringBuilder cb = new StringBuilder();
                        bool inserting = Id == NoId;
                        long id = Id;
                        if (inserting)
                        {
                            if (generatorNameAttribute != null && !string.IsNullOrWhiteSpace(generatorNameAttribute.Name))
                            {
                                var vb = new StringBuilder(" (@ID, ");
                                cb.Append("insert into \"").Append(tableNameAttribute.Name).Append("\" (ID, ");
                                id = Connector.GenNextGenValue(generatorNameAttribute.Name);
                                foreach (PropertyInfo field in PreviousFieldValues.Keys)
                                {
                                    ColumnAttribute an = (ColumnAttribute)(field.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault());
                                    string fieldName = (an == null) ? field.Name.ToUpperInvariant() : an.Name;
                                    cb.Append(fieldName);
                                    vb.Append("@").Append(fieldName);
                                    if (field != lastField)
                                    {
                                        cb.Append(", ");
                                        vb.Append(", ");
                                    }
                                    else
                                    {
                                        cb.Append(")");
                                        vb.Append(")");
                                    }
                                }
                                cb.Append(" values").Append(vb);
                            }
                            else
                                throw new ApplicationException("No generator name provided to insert data");
                            Id = id;
                        }
                        else
                        {
                            cb.Append("update \"").Append(tableNameAttribute.Name).Append("\" set ");
                            foreach (PropertyInfo field in PreviousFieldValues.Keys)
                            {
                                ColumnAttribute an = (ColumnAttribute)(field.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault());
                                string fieldName = (an == null) ? field.Name.ToUpperInvariant() : an.Name;
                                cb.Append(fieldName).Append("=@").Append(fieldName);
                                if (field != lastField)
                                    cb.Append(", ");
                                else
                                    cb.Append(" ");
                            }
                            cb.Append("where ID=@ID");
                        }
                        var command = new FbCommand(cb.ToString(), Connector.Connection);
                        command.Transaction = transaction;
                        command.Parameters.Add("@ID", id);
                        PropertyInfo[] fields = this.GetType().GetProperties();
                        foreach (var field in PreviousFieldValues)
                        {
                            ColumnAttribute an = (ColumnAttribute)(field.Key.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault());
                            string fieldName = (an == null) ? field.Key.Name.ToUpperInvariant() : an.Name;
                            command.Parameters.Add("@" + fieldName, field.Key.GetValue(this, null));
                        }
                        command.ExecuteNonQuery();
                        PreviousFieldValues.Clear();
                        RefreshAutoUpdatedFields(transaction, inserting);
                    }
                    else
                        throw new ApplicationException("No table name provided");
                }
                Modified = false;
            }
        }

        public virtual void Cancel()
        {
            lock (PreviousFieldValues)
            {
                foreach (var field in PreviousFieldValues)
                    field.Key.SetValue(this, field.Value, null);
                PreviousFieldValues.Clear();
            }
            Modified = false;
        }

        public virtual void Delete()
        {
            Delete(null);
        }

        public virtual void Delete(FbTransaction transaction)
        {
            if (IsNew)
                return;
            TableNameAttribute tableNameAttribute = (TableNameAttribute)Table.GetType().GetCustomAttributes(typeof(TableNameAttribute), false).FirstOrDefault();
            if (tableNameAttribute != null && !string.IsNullOrWhiteSpace(tableNameAttribute.Name))
            {
                var command = new FbCommand(string.Format("delete from \"{0}\" where ID={1}", tableNameAttribute.Name, Id), Connector.Connection);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            else
                throw new ApplicationException("No table name provioded to delete record");

        }

        public bool IsNew { get { return Id == NoId; } }

        protected void RefreshAutoUpdatedFields(FbTransaction transaction, bool inserting)
        {
            var method = Table.GetType().BaseType.GetMethod("_refreshAutoUpdatedFields", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(Table, new object[] {this, transaction, !inserting, inserting});
        }

        private bool _modified = false;
        public bool Modified { get { return _modified; }
            set
            {
                if (_modified != value)
                {
                    _modified = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
