using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Net;
using System.Linq;
using GildtAPI.Model;
using GildtAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GildtAPI.Functions
{
    public static class SongRequests
    {
        [FunctionName("SongRequests")]
        public static async Task<HttpResponseMessage> GetSongRequests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SongRequest")]HttpRequestMessage req,
            ILogger log)
        {

            List<SongRequest> songRequests = await SongRequestController.Instance.GetAll();
            string json = JsonConvert.SerializeObject(songRequests);

            return songRequests.Count >= 1
                ? req.CreateResponse(HttpStatusCode.OK, songRequests, "application/json")
                : req.CreateResponse(HttpStatusCode.BadRequest, "", "application/json");

        }
        [FunctionName("SongRequest")]
        public static async Task<HttpResponseMessage> GetSongRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SongRequest/{id}")]HttpRequestMessage req,
            ILogger log, string id)
        {

            //List<SongRequest> songRequests = await SongRequestController.Instance.GetAll();

            try
            {
                int convId = Convert.ToInt32(id);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id");
            }

     
            SongRequest songRequests = await SongRequestController.Instance.Get(Convert.ToInt32(id));

            if (songRequests == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "Event does not exist");
            }

            return req.CreateResponse(HttpStatusCode.OK, songRequests);
        }

        [FunctionName("DeleteSongRequest")]
        public static async Task<IActionResult> DeleteSongRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Delete", Route = "SongRequest/{id}/")]
            HttpRequest req,
            ILogger log, string id)
        {

       
            var sqlStr = $"DELETE SongRequest WHERE Id = '{id}'";
            SqlConnection conn = DBConnect.GetConnection();
         


            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {


                try
                {
                    //wacht op query tot het is afgerond geef waarde aan affectedrows
                    int affectedRows = await cmd.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    // controleren of er rows zijn aangepast in de database zo niet return 404
                    if (affectedRows == 0)
                    {
                        return new NotFoundObjectResult(
                            $"Deleting Songrequest failed: reward with id {id} does not exist!");
                    }
                    //rows aangepast return 200
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the Songrequest");
                }
                catch (Exception e)
                {
                    //return 400 als er een invalid object is > "jjjj"
                    return new BadRequestObjectResult("invalid id " + e.Message);
                }
            }


        }

        [FunctionName("AddSongRequest")]
        public static async Task<HttpResponseMessage> AddSongRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/Add")]
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


            // Queries vul database met  input
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

            // Als er velden ontbreken return 400 badrequest
            if (missingFields.Any())
            {
                string missingFieldsSummary = String.Join(", ", missingFields);
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Missing field(s): {missingFieldsSummary}");
            }
            //controleren of userId niet kleinder is als 0 of als input niet numeric zijn
            if (!int.TryParse(UserId, out int userId) || userId < 0)
            {
                //return 400 
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
                catch (Exception e)
                {
                    //bestaat gebruiker niet return 400
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Creating song request failed: User does not exist! " + e.Message);
                }

            }



            // Close the database connection
            DBConnect.Dispose(conn);
            //bestaat gebruiker wel return 200
            return req.CreateResponse(HttpStatusCode.OK, "Successfully added the song request");


        }

        [FunctionName("UpVotesSongRequest")]
        public static async Task<HttpResponseMessage> UpvoteSongRequest(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/{RequestId}/{UserId}/upvote")]
            HttpRequestMessage req, string RequestId, string UserId,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            int vote = 1;

            //rij toevoegen als die nog niet bestaat
            var sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values ('{RequestId}', '{UserId}', '{vote}')";

            //alle songrequests met een upvote
            var sqlGet1 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '1' ";
            //alle songrequests met een downvote
            var sqlGet2 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '-1' ";

            //update downvote naar upvote
            var sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = {vote} " +
                                $" WHERE RequestId = {RequestId} AND UserId = {UserId};";


            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();


            using (SqlCommand cmd2 = new SqlCommand(sqlGet1, conn))
            //check if user have already upvoted
            using (SqlDataReader reader = cmd2.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.BadRequest, "You've already up voted for this song");
                }
                reader.Close();
            }

            // Update usersvote to upvote if it where downvote
            using (SqlCommand cmd3 = new SqlCommand(sqlGet2, conn))

            using (SqlDataReader reader = cmd3.ExecuteReader())
            {
                // controlleer of er al een vote was in dit geval downvote
                if (reader.HasRows)
                {
                    reader.Close();
                    // update database row
                    using (SqlCommand cmd4 = new SqlCommand(sqlUpdateVote, conn))
                    {
                        cmd4.ExecuteReader();
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.OK, "User has now a Upvote for this song");

                    }
                }
            }


            // insert a usersvote to a song
            using (SqlCommand cmd5 = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    cmd5.ExecuteReader();
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.OK, "User has upvoted this song (NEW)");
                }
                catch (Exception e)
                {
                    return req.CreateErrorResponse(HttpStatusCode.NotFound, "An error has occured  " + e.Message);
                }
                
            }

        }

        [FunctionName("DownVotesSongRequest")]
        public static async Task<HttpResponseMessage> DownvotesSongRequest(
              [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/{RequestId}/{UserId}/downvote")]
            HttpRequestMessage req, string RequestId, string UserId,
              ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            int vote = -1;

            //controleren of opgegeven request id niet kleiner dan 0 is en numeric is
            if (!int.TryParse(RequestId, out int requestId) || requestId < 0)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "An error has occured - request id");
            }
            //controleren of opgegeven user id niet kleiner dan 0 is en numeric is
            if (!int.TryParse(UserId, out int userId) || userId < 0)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "An error has occured - user id ");
            }

            //rij toevoegen als die nog niet bestaat

            var sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values ('{RequestId}', '{UserId}', '{vote}')";

            //alle songrequests met een downvote
            var sqlGet1 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '-1' ";
            //alle songrequests met een upvote
            var sqlGet2 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '1' ";
            //update van upvote naar downvote
            var sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = {vote} " +
                                $" WHERE RequestId = {RequestId} AND UserId = {UserId};";


            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();


            using (SqlCommand cmd2 = new SqlCommand(sqlGet1, conn))
            //check if user have already upvoted
            using (SqlDataReader reader = cmd2.ExecuteReader())
            {

                if (reader.HasRows)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.BadRequest, "You've already downvoted for this song");
                }
                reader.Close();

            }

            // Update usersvote to upvote if it where downvote
            using (SqlCommand cmd3 = new SqlCommand(sqlGet2, conn))

            using (SqlDataReader reader = cmd3.ExecuteReader())
            {

                // controlleer of er al een vote was in dit geval downvote
                if (reader.HasRows)
                {
                    reader.Close();
                    // update database row
                    using (SqlCommand cmd4 = new SqlCommand(sqlUpdateVote, conn))
                    {
                        cmd4.ExecuteReader();
                        DBConnect.Dispose(conn);
                        return req.CreateResponse(HttpStatusCode.OK, "User has now a downvote for this song");

                    }

                }
                
            }


            // insert a usersvote to a song
            using (SqlCommand cmd5 = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    cmd5.ExecuteReader();
                    DBConnect.Dispose(conn);
                    return req.CreateResponse(HttpStatusCode.OK, "User has downvoted this song (NEW)");

                }
                catch (Exception e)
                {
                    return req.CreateErrorResponse(HttpStatusCode.NotFound, "An error has occured  " + e.Message);
                }

            }

        }
    }
}
