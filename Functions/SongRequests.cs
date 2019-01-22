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
                return req.CreateResponse(HttpStatusCode.NotFound, "Songrequest does not exist");
            }
            return req.CreateResponse(HttpStatusCode.OK, songRequests);
        }

        [FunctionName("DeleteSongRequest")]
        public static async Task<HttpResponseMessage> DeleteSongRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Delete", Route = "SongRequest/{id}/")]
            HttpRequestMessage req,
            ILogger log, string id)
        {
            try
            {
                int convId = Convert.ToInt32(id);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id");
            }

            int i = await SongRequestController.Instance.Delete(Convert.ToInt32(id));
            if (i == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Songrequest does not exist");
            }
            return req.CreateResponse(HttpStatusCode.OK, "succesfully deleted songrequest");


        }

        [FunctionName("AddSongRequest")]
        public static async Task<HttpResponseMessage> AddSongRequest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/Add")]
            HttpRequestMessage req,
            ILogger log)
        {

        
            SongRequest song = new SongRequest();

            List<string> missingFields = new List<string>();

            

            NameValueCollection formData = req.Content.ReadAsFormDataAsync().Result;
            if (!GlobalFunctions.CheckValidId(formData["UserId"]))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }
            song.Title = formData["Title"];
            song.Artist = formData["Artist"];
            song.DateTime = Convert.ToDateTime(formData["DateTime"]);
            song.UserId = Convert.ToInt32(formData["UserId"]);

            bool input = GlobalFunctions.CheckInputs(song.Title, song.Artist, song.DateTime.ToString(),song.UserId.ToString());
            if (!input)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Fields are not filled in.", "application/json");
            }
           
            int rowsAffected = await SongRequestController.Instance.AddSongRequest(song);
            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Error couldn't add, check input fields.", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.OK, "Successfully added songrequest .", "application/json");


        }

        [FunctionName("UpVotesSongRequest")]
        public static async Task<HttpResponseMessage> UpvoteSongRequest(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/{RequestId}/{UserId}/upvote")]
            HttpRequestMessage req, string RequestId, string UserId,
           ILogger log)
        {
            // Check if id is valid
            if (!GlobalFunctions.CheckValidId(UserId, RequestId))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }

            int rowsAffected = await SongRequestController.Instance.UpVote(Convert.ToInt32(RequestId), Convert.ToInt32(UserId));
            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Error vote", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.OK, "Successfully Voted.", "application/json");

        }

        [FunctionName("DownVotesSongRequest")]
        public static async Task<HttpResponseMessage> DownvotesSongRequest(
              [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SongRequest/{RequestId}/{UserId}/downvote")]
            HttpRequestMessage req, string RequestId, string UserId,
              ILogger log)
        {
            if (!GlobalFunctions.CheckValidId(UserId, RequestId))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Id", "application/json");
            }
            int rowsAffected = await SongRequestController.Instance.Downvote(Convert.ToInt32(RequestId), Convert.ToInt32(UserId));
            if (rowsAffected == 0)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Error vote", "application/json");
            }

            return req.CreateResponse(HttpStatusCode.OK, "Successfully Voted.", "application/json");

        }
    }
}
