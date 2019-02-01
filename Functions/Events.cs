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
    public static class Events
    {
        [FunctionName("GetAllEvents")]
        public static async Task<HttpResponseMessage> GetAllEventsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Events")] HttpRequestMessage req,
            ILogger log)
        {
            var events = await EventController.Instance.GetAllAsync();

            return events.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, events, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "", "application/json");

        }


        [FunctionName("GetEvent")]
        public static async Task<HttpResponseMessage> GetEventAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Events/{id}")] HttpRequestMessage req,
         ILogger log, string id)
        {
            if (!GlobalFunctions.CheckValidId(id)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and no should not contain special characters", "application/json");
            }

            var evenT = await EventController.Instance.GetEventAsync(Convert.ToInt32(id));
            // check if a event is found by given id, if not than give a 404 not found
            if (evenT == null) {
                return req.CreateResponse(HttpStatusCode.NotFound, "Event could not be found by the given ID", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.OK, evenT, "application/json");
        }


        [FunctionName("DeleteEvent")]
        public static async Task<HttpResponseMessage> DeleteEventAsync(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Events/Delete/{id}")] HttpRequestMessage req,
           ILogger log, string id)
        {

            if (!GlobalFunctions.CheckValidId(id)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await EventController.Instance.DeleteEventAsync(Convert.ToInt32(id));
            if (rowsAffected > 0)  {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully deleted the event.", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.BadRequest, "Error deleting the event, check if the specified id is correct", "application/json");

        }


        [FunctionName("CreateEvent")]
        public static async Task<HttpResponseMessage> CreateEventAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Events/Add")] HttpRequestMessage req,
            ILogger log)
        {
            // Read data from input
            NameValueCollection formData = await req.Content.ReadAsFormDataAsync();

            var evenT = new Event {
                Name = formData["title"],
                Location = formData["location"],
                StartDate = DateTime.Parse(formData["dateTimeStart"]),
                EndDate = DateTime.Parse(formData["dateTimeEnd"]),
                ShortDescription = formData["shortdescription"],
                LongDescription = formData["longdescription"],
                DressCode = formData["dresscode"],
                Image = formData["image"]
            };

            bool inputIsValid = GlobalFunctions.CheckInputs(evenT.Name, evenT.Location, evenT.StartDate.ToString(), evenT.EndDate.ToString());
            if (!inputIsValid) {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Not all required fields are filled in. Be sure that name, location and dates are filled in...", "application/json");
            }

            int rowsAffected = await EventController.Instance.CreateEventAsync(evenT);
            if (rowsAffected > 0) {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully created event.", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.BadRequest, "creating event failed, event might already exsist", "application/json");
        }


        [FunctionName("EditEvent")]
        public static async Task<HttpResponseMessage> EditEventAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Events/Edit/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            // Read data from input
            var formData = await req.Content.ReadAsFormDataAsync();

            Event evenT = new Event {
                Id = Convert.ToInt32(id),
                Name = formData["title"],
                Location = formData["location"],
                StartDate = DateTime.Parse(formData["dateTimeStart"]),
                EndDate = DateTime.Parse(formData["dateTimeEnd"]),
                ShortDescription = formData["shortdescription"],
                LongDescription = formData["longdescription"],
                DressCode = formData["dresscode"],
                Image = formData["image"]
            };

            if (!GlobalFunctions.CheckValidId(id)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and should not contain special characters", "application/json");
            }

            int RowsAffected = await EventController.Instance.EditEventAsync(evenT);
            if (RowsAffected > 0) {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the event.", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.BadRequest, "editing event failed, check if the specified id is correct", "application/json");
        }


        [FunctionName("AddTagToEvent")]
        public static async Task<HttpResponseMessage> AddTagToEventAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Events/Tags/Add/{Eventid}/{tagId}")] HttpRequestMessage req,
            ILogger log, string Eventid, string TagId)
        {
            if (!GlobalFunctions.CheckValidId(Eventid)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid EventId", "application/json");
            }

            if (!GlobalFunctions.CheckValidId(TagId)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid TagId", "application/json");
            }

            int rowsAffected = await EventController.Instance.AddTagToEventAsync(Convert.ToInt32(Eventid), Convert.ToInt32(TagId));
            if (rowsAffected > 0) {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully added Tag the the Event!", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.BadRequest, "adding Tag failed, check if the specified id's are correct", "application/json");
        }


        [FunctionName("RemoveTagFromEvent")]
        public static async Task<HttpResponseMessage> RemoveTagFromEventAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Events/Tags/Remove/{Eventid}/{tagId}")] HttpRequestMessage req,
            ILogger log, string Eventid, string TagId)
        {
            if (!GlobalFunctions.CheckValidId(Eventid)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid EventId", "application/json");
            }

            if (!GlobalFunctions.CheckValidId(TagId)) {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid TagId", "application/json");
            }

            int rowsAffected = await EventController.Instance.RemoveTagFromEventAsync(Convert.ToInt32(Eventid), Convert.ToInt32(TagId));
            if (rowsAffected > 0) {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully removed the Tag from the Event!", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.BadRequest, "removing Tag failed, check if the specified id's are correct", "application/json");
        }

    }
}


