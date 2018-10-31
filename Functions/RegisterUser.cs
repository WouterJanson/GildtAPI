using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;


namespace Company.Function
{
    public static class RegisterUser
    {
        [FunctionName("RegisterUser")]
        public static async Task<HttpResponseMessage> Run(
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
            if(username == null)
            {
                missingFields.Add("Username");
            }
            if(email == null)
            {
                missingFields.Add("Email");
            }
            if(password == null)
            {
                missingFields.Add("Password");
            }

            // Returns bad request if one of the input fields are not filled in
            if(missingFields.Any())
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
            if(UserExist > 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "There is already an account registered with the username or email");
            }
            else
            {
                using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
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
