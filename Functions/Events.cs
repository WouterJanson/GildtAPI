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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<Event> events = new List<Event>();
            var sqlStr = "SELECT [Id],[Name],[DateTime],[Image],[Location],[IsActive],[ShortDescription],[LongDescription] FROM[dbo].[Events]"; // get all events

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    List<Event> eventsList = new List<Event>();

                    eventsList.Add(new Event()
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

                    if (events.Count == 0)
                    {
                        events.Add(
                            new Event()
                            {
                                EventId = Convert.ToInt32(reader["UserId"]),
                               
                            }
                        );
                    }
                    //Event item = new Event();

                    //item.EventId = Convert.ToInt32(reader["EventId"]);
                    //item.name = reader["Name"].ToString();
                    //item.DateTime = DateTime.Parse(reader["DateTime"].ToString());
                    //item.image = reader["Image"].ToString();
                    //item.location = reader["Image"].ToString();
                    //item.IsActive = (bool)reader["IsActive"];
                    //item.ShortDescription = reader["ShortDescription"].ToString();
                    //item.LongDescription = reader["LongDescription"].ToString();

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



