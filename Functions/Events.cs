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

namespace GildtAPI.Functions
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

            // error handling for if input is valid
            if (id != null)
            {
                //check if id is numeric, if it is a number it will give back true if not false
                if (Regex.IsMatch(id, @"^\d+$") == false)
                {
                    return (ActionResult)new BadRequestObjectResult("Invalid input, id should be numeric and not negative");
                }
            }
            
            // get all events
            var sqlStr = $"SELECT TOP {qCount} Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription, Tag, TagId FROM Events " +
                $"LEFT JOIN (SELECT EventsTags.EventsId, Tags.Name AS Tag, Tags.Id AS TagId FROM EventsTags " +
                $"LEFT JOIN Tags ON EventsTags.TagsId = Tags.Id) as tags ON Events.Id = tags.EventsId";
            var sqlWhere = $" WHERE Events.Id = {id}";
            var sqlOrder = " ORDER BY Events.Id";

            SqlConnection conn = DBConnect.GetConnection();

            // Checks if the id parameter is filled in
            if (id != null)
            {
                // if ID is specified in the request, add a where clasule to the query
                sqlStr = sqlStr + sqlWhere;

                    // check if event exist if not throw a status 404 event not found
                    using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                    {
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();

                        if (reader.HasRows == false)
                        {
                            return (ActionResult)new NotFoundObjectResult("Invalid input, event does not exist");
                        }
                        reader.Close();
                    }
            }
            else
            {
                sqlStr = sqlStr + sqlOrder;
            }
            

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                Event currentEvent = null;
                List<Tag> currentEventTagsList = new List<Tag>();

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

                    //check if it is the first event from the reader
                    if (currentEvent == null)
                    {                        
                        currentEvent = newEvent;
                    }

                    // check if reader got a new event
                    if (currentEvent.Id != newEvent.Id)
                    {
                        currentEvent.Tags = currentEventTagsList.ToArray(); //sla alle tags in de list van "currentEventTags" op in current event.tags zodra een nieuwe event binnenkomt.                       
                                               
                        events.Add(currentEvent); //add the event with its tags to the events list                       
                        currentEvent = newEvent; // the new event will now be current event

                        currentEventTagsList = new List<Tag>(); // make a new empty list of "currentEventTags" when a new event has been read
                    }

                    //read the TagId + TagName of the corresponding Event
                    int tagId = Convert.ToInt32(reader["TagId"]);                  
                    string tagName = reader["Tag"].ToString();

                    //Add tag to current event tags List
                    currentEventTagsList.Add(new Tag(tagId, tagName));
                }

                //add the last event from the reader 
                currentEvent.Tags = currentEventTagsList.ToArray();
                events.Add(currentEvent);
                reader.Close();
            }

            DBConnect.Dispose(conn);

            string j = JsonConvert.SerializeObject(events);

            return events != null
                ? (ActionResult)new OkObjectResult(j)
                : new NotFoundObjectResult("No events where found");
        }

        [FunctionName("DeleteEvent")]
        public static async Task<IActionResult> DeleteEvent(
           [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Events/Delete/{id}")] HttpRequest req,
           ILogger log, string id)
        {
            //queries
            var sqlStr = $"DELETE Events WHERE Id = '{id}'";

            // error handling for if input is valid
            if (id != null)
            {
                //check if id is numeric, if it is a number it will give back true if not false
                if (Regex.IsMatch(id, @"^\d+$") == false)
                {
                    return (ActionResult)new BadRequestObjectResult("Invalid input, id should be numeric and not negative");
                }
            }

            SqlConnection conn = DBConnect.GetConnection();
           
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {               
                int rowsaffect = await cmd.ExecuteNonQueryAsync();

                //check if the query has affected any rows if not than the given EventId does not exist
                if (rowsaffect > 0)
                {
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the event");
                }

                return (ActionResult)new NotFoundObjectResult("Invalid input, event does not exist");
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

            string dateTimeStart = (formData["dateTimeStart"]);
            string dateTimeEnd = (formData["dateTimeEnd"]);

            string shortdescription = formData["shortdescription"];
            string longdescription = formData["longdescription"];

            string image = formData["image"];

            // Queries
            var sqlStr =
            $"INSERT INTO Events (Name, Location, StartDate, EndDate, ShortDescription, LongDescription, Image, IsActive) VALUES ('{title}', '{location}', '{dateTimeStart}', '{dateTimeEnd}', '{shortdescription}', '{longdescription}', '{image}', 'false')";

            var sqlCheckEventStr = $"SELECT id, Name, Location, StartDate, EndDate FROM Events WHERE name = '{title}' AND StartDate = '{dateTimeStart}' AND EndDate = '{dateTimeEnd}'";

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

            // Returns bad request if one of the input fields are not filled in, gives back a status 400
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd2 = new SqlCommand(sqlCheckEventStr, conn))
            {
                SqlDataReader reader = await cmd2.ExecuteReaderAsync();
                // check if event already exist, check if name+startDate+EndDate combination already exist in the database, if it already exist give it a status 400
                if (reader.HasRows == true)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Event already exist");
                }

                reader.Close();
            }

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            // if everything is fine give back a status 200
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

            if (id != null)
            {
                //check if id is numeric, if it is a number it will give back true if not false
                if (Regex.IsMatch(id, @"^\d+$") == false) 
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid input, id should be numeric and not negative"); // status 400
                }
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                int rowsaffect = await cmd.ExecuteNonQueryAsync();

                // check if rows in the database have been affected, if it went succefult than the given EventId exist
                if (rowsaffect > 0)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.OK, "Sucessfully edited the event"); // status 200
                }

                return req.CreateResponse(HttpStatusCode.NotFound, "Invalid input, event does not exist"); // status 404
            }

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
            var sqlStr = $"INSERT INTO Tags (Name) VALUES ('{tag}')";
            var sqlTagCheckStr = $"SELECT Name FROM Tags WHERE Name ='{tag}'";

            //Checks if the input fields are filled in
            if (tag == "" || tag == null)
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

            // check if tag already exist in the database to avoid dublicate entries
            using (SqlCommand cmd2 = new SqlCommand(sqlTagCheckStr, conn))

            using (SqlDataReader reader = cmd2.ExecuteReader())
            {
                
                //check if tag already exist in the database
                if (reader.HasRows)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Could not create tag, tag does already exist... this would create a dublicate tag");
                }

                reader.Close();
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
            var sqlStr = $"INSERT INTO EventsTags (EventsId, TagsId) VALUES ('{eventId}', '{tagId}')";
            // query to check if event even exist by checking the id
            var sqlEventStr = $"SELECT Events.Id as EventId FROM Events WHERE Events.Id = {eventId}";
            // querry to validate Tag (does it exist?)
            var sqlTagCheckStr = $"SELECT Id FROM Tags WHERE id ='{tagId}'";
            // querry to check if the given tag is already assigned to a event
            var SqlCheckIfAssigned = $"SELECT TagsId, EventsId FROM EventsTags WHERE TagsId = '{tagId}' AND EventsId = '{eventId}'";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            if (eventId != null)
            {
                //check if id is numeric, if it is a number it will give back true if not false
                if (Regex.IsMatch(eventId, @"^\d+$") == false)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid input, EventId should be numeric and not negative"); // status 400
                }
            }

            if (tagId != null)
            {                
                if (Regex.IsMatch(tagId, @"^\d+$") == false)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid input, TagId should be numeric and not negative"); 
                }
            }

            //check if given event exist
            using (SqlCommand cmd = new SqlCommand(sqlEventStr, conn))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // check if query has found an event by the given EventId
                    if (reader.HasRows == false)
                    {
                        reader.Close();
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.NotFound, "Event not found");
                    }                    
                }               
            }

            //check if given tag exist
            using (SqlCommand cmd2 = new SqlCommand(sqlTagCheckStr, conn))
            {
                using (SqlDataReader reader2 = cmd2.ExecuteReader())
                {
                    // check if the query has found a tag with the given TagId
                    if (reader2.HasRows == false)
                    {
                        reader2.Close();
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.NotFound, "tag not found");                        
                    }
                }
            }

            //check if a tag is already assigned to the event
            using (SqlCommand cmd4 = new SqlCommand(SqlCheckIfAssigned, conn))
            {
                using (SqlDataReader reader4 = cmd4.ExecuteReader())
                {
                    // check if the query has found a tag with the given TagId
                    if (reader4.HasRows == true)
                    {
                        reader4.Close();
                        // Close the database connection
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.BadRequest, "tag is already assigned to the event");
                    }
                }
            }

            //execute operation if everything is OK
            using (SqlCommand cmd3 = new SqlCommand(sqlStr, conn))
            {
                // insert in to the table EventsTags
                await cmd3.ExecuteNonQueryAsync();
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

            if (id != null)
            {
                //check if id is numeric, if it is a number it will give back true if not false
                if (Regex.IsMatch(id, @"^\d+$") == false)
                {
                    return (ActionResult)new BadRequestObjectResult("Invalid input, id should be numeric and not negative"); // status 400
                }
            }
         
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                int affectedRows = cmd.ExecuteNonQuery();
                //cmd.ExecuteNonQuery();
                DBConnect.Dispose(conn);
                if (affectedRows == 0)
                {
                return new NotFoundObjectResult($"Deleting TAGS failed: Tag {id} does not have any tags!");
                }

                return (ActionResult)new OkObjectResult("Sucessfully deleted the tag");
            }            
           
        }


        [FunctionName("EditTags")]
        public static async Task<HttpResponseMessage> EditCoupon(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Events/Tags/{id}")] HttpRequestMessage req,
            ILogger log, string id)
        {
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string name = formData["Name"];

            //query om te updaten
            string sqlStrUpdate = $"UPDATE Tags SET " +
                                  $"Name = COALESCE({(name == null ? "NULL" : $"'{name}'")}, Name)" +
                                  $"Where Id= {id}";
            //db connectie
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                try
                {
                    //krijgt pas waarde als query is voldaan
                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);

                    //controleren of er rows in de DB zijn aangepast return 400
                    if (affectedRows == 0)
                    {
                        return req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Edit Tags failed: Tag with id: {id} does not exist.");
                    }
                    //waardes in DB aangepast return 200
                    return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the Tag"); ;
                }
                catch (InvalidCastException e)
                {
                    //object niet gevonden return 404
                    return req.CreateErrorResponse(HttpStatusCode.BadRequest, e.Message);
                }

            }

        }

    }
}



