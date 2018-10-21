using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Company.Function
{
    public static class Users
    {
        [FunctionName("Users")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<User> users = new List<User>();
            var sqlStr = 
                "SELECT Users.Id as UserId, Users.IsAdmin, Users.Username, Users.Email, Users.Password, Coupons.Id as CouponId, Coupons.Name, Coupons.Description, Coupons.StartDate, Coupons.EndDate, Coupons.Type, Coupons.TotalUsed, Coupons.Image FROM Users INNER JOIN UsersCoupons ON Users.Id = UsersCoupons.UserId INNER JOIN Coupons ON UsersCoupons.CouponId = Coupons.Id";
            var sqlUser = "SELECT Users.Id as UserId, Users.IsAdmin, Users.Username, Users.Email, Users.Password FROM Users";


            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();
                DataTable schemaTable = reader.GetSchemaTable();
                while (reader.Read())
                {
                    List<Coupon> couponsList = new List<Coupon>();

                    couponsList.Add(
                        new Coupon() {
                            couponId = Convert.ToInt32(reader["CouponId"]),
                            name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            startDate = DateTime.Parse(reader["StartDate"].ToString()),
                            endDate = DateTime.Parse(reader["EndDate"].ToString()),
                            type = Convert.ToInt32(reader["Type"].ToString()),
                            totalUsed = Convert.ToInt32(reader["TotalUsed"]),
                            image = reader["Image"].ToString()
                        }
                    );
                    
                    users.Add(
                        new User(){
                            userId = Convert.ToInt32(reader["UserId"]),
                            username = reader["Username"].ToString(), 
                            email = reader["Email"].ToString(), 
                            password = reader["Password"].ToString(),
                            coupons = couponsList
                        }
                    );
                }

            }
            DBConnect.Dispose(conn);
            
            string j = JsonConvert.SerializeObject(users);

            return users != null
                ? (ActionResult)new OkObjectResult(j)
                : new BadRequestObjectResult("No users where found");

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
        // public static User CreateTestUser(){
        //     return new User(){name = "TestUser", email = "Test@email.com", password="TestPass", coupons = new int[]{0, 1}};
        // }

    }


    public class User{
        public int userId;
        public bool IsAdmin;
        public string username;
        public string email;
        public string password;
        public List<Coupon> coupons;
    }

    public class Coupon {
        public int couponId;
        public string name;
        public string Description;
        public DateTime startDate;
        public DateTime endDate;
        public int type;
        public int totalUsed;
        public string image;
    }
}
