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
            List<TagsEvents> tagsEvents = new List<TagsEvents>();

            string qCount = req.Query["count"];
            if (qCount == null)
            {
                qCount = "20";
            }

            log.LogInformation("Test" + id);

            // get all events
            var sqlStr = $"SELECT TOP {qCount} Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription FROM Events";
            // get all tags
            var sqlStrTags = $"SELECT EventsTags.TagsId, EventsTags.Name, EventsTags.EventsId FROM EventsTags";
            // if ID is specified in the request, add a where clasule to the query
            var sqlWhere = $" WHERE Events.Id = {id}";

            // Checks if the id parameter is filled in
            if (id != null)
            {
                sqlStr = sqlStr + sqlWhere;
            }

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmdTags = new SqlCommand(sqlStrTags, conn))
            {
                SqlDataReader readerTags = cmdTags.ExecuteReader();
                while (readerTags.Read())
                {
                    tagsEvents.Add(
                        new TagsEvents()
                        {
                            Tag = new Tag()
                            {
                                //Id = Convert.ToInt32(readerTags["TagsId"]),
                                Name = readerTags["Name"].ToString()
                            },
                            Event = new Event()
                            {
                                Id = Convert.ToInt32(readerTags["EventsId"])
                            }
                        }
                    );
                }

                readerTags.Close();
            }

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    List<Tag> tags = new List<Tag>();
                    foreach (var tagEvent in tagsEvents)
                    {
                        if (tagEvent.Event.Id == Convert.ToInt32(reader["EventId"]))
                        {
                            tags.Add(tagEvent.Tag);
                        }
                    }

                    events.Add(
                        new Event()
                        {
                            Id = Convert.ToInt32(reader["EventId"]),
                            Name = reader["Name"].ToString(),
                            StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                            EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                            Image = reader["Image"].ToString(),
                            Location = reader["location"].ToString(),
                            IsActive = (bool)reader["IsActive"],
                            ShortDescription = reader["ShortDescription"].ToString(),
                            LongDescription = reader["LongDescription"].ToString(),
                            Tags = tags.ToArray<Tag>()
                        }
                    );

                }
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
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Events/Delete/{id}")] HttpRequest req,
           ILogger log, string id)
        {
            var sqlStr = $"DELETE Events WHERE Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.ExecuteNonQuery();
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
                cmd.ExecuteNonQuery();
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
                cmd.ExecuteNonQuery();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully edited the event");
        }


        [FunctionName("AddTags")]
        public static async Task<HttpResponseMessage> AddTags(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Events/Tags/Add/{id}")] HttpRequestMessage req,
        ILogger log, string id)
        {
            
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string tags = formData["tags"];
            string eventId = id;

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //check if tags are filled if so fill the Tags table
            if (tags != null)
            {

                string[] tagsArray = tags.Split(',');

                foreach (var item in tagsArray)
                {
                    string tag = item;

                    var sqlTagStr =
                    $"INSERT INTO EventsTags (Name, EventsId) VALUES ('{tag}', '{eventId}')";

                    using (SqlCommand cmd = new SqlCommand(sqlTagStr, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                }
          
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully added tags to the event");

        }
    }
}



