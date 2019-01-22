using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using GildtAPI.Model;
using System.Net.Http;
using System.Collections.Specialized;
using System.Net;
using GildtAPI.Controllers;

namespace GildtAPI.Functions
{
    public static class Tags
    {
        [FunctionName("EditTags")]
        public static async Task<HttpResponseMessage> EditTags(
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

            int rowsAffected = await TagController.Instance.EditTag(tag, id);

            //controleren of er rows in de DB zijn aangepast return 400
            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Edit Tags failed: Tag does not exist.", "application/json");
            }
            else
            {
                //waardes in DB aangepast return 200
                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the Tag", "application/json");
            }
        }


        [FunctionName("CreateTag")]
        public static async Task<HttpResponseMessage> CreateTag(
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

            int status = await TagController.Instance.CreateTag(tag);

            if (status == 400)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Could not create tag, tag does already exist... this would create a dublicate tag", "application/json");
            }
            else if (status > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully created Tag.", "application/json");
            }
            else // is dit niet overbodig zoek uit
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "creating Tag failed.", "application/json");
            }
        }


        [FunctionName("DeleteTags")]
        public static async Task<HttpResponseMessage> DeleteTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Tags/Delete/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and no should not contain special characters", "application/json");
            }

            int rowsAffected = await TagController.Instance.DeleteTag(Convert.ToInt32(id));

            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Deleting TAGS failed: Tag does not exist", "application/json");
            }

            else
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully deleted Tag!", "application/json");
            }

        }
    }
}
