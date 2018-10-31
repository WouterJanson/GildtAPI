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

            log.LogInformation("Test" + id);

            var sqlStr = $"SELECT TOP {qCount} Events.Id as EventId, Events.Name, Events.EndDate, Events.StartDate, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription FROM Events"; // get all events
            var sqlWhere = $" WHERE Events.Id = {id}";

            // Checks if the id parameter is filled in
            if (id != null)
            {
                sqlStr = sqlStr + sqlWhere;
            }

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    //List<Event> eventsList = new List<Event>();

                        events.Add(new Event()
                    {
                        EventId = Convert.ToInt32(reader["EventId"]),
                        name = reader["Name"].ToString(),
                        StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                        EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                        image = reader["Image"].ToString(),
                        location = reader["location"].ToString(),
                        IsActive = (bool)reader["IsActive"],
                        ShortDescription = reader["ShortDescription"].ToString(),
                        LongDescription = reader["LongDescription"].ToString()
                    }
                    );

                }
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

            //var sqlGet =
            //$"SELECT COUNT(*) FROM Events WHERE (Name = '{title}')";

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

            //Checks if the Event is already added

            //SqlCommand checkEvent = new SqlCommand(sqlGet, conn);
            //checkEvent.Parameters.AddWithValue("Name", title);
            //int EventExist = (int)checkEvent.ExecuteScalar();
            //if (EventExist > 0)
            //{
            //    return req.CreateResponse(HttpStatusCode.BadRequest, "There is already an Event added with that name );
            //}
            //else
            //{
            //    using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            //    {
            //        cmd.ExecuteNonQuery();
            //    }

            //    // Close the database connection
            //    DBConnect.Dispose(conn);
            //    return req.CreateResponse(HttpStatusCode.OK, "Successfully added the event");
            //}

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
                $" WHERE id = 1;";

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
    }
}



