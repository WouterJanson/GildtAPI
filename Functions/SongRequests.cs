using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using GildtAPI.Model;

namespace GildtAPI.Functions
{
    public static class SongRequests
    {
        [FunctionName("SongRequest")]
        public static async Task<IActionResult> GetSongRequests(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SongRequest/{id?}")]HttpRequest req,
            ILogger log, string id)
        {
            List<SongRequest> AllRequests = new List<SongRequest>();
            string qCount = req.Query["count"];
            if (qCount == null)
            {
                qCount = "20";
            }

     

            //controleren of userId niet kleinder is als 0 en op input
            if (!int.TryParse(id, out int Id) || Id < 0)
            {
                return new BadRequestObjectResult("Invalid input");
            }



            var sqlAllRequests =
                $"SELECT TOP {qCount} sr.Id AS RequestId,sr.DateTime ,sr.UserId, sr.Title, sr.Artist," +
                " CASE WHEN uv.Upvotes IS NULL THEN 0 ELSE uv.Upvotes END as Upvotes, CASE WHEN dv.Downvotes IS NULL THEN 0 ELSE dv.Downvotes END as Downvotes " +
                "FROM SongRequest AS sr " +
                "LEFT JOIN( " +
                "SELECT d.RequestId AS RequestID, COUNT(UserId) AS Downvotes FROM SongRequestUserVotes AS d WHERE d.Vote< 0 GROUP BY " +
                "d.RequestId, d.Vote) as dv ON dv.RequestID = sr.Id " +
                "LEFT JOIN( " +
                "SELECT u.RequestId AS RequestID, COUNT(UserId) AS Upvotes FROM SongRequestUserVotes AS u WHERE u.Vote > 0 GROUP BY " +
                "u.RequestId, u.Vote ) as uv ON sr.Id = uv.RequestID ";

            var sqlWhere = $" WHERE sr.Id = {id}";

       

            //controleren op {id} Als het bestaat Add where op query
            if (id != null)
            {
                sqlAllRequests = sqlAllRequests + sqlWhere;
            }


            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlAllRequests, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    //List<Event> eventsList = new List<Event>();

                    AllRequests.Add(new Model.SongRequest()
                    {
                        Id = Convert.ToInt32(reader["RequestId"]),
                        Title = reader["Title"].ToString(),
                        Artist = reader["Artist"].ToString(),
                        DateTime = DateTime.Parse(reader["DateTime"].ToString()),
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Upvotes = Convert.ToInt32(reader["Upvotes"]),
                        Downvotes = Convert.ToInt32(reader["Downvotes"])
                    }
                    );

                }
            }

            DBConnect.Dispose(conn);
            string j = JsonConvert.SerializeObject(AllRequests);

            return AllRequests.Count > 0
                ? (ActionResult)new OkObjectResult(j)
                : new NotFoundObjectResult("No songs were found");
        }

        [FunctionName("DeleteSongRequest")]
        public static async Task<IActionResult> DeleteSongRequest(
            [HttpTrigger(AuthorizationLevel.Function, "Delete", Route = "SongRequest/{id}/")]
            HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE SongRequest WHERE Id = '{id}'";
            SqlConnection conn = DBConnect.GetConnection();



            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {


                try
                {
                    //await = gaat pas verder als taks is afgerond
                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    if (affectedRows == 0)
                    {
                        return new NotFoundObjectResult(
                            $"Deleting Songrequest failed: reward with id {id} does not exist!");
                    }

                    return (ActionResult)new OkObjectResult("Sucessfully deleted the Songrequest");
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult("invalid id"+e.Message);
                }
            }

          
        }

        [FunctionName("AddSongRequest")]
        public static async Task<HttpResponseMessage> AddSongRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SongRequest/Add")]
            HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            List<string> missingFields = new List<string>();

            // Read data from input
            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            string Title = formData["Title"];
            string Artist = formData["Artist"];
            string UserId = formData["UserId"];


            // Queries
            var sqlStr =
                $"INSERT INTO SongRequest (Title, Artist, DateTime, UserId) " +
                $"VALUES ('{Title}', '{Artist}', '{DateTime.UtcNow}', '{UserId}')";



            //Checks if the input fields are filled in
            if (Title == null)
            {
                missingFields.Add("Title");
            }

            if (UserId == null)
            {
                missingFields.Add("UserId");
            }

            if (Artist == null)
            {
                missingFields.Add("Artist");
            }

            // Returns bad request if one of the input fields are not filled in
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }
            //controleren of userId niet kleinder is als 0 
            if (!int.TryParse(UserId, out int userId) || userId < 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"UserId is not a valid number.");
            }


            ////Connects with the database
            SqlConnection conn = DBConnect.GetConnection();
    

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch(Exception e)
                {
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Creating song request failed: User does not exist!" + e.Message);
                }
                
            }

            // Close the database connection
            DBConnect.Dispose(conn);
            return req.CreateResponse(HttpStatusCode.OK, "Successfully added the song request");


        }
    }
}
