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
using GildtAPI.Model;

namespace GildtAPI.Functions
{
    public static class Users
    {
        [FunctionName("Users")]
        public static async Task<IActionResult> GetUsers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Users/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            List<User> users = new List<User>();
            List<UsersCoupon> couponsList = new List<UsersCoupon>();

            string qCount = req.Query["count"];
            //Default count number - Used to select number of rows
            if(qCount == null)
            {
                qCount = "20";
            }
            
            var sqlStrUsers = $"SELECT TOP {qCount}* FROM Users ";
            var sqlStrCoupons = "SELECT * FROM UsersCoupons INNER JOIN Coupons ON UsersCoupons.CouponId = Coupons.Id";
            var sqlWhere = $"WHERE Id = {id}";
            
            // Checks if an id parameter is filled in
            if(id != null)
            {
                sqlStrUsers = sqlStrUsers + sqlWhere;
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmdCoupons = new SqlCommand(sqlStrCoupons, conn))
            {
                SqlDataReader readerCoupons = await cmdCoupons.ExecuteReaderAsync();
                while(readerCoupons.Read())
                {
                    couponsList.Add(
                        new UsersCoupon() {
                            CouponId = Convert.ToInt32(readerCoupons["CouponId"]),
                            Name = readerCoupons["Name"].ToString(),
                            Description = readerCoupons["Description"].ToString(),
                            StartDate = DateTime.Parse(readerCoupons["StartDate"].ToString()),
                            EndDate = DateTime.Parse(readerCoupons["EndDate"].ToString()),
                            Type = Convert.ToInt32(readerCoupons["Type"].ToString()),
                            TotalUsed = Convert.ToInt32(readerCoupons["TotalUsed"]),
                            Image = readerCoupons["Image"].ToString(),
                            UserId = Convert.ToInt32(readerCoupons["UserId"])
                        }
                    );
                }
                readerCoupons.Close();
            }

            using (SqlCommand cmdUsers = new SqlCommand(sqlStrUsers, conn))
            {
                SqlDataReader readerUsers = await cmdUsers.ExecuteReaderAsync();
                while(readerUsers.Read())
                {
                    List<UsersCoupon> tempList = new List<UsersCoupon>();
                    foreach(UsersCoupon coupons in couponsList)
                    {
                        if(coupons.UserId == Convert.ToInt32(readerUsers["id"]))
                        {
                            tempList.Add(coupons);
                        }
                    }
                    users.Add(
                        new User(){
                            Id = Convert.ToInt32(readerUsers["id"]),
                            Username = readerUsers["Username"].ToString(), 
                            Email = readerUsers["Email"].ToString(), 
                            Password = readerUsers["Password"].ToString(),
                            Coupons = tempList
                        }
                    );

                }
                readerUsers.Close();
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
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Users/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE FROM Users WHERE Users.Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
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
            string passwordHashed = PasswordHasher.HashPassword(password);

            // Queries
            var sqlStr =
            $"INSERT INTO Users (IsAdmin, Username, Email, Password) VALUES ('false', '{username}', '{email}', '{passwordHashed}')";
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
                    await cmd.ExecuteNonQueryAsync();
                }

                // Close the database connection
                DBConnect.Dispose(conn);
                return req.CreateResponse(HttpStatusCode.OK, "Successfully registered the user");
            }
        }
    
        [FunctionName("EditUser")]
        public static async Task<HttpResponseMessage> EditUser(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route= "Users/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string username = formData["username"];
            string email = formData["email"];
            string password = formData["password"];
            string isAdmin = formData["isadmin"];

            string sqlStrUpdate = $"UPDATE Users SET " + 
            $"Username = COALESCE({(username == null ? "NULL" : $"'{username}'")}, Username), " + 
            $"Email = COALESCE({(email == null ? "NULL" : $"'{email}'")}, Email), " + 
            $"Password = COALESCE({(password == null ? "NULL" : $"'{password}'")}, Password), " + 
            $"IsAdmin = COALESCE({(isAdmin == null ? "NULL" : $"'{isAdmin}'")}, IsAdmin) " + 
            $"WHERE Id = {id}";

            SqlConnection conn = DBConnect.GetConnection();
            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
                {
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid input type");
                    }
                    
                }

                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the user");
            }
            catch(InvalidCastException e)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, e);
            }
        }

    }
}
