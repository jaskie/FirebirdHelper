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
        public virtual long Id { get; set; }
        public FirebirdRow()
        {
            Id = NoId;
        }

        public FirebirdRow(object table)
        {
            Table = table;
            Id = NoId;
        }

        internal bool IsDbReading = false;
        protected internal object Table;

        protected bool SetField(ref string field, string value, [CallerMemberName] string fieldName = null)
        {
            if (EqualityComparer<string>.Default.Equals(field, value)) return false;
            if (!IsDbReading)
            {
                PropertyInfo pi = this.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                ColumnAttribute columnAttribute = (ColumnAttribute)pi.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
                if (columnAttribute != null)  // database column
                {
                    if (!(pi.GetCustomAttributes(typeof(DatabaseAutoUpdatedAttribute), true).Any()
                       || pi.GetCustomAttributes(typeof(JoinFieldAttribute), true).Any())) // not database and read only
                        lock (FieldsPreviousValue)
                            if (!FieldsPreviousValue.ContainsKey(pi))
                                FieldsPreviousValue[pi] = field;
                }
                Modified = true;
                if (!string.IsNullOrWhiteSpace(value) && columnAttribute != null && columnAttribute.Length > 0 && value.Length > columnAttribute.Length)
                    field = value.Substring(0, columnAttribute.Length);
                else
                    field = value;
            }
            else
                field = value;
            NotifyOfPropertyChange(fieldName);
            return true;
        }

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
                        lock (FieldsPreviousValue)
                        {
                            if (!FieldsPreviousValue.ContainsKey(pi))
                                FieldsPreviousValue[pi] = field;
                        }
                }
                Modified = true;
            }
            field = value;
            NotifyOfPropertyChange(fieldName);
            return true;
        }

        protected internal Dictionary<PropertyInfo, object> FieldsPreviousValue = new Dictionary<PropertyInfo, object>();

        public virtual bool Save() { return Save(null); }
        public virtual bool Save(FbTransaction transaction)
        {
            TableNameAttribute tableNameAttribute = (TableNameAttribute)Table.GetType().GetCustomAttributes(typeof(TableNameAttribute), false).FirstOrDefault();
            GeneratorNameAttribute generatorNameAttribute = (GeneratorNameAttribute)Table.GetType().GetCustomAttributes(typeof(GeneratorNameAttribute), false).FirstOrDefault();
            ColumnAttribute idAttribute = (ColumnAttribute)GetType().GetProperty("Id").GetCustomAttributes(typeof(ColumnAttribute), false).First();
            bool result = false;
            lock (FieldsPreviousValue)
            {
                if (FieldsPreviousValue.Count() > 0)
                {
                    PropertyInfo lastField = FieldsPreviousValue.Last().Key;
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
                                cb.Append("insert into \"").Append(tableNameAttribute.Name).Append("\" (").Append(idAttribute.Name).Append(", ");
                                id = Connector.GenNextGenValue(generatorNameAttribute.Name, transaction);
                                foreach (PropertyInfo field in FieldsPreviousValue.Keys)
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
                            foreach (PropertyInfo field in FieldsPreviousValue.Keys)
                            {
                                ColumnAttribute an = (ColumnAttribute)(field.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault());
                                string fieldName = (an == null) ? field.Name.ToUpperInvariant() : an.Name;
                                cb.Append(fieldName).Append("=@").Append(fieldName);
                                if (field != lastField)
                                    cb.Append(", ");
                                else
                                    cb.Append(" ");
                            }
                            cb.Append("where ").Append(idAttribute.Name).Append("=@ID");
                        }
                        var command = new FbCommand(cb.ToString(), Connector.Connection);
                        command.Transaction = transaction;
                        command.Parameters.Add("@ID", id);
                        PropertyInfo[] fields = this.GetType().GetProperties();
                        foreach (var field in FieldsPreviousValue)
                        {
                            ColumnAttribute an = (ColumnAttribute)(field.Key.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault());
                            string fieldName = (an == null) ? field.Key.Name.ToUpperInvariant() : an.Name;
                            command.Parameters.Add("@" + fieldName, field.Key.GetValue(this, null)??DBNull.Value);
                        }
                        command.ExecuteNonQuery();
                        FieldsPreviousValue.Clear();
                        RefreshAutoUpdatedFields(transaction, inserting);
                        result = true;
                    }
                    else
                        throw new ApplicationException("No table name provided");
                }
                Modified = false;
            }
            return result;
        }

        public virtual void Cancel()
        {
            lock (FieldsPreviousValue)
            {
                foreach (var field in FieldsPreviousValue)
                    field.Key.SetValue(this, field.Value, null);
                FieldsPreviousValue.Clear();
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
            ColumnAttribute idAttribute = (ColumnAttribute)GetType().GetProperty("Id").GetCustomAttributes(typeof(ColumnAttribute), false).First();
            if (tableNameAttribute != null && !string.IsNullOrWhiteSpace(tableNameAttribute.Name) 
                && !string.IsNullOrWhiteSpace(idAttribute.Name))
            {
                var command = new FbCommand(string.Format("delete from \"{0}\" where \"{1}\"={2}", tableNameAttribute.Name, idAttribute.Name, Id), Connector.Connection);
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            else
                throw new ApplicationException("No table name provided to delete record");

        }

        public bool IsNew { get { return Id == NoId; } }

        protected void RefreshAutoUpdatedFields(FbTransaction transaction, bool inserting)
        {
            var method = Table.GetType().BaseType.GetMethod("_refreshAutoUpdatedFields", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(Table, new object[] {this, transaction, !inserting, inserting});
        }

        public void Refresh(FbTransaction transaction)
        {
            var method = Table.GetType().BaseType.GetMethod("RefreshRow", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(Table, new object[] { this, transaction });
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
