using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;


namespace Company.Function
{
    public static class AddEvent
    {
        [FunctionName("AddEvent")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
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

        //if (description.Length >= 15) // if there are no 2 input fields of description and it needs to be checked if its a long or short description
        //{
        //.. code where description is determined LONG 
        //}

        //else
        //{
        //.. code where description is determined SHORT 
        //}

    }
}
