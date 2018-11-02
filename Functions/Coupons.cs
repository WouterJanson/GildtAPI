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

            var sqlStr = $"SELECT * FROM Coupons";

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
        public static async Task<IActionResult> DeleteCoupons(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Coupons/{id}")] HttpRequest req,
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
        public static async Task<HttpResponseMessage> EditCoupons(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Coupons/{id}")] HttpRequestMessage req,
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
    }
}
