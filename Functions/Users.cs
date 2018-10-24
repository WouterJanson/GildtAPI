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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Users/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            List<User> users = new List<User>();
            string qCount = req.Query["count"];
            if(qCount == null)
            {
                qCount = "20";
            }

            log.LogInformation("Test" + id);

            var sqlStr = 
                $"SELECT TOP {qCount} Users.Id as UserId, Users.IsAdmin as IsAdmin, Users.Username, Users.Email, Users.Password, Coupons.Id " +
                "as CouponId, Coupons.Name, Coupons.Description, Coupons.StartDate, Coupons.EndDate, Coupons.Type, Coupons.TotalUsed, Coupons.Image " +
                "FROM Users INNER JOIN UsersCoupons ON Users.Id = UsersCoupons.UserId " +
                "INNER JOIN Coupons ON UsersCoupons.CouponId = Coupons.Id ";
            var sqlWhere = $"WHERE UserId = {id}";
            
            // Checks if the id parameter is filled in
            if(id != null)
            {
                sqlStr = sqlStr + sqlWhere;
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    List<Coupon> couponsList = new List<Coupon>();

                    // Adds the coupon to the couponlist
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

                    // Checks if list users is empty
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
                        // Search the user in the list and adds the coupon to it
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

            // Close the database connection
            DBConnect.Dispose(conn);
            
            string j = JsonConvert.SerializeObject(users);

            return users != null
                ? (ActionResult)new OkObjectResult(j)
                : new BadRequestObjectResult("No users where found");
        }
    }
}
