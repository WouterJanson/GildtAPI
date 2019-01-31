using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using GildtAPI.Controllers;
using GildtAPI.Model;

namespace GildtAPI.Functions
{
    public static class Tags
    {
        [FunctionName("GetAllTags")]
        public static async Task<HttpResponseMessage> GetAllTagsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Tags")] HttpRequestMessage req,
        ILogger log)
        {
            List<Tag> tags = await TagController.Instance.GetAllTagsAsync();

            return tags.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, tags, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "", "application/json");

        }

        [FunctionName("EditTags")]
        public static async Task<HttpResponseMessage> EditTagsAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Tags/Edit/{id}")] HttpRequestMessage req,
           ILogger log, string id)
        {
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tag = formData["Name"];

            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and should not contain special characters", "application/json");
            }

            int rowsAffected = await TagController.Instance.EditTagAsync(tag, id);

            //controleren of er rows in de DB zijn aangepast return 400
            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Edit Tag failed: Tag does not exist.", "application/json");
            }
            else
            {
                //waardes in DB aangepast return 200
                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the Tag", "application/json");
            }
        }


        [FunctionName("CreateTag")]
        public static async Task<HttpResponseMessage> CreateTagAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Tags/Create")] HttpRequestMessage req,
            ILogger log)
        {
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tag = formData["tag"];

            bool inputIsValid = GlobalFunctions.CheckInputs(tag);

            if (!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Tag name is not filled in", "application/json");
            }

            int rowsAffected = await TagController.Instance.CreateTagAsync(tag);

            if (rowsAffected > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully created Tag.", "application/json");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "creating Tag failed, tag might already exist", "application/json");
            }
        }


        [FunctionName("DeleteTags")]
        public static async Task<HttpResponseMessage> DeleteTagsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Tags/Delete/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and no should not contain special characters", "application/json");
            }

            int rowsAffected = await TagController.Instance.DeleteTagAsync(Convert.ToInt32(id));

            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, $"Deleting TAG failed: Tag does not exist", "application/json");
            }

            else
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully deleted Tag!", "application/json");
            }

        }
    }
}
