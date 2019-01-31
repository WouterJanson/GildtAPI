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
    public static class Users
    {
        [FunctionName("GetUsers")]
        public static async Task<HttpResponseMessage> GetUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Users")] HttpRequestMessage req,
            ILogger log)
        {
            List<User> users = await UserController.Instance.GetAll();

            string j = JsonConvert.SerializeObject(users);

            return users.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, users, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error returning the users", "application/json");
        }

        [FunctionName("GetUser")]
        public static async Task<HttpResponseMessage> GetUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Users/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            User user = await UserController.Instance.Get(Convert.ToInt32(id));

            return user != null 
                ? req.CreateResponse(HttpStatusCode.OK, user, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "The user does not exists", "application/json");
        }


        [FunctionName("DeleteUser")]
        public static async Task<HttpResponseMessage> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Users/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await UserController.Instance.Delete(Convert.ToInt32(id));

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully deleted the user.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error deleting the user.", "application/json");
        }


        [FunctionName("AddUser")]
        public static async Task<HttpResponseMessage> AddUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Users/Register")] HttpRequestMessage req,
            ILogger log)
        {
            User user = new User();
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            user.Username = formData["username"];
            user.Email = formData["email"];
            user.Password = PasswordHasher.HashPassword(formData["password"]);

            bool inputIsValid = GlobalFunctions.CheckInputs(user.Username, user.Email, user.Password);

            if(!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Not all fields are filled in.", "application/json");
            }

            int rowsAffected = await UserController.Instance.Create(user);

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully created the user.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error creating the user.", "application/json");
        }

        [FunctionName("EditUser")]
        public static async Task<HttpResponseMessage> EditUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route= "Users/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            User user = new User();
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            user.Id = Convert.ToInt32(id);
            user.Username = formData["username"];
            user.Email = formData["email"];
            user.Password = formData["password"];
            user.IsAdmin = Convert.ToBoolean(formData["isadmin"]);

            int rowsAffected = await UserController.Instance.Edit(user);

            return rowsAffected > 0
                ? req.CreateResponse(HttpStatusCode.OK, "Successfully edited the user.", "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Error editing the user.", "application/json");
        }
    }
}
