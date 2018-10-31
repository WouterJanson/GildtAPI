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
using System.Net.Http;
using System.Collections.Specialized;
using System.Net;
using System.Linq;

namespace Company.Function
{
    public static class Users
    {
        [FunctionName("Users")]
        public static async Task<IActionResult> GetUsers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Users/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            List<User> users = new List<User>();
            
            string qCount = req.Query["count"];
            //Default count number - Used to select number of rows
            if(qCount == null)
            {
                qCount = "20";
            }
            
            var sqlStr = 
                $"SELECT TOP {qCount} Users.Id as UserId, Users.IsAdmin as IsAdmin, Users.Username, Users.Email, Users.Password, Coupons.Id " +
                "as CouponId, Coupons.Name, Coupons.Description, Coupons.StartDate, Coupons.EndDate, Coupons.Type, Coupons.TotalUsed, Coupons.Image " +
                "FROM Users INNER JOIN UsersCoupons ON Users.Id = UsersCoupons.UserId " +
                "INNER JOIN Coupons ON UsersCoupons.CouponId = Coupons.Id ";
            var sqlStr2 = 
                $"SELECT u.Id as UserId, u.IsAdmin, u.Username, u.Email, u.Password, c.Id AS CouponId, c.Name, c.Description, c.StartDate, c.EndDate, c.Type, c.TotalUsed, c.Image FROM Users AS u LEFT JOIN UsersCoupons ON u.Id = UsersCoupons.UserId LEFT JOIN Coupons AS c ON UsersCoupons.CouponId = c.Id";
            var sqlWhere = $"WHERE UserId = {id}";
            
            // Checks if an id parameter is filled in
            if(id != null)
            {
                // sqlStr = sqlStr + sqlWhere;
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

            return users.Count >= 1
                ? (ActionResult)new OkObjectResult(j)
                : new NotFoundObjectResult("No users where found");
        }


        [FunctionName("DeleteUser")]
        public static async Task<IActionResult> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Users/Delete/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE FROM Users WHERE Users.Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.ExecuteNonQuery();
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the user");
                }
            }
            catch (InvalidCastException e)
            {
                return (ActionResult)new BadRequestObjectResult(e);
            }
        }


        [FunctionName("RegisterUser")]
        public static async Task<HttpResponseMessage> RegisterUser(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Users/Register")] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string username = formData["username"];
            string email = formData["email"];
            string password = formData["password"];

            // Queries
            var sqlStr =
            $"INSERT INTO Users (IsAdmin, Username, Email, Password) VALUES ('false', '{username}', '{email}', '{password}')";
            var sqlGet =
            $"SELECT COUNT(*) FROM Users WHERE (Username = '{username}' OR Email = '{email}')";

            //Checks if the input fields are filled in
            if (username == null)
            {
                missingFields.Add("Username");
            }
            if (email == null)
            {
                missingFields.Add("Email");
            }
            if (password == null)
            {
                missingFields.Add("Password");
            }

            // Returns bad request if one of the input fields are not filled in
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //Checks if the username or email is already registered
            SqlCommand checkAccount = new SqlCommand(sqlGet, conn);
            checkAccount.Parameters.AddWithValue("Username", username);
            int UserExist = (int)checkAccount.ExecuteScalar();
            if (UserExist > 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "There is already an account registered with the username or email");
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Close the database connection
                DBConnect.Dispose(conn);
                return req.CreateResponse(HttpStatusCode.OK, "Successfully registered the user");
            }
        }
    }
}
