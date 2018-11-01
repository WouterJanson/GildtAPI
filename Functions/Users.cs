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
            List<Coupon> couponsList = new List<Coupon>();

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
                SqlDataReader readerCoupons = cmdCoupons.ExecuteReader();
                while(readerCoupons.Read())
                {
                    couponsList.Add(
                        new Coupon() {
                            couponId = Convert.ToInt32(readerCoupons["CouponId"]),
                            name = readerCoupons["Name"].ToString(),
                            Description = readerCoupons["Description"].ToString(),
                            startDate = DateTime.Parse(readerCoupons["StartDate"].ToString()),
                            endDate = DateTime.Parse(readerCoupons["EndDate"].ToString()),
                            type = Convert.ToInt32(readerCoupons["Type"].ToString()),
                            totalUsed = Convert.ToInt32(readerCoupons["TotalUsed"]),
                            image = readerCoupons["Image"].ToString(),
                            UserId = Convert.ToInt32(readerCoupons["UserId"])
                        }
                    );
                }
                readerCoupons.Close();
            }

            using (SqlCommand cmdUsers = new SqlCommand(sqlStrUsers, conn))
            {
                SqlDataReader readerUsers = cmdUsers.ExecuteReader();
                while(readerUsers.Read())
                {
                    List<Coupon> tempList = new List<Coupon>();
                    foreach(Coupon coupons in couponsList)
                    {
                        if(coupons.UserId == Convert.ToInt32(readerUsers["id"]))
                        {
                            tempList.Add(coupons);
                        }
                    }
                    users.Add(
                        new User(){
                            userId = Convert.ToInt32(readerUsers["id"]),
                            username = readerUsers["Username"].ToString(), 
                            email = readerUsers["Email"].ToString(), 
                            password = readerUsers["Password"].ToString(),
                            coupons = tempList
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
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Users/{id}/Delete")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE User WHERE Id = '{id}'";

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
    
        [FunctionName("EditUser")]
        public static async Task<HttpResponseMessage> EditUser(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route= "Users/{id}/Edit")] HttpRequestMessage req,
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
                        cmd.ExecuteNonQuery();
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
