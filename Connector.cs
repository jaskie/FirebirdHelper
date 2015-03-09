using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Diagnostics;

namespace Puch.FirebirdHelper
{
    public class Connector
    {
        internal static FbConnection Connection = new FbConnection();
        public static FbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        public static bool Connect(string connectionString)
        {
            try
            {
                Connection.ConnectionString = connectionString;
                Connection.Open();
                Connection.InfoMessage += Connection_InfoMessage;
            }
            catch (Exception e)
            { 
                Debug.WriteLine(e);
                return false;
            }
            return true;
        }

        static void Connection_InfoMessage(object sender, FbInfoMessageEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        public static void Execute(string statement)
        {
            Execute(statement, null);
        }

        public static void Execute(string statement, FbTransaction transaction)
        {
            FbCommand c;
            if (transaction == null)
                c = new FbCommand(statement, Connection, transaction);
            else
                c = new FbCommand(statement, Connection);
                c.ExecuteNonQuery();
        }

        public static object ExecuteScalar(string statement)
        {
            return ExecuteScalar(statement, null);
        }

        public static object ExecuteScalar(string statement, FbTransaction transaction)
        {
            FbCommand c;
            if (transaction == null)
                c = new FbCommand(statement, Connection, transaction);
            else
                c = new FbCommand(statement, Connection);
            return c.ExecuteScalar();
        }

        internal static long GenNextGenValue(string generatorName)
        {
            FbCommand c = new FbCommand(string.Format("select gen_id({0}, 1) from rdb$database;", generatorName), Connection);
            return (long)c.ExecuteScalar();
        }

    }
}
