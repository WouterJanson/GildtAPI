using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using GildtAPI.Model;
using System.Net.Http;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using GildtAPI.Controllers;

namespace GildtAPI.Functions
{
    public static class Events
    {
        [FunctionName("GetEvents")]
        public static async Task<HttpResponseMessage> GetEvents(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Events")] HttpRequestMessage req,
            ILogger log)
        {
            List<Event> events = await EventController.Instance.GetAll();

            string j = JsonConvert.SerializeObject(events);

            return events.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, events, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "", "application/json");

        }


        [FunctionName("GetEvent")]
        public static async Task<HttpResponseMessage> GetEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Events/{id}")] HttpRequestMessage req,
         ILogger log, string id)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and no should not contain special characters");
            }

            Event evenT = await EventController.Instance.GetEvent(Convert.ToInt32(id));

            // check if a event is found by given id, if not than give a 404 not found
            if (evenT == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "Event could not be found by the given ID");
            }

            return req.CreateResponse(HttpStatusCode.OK, evenT);

        }


        [FunctionName("DeleteEvent")]
        public static async Task<HttpResponseMessage> DeleteEvent(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Events/Delete/{id}")] HttpRequestMessage req,
           ILogger log, string id)
        {

            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id");
            }

            int rowsAffected = await EventController.Instance.DeleteEvent(Convert.ToInt32(id));

            if (rowsAffected > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully deleted the event.");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Error deleting the event");
            }

        }


        [FunctionName("AddEvent")]
        public static async Task<HttpResponseMessage> AddEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Events/Add")] HttpRequestMessage req,
            ILogger log)
        {
            List<string> missingFields = new List<string>();
            Event evenT = new Event();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            evenT.Name = formData["title"];
            evenT.Location = formData["location"];
            evenT.StartDate = DateTime.Parse(formData["dateTimeStart"]);
            evenT.EndDate = DateTime.Parse(formData["dateTimeEnd"]);
            evenT.ShortDescription = formData["shortdescription"];
            evenT.LongDescription = formData["longdescription"];
            evenT.Image = formData["image"];

            bool inputIsValid = GlobalFunctions.CheckInputs(evenT.Name, evenT.Location, evenT.StartDate.ToString(), evenT.EndDate.ToString());

            if (!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Not all required fields are filled in. Be sure that name, location and dates are filled in...", "application/json");
            }

            int status = await EventController.Instance.CreateEvent(evenT);

            if (status == 400)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Event already exist");
            }
            else if (status > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully created event.");
            }
            else 
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "creating event failed.");
            }

        }


        [FunctionName("EditEvent")]
        public static async Task<HttpResponseMessage> EditEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Events/Edit/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            // Read data from input
            Event evenT = new Event();
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            evenT.Id = Convert.ToInt32(id);
            evenT.Name = formData["title"];
            evenT.Location = formData["location"];
            evenT.StartDate = DateTime.Parse(formData["dateTimeStart"]);
            evenT.EndDate = DateTime.Parse(formData["dateTimeEnd"]);
            evenT.ShortDescription = formData["shortdescription"];
            evenT.LongDescription = formData["longdescription"];
            evenT.Image = formData["image"];

            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and should not contain special characters");
            }

            int RowsAffected = await EventController.Instance.EditEvent(evenT);

            if (RowsAffected > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the event.");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "editing event failed.");
            }

        }


        [FunctionName("AddTags")]
        public static async Task<HttpResponseMessage> AddTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Events/Tags/Add/{Eventid}/{tagId}")] HttpRequestMessage req,
            ILogger log, string Eventid, string TagId)
        {

            // Check if Eventid is valid
            if (!GlobalFunctions.CheckValidId(Eventid))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid EventId");
            }

            // Check if TagId is valid
            if (!GlobalFunctions.CheckValidId(TagId))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid TagId");
            }

            int status = await EventController.Instance.AddTag(Convert.ToInt32(Eventid), Convert.ToInt32(TagId));

            //error handling 
            if (status == 400)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Could not add the tag to the event, the event does not exist...");
            }
            else if (status == 401)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Could not add the tag to the event, the tag does not exist...");
            }
            else if (status == 402)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Could not add the tag to the event, the tag is already assigned to the specified Event...");
            }
            else if (status > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully added Tag the the Event!");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "adding Tag failed.");
            }
        }


        [FunctionName("EditTags")]
        public static async Task<HttpResponseMessage> EditTags(
           [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Events/Tags/{id}")] HttpRequestMessage req,
           ILogger log, string id)
        {

            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tag = formData["Name"];

            //check if id is numeric, if it is a number it will give back true if not false
            if (Regex.IsMatch(id, @"^\d+$") == false)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid TagId");
            }

            int rowsAffected = await EventController.Instance.EditTag(tag,id);

            //controleren of er rows in de DB zijn aangepast return 400
            if (rowsAffected == 0)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Edit Tags failed: Tag does not exist.");
            }
            else
            {
                //waardes in DB aangepast return 200
                return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the Tag");
            }

        }


        [FunctionName("CreateTag")]
        public static async Task<HttpResponseMessage> CreateTag(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Events/Tags/Create")] HttpRequestMessage req,
            ILogger log)
        {
            List<string> missingFields = new List<string>();

            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tag = formData["tag"];

            bool inputIsValid = GlobalFunctions.CheckInputs(tag);

            if (!inputIsValid)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Tag name is not filled in", "application/json");
            }

            int status = await EventController.Instance.CreateTag(tag);

            if (status == 400)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Could not create tag, tag does already exist... this would create a dublicate tag");
            }
            else if (status > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully created Tag.");
            }
            else // is dit niet overbodig zoek uit
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "creating Tag failed.");
            }
        }


        [FunctionName("DeleteTags")]
        public static async Task<HttpResponseMessage> DeleteTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Events/Tags/Delete/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {

            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(id))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id, Id should be numeric and no should not contain special characters");
            }

            int rowsAffected = await EventController.Instance.DeleteTag(Convert.ToInt32(id));

            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Deleting TAGS failed: Tag does not exist");
            }

            else
            {
                return req.CreateResponse(HttpStatusCode.OK, "Successfully deleted Tag!");
            }

        }

       
    }
}


