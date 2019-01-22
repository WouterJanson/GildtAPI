using System.Data.SqlClient;
namespace GildtAPI
{

    public class DBConnect {
        static string connStr = "Server=tcp:gildt.database.windows.net,1433;" +
            "Initial Catalog=GildtAPI;" +
            "Persist Security Info=False;" +
            $"User ID={Constants.DBUSERNAME};" +
            $"Password={Constants.DBPASSWORD};" +
            "MultipleActiveResultSets=False;" +
            "Encrypt=True;" +
            "TrustServerCertificate=False;" +
            "Connection Timeout=30;";

        public static SqlConnection GetConnection() {
            SqlConnection conn = new SqlConnection(connStr);
            conn.Open();
            return conn;
        }

        public static void Dispose(SqlConnection conn) {
            if(conn.State == System.Data.ConnectionState.Open)
            {
                conn.Close();
            }
            conn.Dispose();
        }
    }

}