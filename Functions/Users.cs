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
            string qCount = req.Query["count"];
            if(qCount == null)
            {
                qCount = "20";
            }

            var sqlStr = 
                $"SELECT TOP {qCount} Users.Id as UserId, Users.IsAdmin as IsAdmin, Users.Username, Users.Email, Users.Password, Coupons.Id " +
                "as CouponId, Coupons.Name, Coupons.Description, Coupons.StartDate, Coupons.EndDate, Coupons.Type, Coupons.TotalUsed, Coupons.Image " +
                "FROM Users INNER JOIN UsersCoupons ON Users.Id = UsersCoupons.UserId";

            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();
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

                    if(users.Count == 0)
                    {
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
                    else
                    {
                        if(users.Exists(x => x.userId == Convert.ToInt32(reader["UserId"])))
                        {
                            foreach(User user in users.ToArray())
                            {
                                if(user.userId == Convert.ToInt32(reader["UserId"]))
                                {
                                    user.coupons.Add(couponsList[couponsList.Count - 1]);
                                }
                            }
                        }
                        else
                        {
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
}
