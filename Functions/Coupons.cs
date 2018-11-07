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
    public static class Coupons
    {
        [FunctionName("Coupons")]
        public static async Task<IActionResult> GetCoupons(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Coupons/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            List<Coupon> couponsList = new List<Coupon>();

            var sqlStr = $"SELECT * FROM Coupons";
            var sqlWhere = $" WHERE Id = '{id}'";
            
            // Check if input is valid
            try
            {
                if(id != null)
                {
                    int convId = Convert.ToInt32(id);
                }
            }
            catch
            {
                return new BadRequestObjectResult("Invalid input");
            }

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
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                            EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                            Type = Convert.ToInt32(reader["Type"].ToString()),
                            TotalUsed = Convert.ToInt32(reader["TotalUsed"]),
                            Image = reader["Image"].ToString()
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

       [FunctionName("AddCoupon")]
        public static async Task<HttpResponseMessage> AddCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Coupons")] HttpRequestMessage req,
            ILogger log)
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
                $"INSERT INTO Coupons (Name, Description, StartDate, EndDate, Type, TotalUsed, Image) VALUES ('{name}', '{description}', '{startDate}', '{endDate}', '{type}', '0', '{image}')";
            var sqlGet =
                $"SELECT COUNT(*) FROM Coupons WHERE Name = '{name}'";

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
                try
                {
                     using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();

                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.OK, "Successfully added the coupon");
                    }
                }
                catch(Exception e)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, "An error has occured");
                }
            }
        } 
       
        [FunctionName("DeleteCoupons")]
        public static async Task<IActionResult> DeleteCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Coupons/{id?}")] HttpRequest req,
            ILogger log, string id)
        {
            string qName = req.Query["name"];
            string sqlStrDelete;
            if(id != null)
            {
                sqlStrDelete = $"DELETE Coupons WHERE Id = '{id}'";
            }
            else
            {
                sqlStrDelete = $"DELETE Coupons WHERE Username = '{qName}'";
            }

            try
            {
                Convert.ToInt32(id);
            }
            catch
            {
                return new BadRequestObjectResult("Invalid input");
            }
            

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using(SqlCommand cmd = new SqlCommand(sqlStrDelete, conn))
                {
                   
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if(rowsAffected == 0)
                    {
                        return (ActionResult)new NotFoundObjectResult("No coupon where found");
                    }
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

                string sqlStrUpdate = $"UPDATE Coupons SET " + 
                    $"Name = COALESCE({(name == null ? "NULL" : $"'{name}'")}, Name), " + 
                    $"Description = COALESCE({(description == null ? "NULL" : $"'{description}'")}, Description), " + 
                    $"StartDate = COALESCE({(startDate == null ? "NULL" : $"'{startDate}'")}, StartDate), " + 
                    $"EndDate = COALESCE({(endDate == null ? "NULL" : $"'{endDate}'")}, EndDate), " + 
                    $"Type = COALESCE({(type == null ? "NULL" : $"'{type}'")}, Type), " + 
                    $"Image = COALESCE({(image == null ? "NULL" : $"'{image}'")}, Image) " + 
                    $" WHERE Id = {id}";

                SqlConnection conn = DBConnect.GetConnection();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
                    {
                        try
                        {
                            int rows = await cmd.ExecuteNonQueryAsync();
                            if(rows == 0)
                            {
                                return req.CreateErrorResponse(HttpStatusCode.NotFound, "Coupon not found");
                            }
                            else
                            {
                                 DBConnect.Dispose(conn);      
                                 return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the coupon");
                            }

                        }
                        catch
                        {
                            return req.CreateErrorResponse(HttpStatusCode.BadRequest, "An error has occured");
                        }          
                    }
                }
                catch(InvalidCastException e)
                {
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, e);
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
            var sqlStr = $"INSERT INTO UsersCoupons (UserId, CouponId) VALUES ('{user}', '{id}')";
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
