using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;

public class DBConnect {
    static string connStr = "Server=tcp:gildt.database.windows.net,1433;Initial Catalog=GildtAPI;Persist Security Info=False;User ID=ServerAdmin;Password=Krijgdetering123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

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