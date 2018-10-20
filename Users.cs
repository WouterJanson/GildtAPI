using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Company.Function
{
    public static class Users
    {
        static string connStr = "Server=tcp:gildt.database.windows.net,1433;Initial Catalog=GildtAPI;Persist Security Info=False;User ID=ServerAdmin;Password=Krijgdetering123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        [FunctionName("Users")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                var sqlStr = "SELECT * FROM Users";
                using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var tes1 = reader["Username"].ToString();
                        return (ActionResult)new OkObjectResult(tes1);

                    }
                    reader.Close();
                    return (ActionResult)new OkObjectResult("done");
                    
                }
            }

            // log.LogInformation("C# HTTP trigger function processed a request.");

            // string name = req.Query["name"];

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;
            // User u = CreateTestUser();
            // string j = JsonConvert.SerializeObject(u);
            // return name != null
            //     ? (ActionResult)new OkObjectResult(j)
            //     : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
        public static User CreateTestUser(){
            return new User(){name = "TestUser", email = "Test@email.com", password="TestPass", coupons = new int[]{0, 1}};
        }

    }


    public class User{
        public string name;
        public string email;
        public string password;
        public int[] coupons;
    }
}
