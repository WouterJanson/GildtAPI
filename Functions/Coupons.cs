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
using GildtAPI.Controllers;

namespace GildtAPI.Functions
{
    public static class Coupons
    {
        [FunctionName("GetAllCoupons")]
        public static async Task<HttpResponseMessage> GetCoupons(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Coupons")] HttpRequestMessage req,
            ILogger log)
        {
            List<Coupon> coupons = await CouponController.Instance.GetAll();

            string j = JsonConvert.SerializeObject(coupons);

            return coupons.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, coupons, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "", "application/json");
        }

        [FunctionName("GetSingleCoupon")]
        public static async Task<HttpResponseMessage> GetCoupon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Coupons/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            Coupon coupon = await CouponController.Instance.Get(Convert.ToInt32(id));

            return req.CreateResponse(HttpStatusCode.OK, coupon, "application/json");
        }

        [FunctionName("AddCoupon")]
        public static async Task<HttpResponseMessage> AddCoupon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Coupons")] HttpRequestMessage req,
            ILogger log)
        {
            List<string> missingFields = new List<string>();
            Coupon coupon = new Coupon();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            coupon.Name = formData["Name"];
            coupon.Description = formData["Description"];
            coupon.StartDate = Convert.ToDateTime(formData["StartDate"]);
            coupon.EndDate = Convert.ToDateTime(formData["EndDate"]);
            coupon.Type = Convert.ToInt32(formData["Type"]);
            coupon.Image = formData["Image"];


            bool inputIsValid = GlobalFunctions.CheckInputs(coupon.Name, coupon.Description, coupon.StartDate.ToString(), coupon.EndDate.ToString(), coupon.Type.ToString(), coupon.Image);

            if(!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Not all fields are filled in.", "application/json");
            }

            int rowsAffected = await CouponController.Instance.Create(coupon);

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully created the coupon.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error creating the coupon.", "application/json");
        } 
       
        [FunctionName("DeleteCoupons")]
        public static async Task<HttpResponseMessage> DeleteCoupon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Coupons/{id?}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await CouponController.Instance.Delete(Convert.ToInt32(id));

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully deleted the coupon.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error deleting the coupon.", "application/json");
        }

        [FunctionName("EditCoupon")]
        public static async Task<HttpResponseMessage> EditCoupon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Coupons/{id}")] HttpRequestMessage req,
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

        //[FunctionName("SignupCoupon")]
        //public static async Task<HttpResponseMessage> SignupCoupon(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Coupons/{couponId}/Signup/{userId}")] HttpRequestMessage req,
        //    ILogger log, string couponId, string userId)
        //{
        //    // Check if id is valid
        //    if (!GlobalFunctions.CheckValidId(userId) || !GlobalFunctions.CheckValidId(couponId))
        //    {
        //        return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
        //    }

        //    //Query
        //    var sqlStr = $"INSERT INTO UsersCoupons (UserId, CouponId) VALUES ('{userId}', '{couponId}')";
        //    var sqlGet =
        //        $"SELECT COUNT(*) FROM UsersCoupons WHERE CouponId = '{couponId}' AND UserId = '{userId}'";

        //    SqlConnection conn = DBConnect.GetConnection();

        //    SqlCommand checkCoupon = new SqlCommand(sqlGet, conn);
        //    checkCoupon.Parameters.AddWithValue("CouponId", couponId);
        //    checkCoupon.Parameters.AddWithValue("UserId", userId);
        //    int CouponExist = (int)checkCoupon.ExecuteScalar();
        //    if(CouponExist > 0)
        //    {
        //        return (ActionResult)new BadRequestObjectResult("This coupon is already registered by the user");
        //    }
        //    else
        //    {
        //        using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
        //        {
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        DBConnect.Dispose(conn);
        //        return (ActionResult)new OkObjectResult("Succesfully signed up the coupon.");
        //    }    
        //}
    }
}
