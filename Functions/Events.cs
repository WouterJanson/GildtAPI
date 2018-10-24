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

namespace GildtAPI
{
    public static class Events
    {
        [FunctionName("Events")]
        public static async Task<IActionResult> Run(
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

            var sqlStr = $"SELECT TOP {qCount} Events.Id as EventId, Events.Name, Events.DateTime, Events.DateTime, Events.Image, Events.Location, Events.IsActive, Events.ShortDescription, Events.LongDescription FROM Events"; // get all events
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
                        DateTime = DateTime.Parse(reader["DateTime"].ToString()),
                        image = reader["Image"].ToString(),
                        location = reader["Image"].ToString(),
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
                : new BadRequestObjectResult("No users where found");
        }

    }
}



