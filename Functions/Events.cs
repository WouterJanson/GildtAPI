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

namespace GildtAPI
{
    public static class Events
    {
        [FunctionName("Events")]
        public static async Task<IActionResult> GetEvents(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Events/{id?}")] HttpRequest req,
            ILogger log, string id)
        {

            List<Event> events = new List<Event>();

            string qCount = req.Query["count"];
            if (qCount == null)
            {
                qCount = "20";
            }

            // get all events
            var sqlStr = $"SELECT TOP {qCount} Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription, Tag, TagId FROM Events " +
                $"LEFT JOIN (SELECT EventsTags.EventsId, Tags.Name AS Tag, Tags.Id AS TagId FROM EventsTags " +
                $"LEFT JOIN Tags ON EventsTags.TagsId = Tags.Id) as tags ON Events.Id = tags.EventsId";
            var sqlWhere = $" WHERE Events.Id = {id}";
            var sqlOrder = " ORDER BY Events.Id";
            // Checks if the id parameter is filled in
            if (id != null)
            {
                // if ID is specified in the request, add a where clasule to the query
                sqlStr = sqlStr + sqlWhere;
            }
            else
            {
                sqlStr = sqlStr + sqlOrder;
            }

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                Event currentEvent = null;
                List<Tag> currentEventTags = new List<Tag>();
                while (reader.Read())
                {
                    Event newEvent = new Event()
                    {
                        //read event
                        Id = Convert.ToInt32(reader["EventId"]),
                        Name = reader["Name"].ToString(),
                        StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                        EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                        Image = reader["Image"].ToString(),
                        Location = reader["location"].ToString(),
                        IsActive = (bool)reader["IsActive"],
                        ShortDescription = reader["ShortDescription"].ToString(),
                        LongDescription = reader["LongDescription"].ToString()
                    };
                    if (currentEvent == null)
                    {
                        //reading first event
                        currentEvent = newEvent;
                    }
                    if (currentEvent.Id != newEvent.Id)
                    {
                        //reading next event: save all read tags + save event to list
                        currentEvent.Tags = currentEventTags.ToArray();
                        currentEventTags = new List<Tag>();
                        events.Add(currentEvent);
                        currentEvent = newEvent;
                    }
                    //read tag
                    string tagIdstr = reader["TagId"].ToString();
                    if (String.IsNullOrWhiteSpace(tagIdstr))
                    {
                        //tag is null, don't add
                        continue;
                    }
                    int tagId = int.Parse(tagIdstr);
                    string tagName = reader["Tag"].ToString();
                    //Add tag to current events tags
                    currentEventTags.Add(new Tag(tagId, tagName));
                    log.LogInformation($"Read tag with id {tagIdstr} eventid {currentEvent.Id}");
                }
                //add last read eventtags + event to list
                currentEvent.Tags = currentEventTags.ToArray();
                events.Add(currentEvent);
                reader.Close();
            }

            DBConnect.Dispose(conn);

            string j = JsonConvert.SerializeObject(events);

            return events != null
                ? (ActionResult)new OkObjectResult(j)
                : new BadRequestObjectResult("No events where found");
        }

        [FunctionName("DeleteEvent")]
        public static async Task<IActionResult> DeleteEvent(
           [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Events/Delete/{id}")] HttpRequest req,
           ILogger log, string id)
        {
            var sqlStr = $"DELETE Events WHERE Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the event");
                }
            }
            catch (InvalidCastException e)
            {
                return (ActionResult)new BadRequestObjectResult(e);
            }
        }

        [FunctionName("AddEvent")]
        public static async Task<HttpResponseMessage> AddEvent(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Events/Add")] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string title = formData["title"];
            string location = formData["location"];

            DateTime dateTimeStart = DateTime.Parse(formData["dateTimeStart"]);
            DateTime dateTimeEnd = DateTime.Parse(formData["dateTimeEnd"]);

            string shortdescription = formData["shortdescription"];
            string longdescription = formData["longdescription"];

            string image = formData["image"];

            // Queries
            var sqlStr =
            $"INSERT INTO Events (Name, Location, StartDate, EndDate, ShortDescription, LongDescription, Image, IsActive) VALUES ('{title}', '{location}', '{dateTimeStart}', '{dateTimeEnd}', '{shortdescription}', '{longdescription}', '{image}', 'false')";

            //Checks if the input fields are filled in
            if (title == null)
            {
                missingFields.Add("Event Name");
            }
            if (location == null)
            {
                missingFields.Add("Location");
            }
            if (dateTimeStart == null)
            {
                missingFields.Add("DateTime Start");
            }
            if (dateTimeEnd == null)
            {
                missingFields.Add("DateTime End");
            }

            // Returns bad request if one of the input fields are not filled in
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully added the event");
        }


        [FunctionName(nameof(EditEvent))]
        public static async Task<HttpResponseMessage> EditEvent(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Events/Edit/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string title = formData["title"];
            string location = formData["location"];

            string dateTimeStartString = formData["dateTimeStart"];
            string dateTimeEndString = formData["dateTimeEnd"];
            string isActiveString = formData["isactive"];
            string shortdescription = formData["shortdescription"];
            string longdescription = formData["longdescription"];
            string image = formData["image"];

            //queries

            var sqlStr = $"UPDATE Events SET " +
                $"Name = COALESCE({(title == null ? "NULL" : $"\'{title}\'")}, Name), " +
                $"Location = COALESCE({(location == null ? "NULL" : $"\'{location}\'")}, Location), " +
                $"StartDate = COALESCE({(dateTimeStartString == null ? "NULL" : $"\'{dateTimeStartString}\'")}, StartDate), " +
                $"EndDate = COALESCE({(dateTimeEndString == null ? "NULL" : $"\'{dateTimeEndString}\'")}, EndDate), " +
                $"ShortDescription = COALESCE({(shortdescription == null ? "NULL" : $"\'{shortdescription}\'")}, ShortDescription), " +
                $"LongDescription = COALESCE({(longdescription == null ? "NULL" : $"\'{longdescription}\'")}, LongDescription), " +
                $"Image = COALESCE({(image == null ? "NULL" : $"\'{location}\'")}, image), " +
                $"IsActive = COALESCE({(isActiveString == null ? "NULL" : $"\'{isActiveString}\'")}, IsActive) " +
                $" WHERE id = {id};";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the event");
        }


        [FunctionName("CreateTag")]
        public static async Task<HttpResponseMessage> CreateTag(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Events/Tags/Create")] HttpRequestMessage req,
        ILogger log)
        {
            List<string> missingFields = new List<string>();

            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tag = formData["tag"];

            // Queries
            var sqlStr =
            $"INSERT INTO Tags (Name) VALUES ('{tag}')";

            //Checks if the input fields are filled in
            if (tag == null)
            {
                missingFields.Add("Tag");
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            // Returns bad request if one of the input fields are not filled in
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully created tag");

        }


        [FunctionName("AddTags")]
        public static async Task<HttpResponseMessage> AddTags(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Events/Tags/Add/{Eventid}/{tagId}")] HttpRequestMessage req,
        ILogger log, string Eventid, string TagId)
        {

            string eventId = Eventid;
            string tagId = TagId;

            // Queries
            var sqlStr =
            $"INSERT INTO EventsTags (EventsId, TagsId) VALUES ('{eventId}', '{tagId}')";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            // query to check if event even exist by checking the id
            var sqlEventStr = $"SELECT Events.Id as EventId FROM Events WHERE Events.Id = {eventId}";

            using (SqlCommand cmd = new SqlCommand(sqlEventStr, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // check if query has found an event by the given EventId
                    if (reader.HasRows)
                    {
                        reader.Close();

                        using (SqlCommand cmd2 = new SqlCommand(sqlStr, conn))
                        {
                            await cmd2.ExecuteNonQueryAsync();
                        }

                    }
                    
                    else
                    {
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.NotFound, "Event not found");
                    }
                    
                }

                DBConnect.Dispose(conn);
                return req.CreateResponse(HttpStatusCode.OK, "Successfully added the taggs to the event");
            }
        }



        [FunctionName("DeleteTags")]
        public static async Task<IActionResult> DeleteTags(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Events/Tags/Delete/{id}")] HttpRequest req,
        ILogger log, string id)
        {
            var sqlStr = $"DELETE Tags WHERE Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    int affectedRows = cmd.ExecuteNonQuery();
                    //cmd.ExecuteNonQuery();
                    DBConnect.Dispose(conn);
                    if (affectedRows == 0)
                    {
                        return new BadRequestObjectResult(
                            $"Deleting TAGS failed: Tag {id} does not have any tags!");
                    }
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the tag");
                }
            }
            catch (InvalidCastException e)
            {
                return (ActionResult)new BadRequestObjectResult(e);
            }
        }

    }
}



