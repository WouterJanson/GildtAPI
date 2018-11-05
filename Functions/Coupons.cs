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
    public static class Coupons
    {
        [FunctionName("Coupons")]
        public static async Task<IActionResult> GetCoupons(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Coupons/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            //TODO Single coupon
            List<Coupon> couponsList = new List<Coupon>();

            var sqlStr = $"SELECT * FROM Coupons ";
            var sqlWhere = $"WHERE Id = '{id}'";
            if(id != null)
            {
                sqlStr = sqlStr + sqlWhere;
            }

            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while(reader.Read())
                {
                    couponsList.Add(
                        new Coupon() {
                            couponId = Convert.ToInt32(reader["Id"]),
                            name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            startDate = DateTime.Parse(reader["StartDate"].ToString()),
                            endDate = DateTime.Parse(reader["EndDate"].ToString()),
                            type = Convert.ToInt32(reader["Type"].ToString()),
                            totalUsed = Convert.ToInt32(reader["TotalUsed"]),
                            image = reader["Image"].ToString()
                        }
                    );
                }
                reader.Close();
            }

            DBConnect.Dispose(conn);
            
            string j = JsonConvert.SerializeObject(couponsList);

            return couponsList.Count >= 1
                ? (ActionResult)new OkObjectResult(j)
                : new NotFoundObjectResult("No Coupons where found");
        }
        
        [FunctionName("DeleteCoupons")]
        public static async Task<IActionResult> DeleteCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Coupons/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStrDelete = $"DELETE Coupons WHERE Id = {id}";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using(SqlCommand cmd = new SqlCommand(sqlStrDelete, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the coupon");
                }
            }
            catch(Exception e)
            {
                return (ActionResult)new NotFoundObjectResult(e);
            }
        }

        [FunctionName("EditCoupon")]
        public static async Task<HttpResponseMessage> EditCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Coupons/{id}")] HttpRequestMessage req,
            ILogger log, string id)
            {
                NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
                string name = formData["Name"];
                string description = formData["Description"];
                string startDate = formData["StartDate"];
                string endDate = formData["EndDate"];
                string type = formData["Type"];
                string image = formData["Image"];

                string sqlStrUpdate = $"UPDATE Users SET " + 
                    $"Username = COALESCE({(name == null ? "NULL" : $"'{name}'")}, Username), " + 
                    $"Email = COALESCE({(description == null ? "NULL" : $"'{description}'")}, Email), " + 
                    $"Password = COALESCE({(startDate == null ? "NULL" : $"'{startDate}'")}, Password), " + 
                    $"Password = COALESCE({(endDate == null ? "NULL" : $"'{endDate}'")}, Password), " + 
                    $"Password = COALESCE({(type == null ? "NULL" : $"'{type}'")}, Password), " + 
                    $"IsAdmin = COALESCE({(image == null ? "NULL" : $"'{image}'")}, IsAdmin) " + 
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

                    return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the coupon");
                }
                catch(InvalidCastException e)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, e);
                }
            }
    
        [FunctionName("AddCoupon")]
        public static async Task<HttpResponseMessage> AddCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Coupons")] HttpRequestMessage req,
            ILogger log, string id)
        {
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string name = formData["Name"];
            string description = formData["Description"];
            string startDate = formData["StartDate"];
            string endDate = formData["EndDate"];
            string type = formData["Type"];
            string image = formData["Image"];

            var sqlStr =
                $"INSERT INTO Coupons (Name, Description, StartDate, EndDate, Type, Image) VALUES ('{name}', '{description}', '{startDate}', '{endDate}', '{type}', '{image}')";
            var sqlGet =
                $"SELECT COUNT(*) FROM Coupons WHERE Name = {name}";

            //Checks if the input fields are filled in
            if (name == null)
            {
                missingFields.Add("Name");
            }
            if (description == null)
            {
                missingFields.Add("Description");
            }
            if (startDate == null)
            {
                missingFields.Add("Start Date");
            }
            if (endDate == null)
            {
                missingFields.Add("End Date");
            }
            if (type == null)
            {
                missingFields.Add("Type");
            }
            if (image == null)
            {
                missingFields.Add("Image");
            }

            // Returns bad request if one of the input fields are not filled in
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //Checks if the coupon is already registered
            SqlCommand checkCoupon = new SqlCommand(sqlGet, conn);
            checkCoupon.Parameters.AddWithValue("Name", name);
            int CouponExist = (int)checkCoupon.ExecuteScalar();
            if (CouponExist > 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "There is already a coupon registered with the same name");
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Close the database connection
                DBConnect.Dispose(conn);
                return req.CreateResponse(HttpStatusCode.OK, "Successfully added the coupon");
            }
        }

        [FunctionName("SignupCoupon")]
        public static async Task<IActionResult> SignupCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Coupons/{id}/Signup")] HttpRequest req,
            ILogger log, string id)
        {
            string user = req.Query["UserId"];
            if(user == null)
            {
                return (ActionResult)new BadRequestObjectResult("Missing query parameter UserId.");
            }

            //Query
            var sqlStr = $"INSERT INTO UsersCoupons (UserId, CouponId) VALUES ('{user}', '{id}'";
            var sqlGet =
                $"SELECT COUNT(*) FROM UsersCoupons WHERE CouponId = '{id}' AND UserId = '{user}'";

            SqlConnection conn = DBConnect.GetConnection();

            SqlCommand checkCoupon = new SqlCommand(sqlGet, conn);
            checkCoupon.Parameters.AddWithValue("CouponId", id);
            checkCoupon.Parameters.AddWithValue("UserId", user);
            int CouponExist = (int)checkCoupon.ExecuteScalar();
            if(CouponExist > 0)
            {
                return (ActionResult)new BadRequestObjectResult("This coupon is already registered by the user");
            }
            else
            {
                using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                DBConnect.Dispose(conn);
                return (ActionResult)new OkObjectResult("Succesfully signed up the coupon.");
            }    
        }
    }
}
