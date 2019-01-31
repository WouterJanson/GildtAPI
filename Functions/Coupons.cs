using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using GildtAPI.Model;
using GildtAPI.Controllers;

namespace GildtAPI.Functions
{
    public static class Coupons
    {
        [FunctionName("GetAllCoupons")]
        public static async Task<HttpResponseMessage> GetCouponsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Coupons")] HttpRequestMessage req,
            ILogger log)
        {
            List<Coupon> coupons = await CouponController.Instance.GetAllAsync();

            return coupons.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, coupons, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error returning the coupons", "application/json");
        }

        [FunctionName("GetSingleCoupon")]
        public static async Task<HttpResponseMessage> GetCouponAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Coupons/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            Coupon coupon = await CouponController.Instance.GetAsync(Convert.ToInt32(id));

            return coupon != null
                ? req.CreateResponse(HttpStatusCode.OK, coupon, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "The coupon does not exists", "application/json");
        }

        [FunctionName("AddCoupon")]
        public static async Task<HttpResponseMessage> AddCouponAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Coupons")] HttpRequestMessage req,
            ILogger log)
        {
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;

            var coupon = new Coupon
            {
                Name = formData["Name"],
                Description = formData["Description"],
                StartDate = Convert.ToDateTime(formData["StartDate"]),
                EndDate = Convert.ToDateTime(formData["EndDate"]),
                Type = Convert.ToInt32(formData["Type"]),
                Image = formData["Image"]
            };


            bool inputIsValid = GlobalFunctions.CheckInputs(coupon.Name, coupon.Description, coupon.StartDate.ToString(), coupon.EndDate.ToString(), coupon.Type.ToString(), coupon.Image);

            if (!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Not all fields are filled in.", "application/json");
            }

            int rowsAffected = await CouponController.Instance.CreateAsync(coupon);

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully created the coupon.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error creating the coupon.", "application/json");
        }

        [FunctionName("DeleteCoupons")]
        public static async Task<HttpResponseMessage> DeleteCouponAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Coupons/{id?}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await CouponController.Instance.DeleteAsync(Convert.ToInt32(id));

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully deleted the coupon.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error deleting the coupon.", "application/json");
        }

        [FunctionName("EditCoupon")]
        public static async Task<HttpResponseMessage> EditCouponAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Coupons/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;

            var coupon = new Coupon
            {
                Id = int.Parse(id),
                Name = formData["Name"],
                Description = formData["Description"],
                StartDate = DateTime.Parse(formData["StartDate"]),
                EndDate = DateTime.Parse(formData["EndDate"]),
                Type = int.Parse(formData["Type"]),
                Image = formData["Image"]
            };

            int rowsAffected = await CouponController.Instance.EditAsync(coupon);

            return rowsAffected > 0
            ? req.CreateResponse(HttpStatusCode.OK, "Successfully edited the coupon.", "application/json")
            : req.CreateResponse(HttpStatusCode.BadRequest, "Error editing the coupon.", "application/json");

        }

        [FunctionName("SignupCoupon")]
        public static async Task<HttpResponseMessage> SignupCouponAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Coupons/{couponId}/Signup/{userId}")] HttpRequestMessage req,
            ILogger log, string couponId, string userId)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(userId) || !GlobalFunctions.CheckValidId(couponId))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await CouponController.Instance.SignUpAsync(Convert.ToInt32(couponId), Convert.ToInt32(userId));

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully signed up.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error signing up", "application/json");
        }
    }
}
