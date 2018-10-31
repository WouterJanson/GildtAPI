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
using GildtAPI.Model;

namespace GildtAPI
{
    public static class SongRequest
    {
        [FunctionName("SongRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get",Route ="SongRequest/{id?}")] HttpRequest req,
            ILogger log,string id)
        {
            List<SongRequests> AllRequests = new List<SongRequests>();
            string qCount = req.Query["count"];
            if (qCount == null)
            {
                qCount = "20";
            }

            var sqlAllRequests = $"SELECT TOP {qCount} SongRequest.Id AS SongRequestId ,SongRequest.Title ,SongRequest.Artist," +
                                 "SongRequest.DateTime,SongRequest.Username FROM SongRequest"; 

            var sqlUpvotes = "SELECT SongRequest.Title, COUNT(SongRequestUpvotes.UserId) AS CountUpvotes" +
                             " FROM SongRequestUpvotes INNER JOIN SongRequest ON SongRequestUpvotes.RequestId = SongRequest.Id" +
                             "GROUP BY SongRequest.Title";

            var sqlDownvotes = "SELECT SongRequest.Title, COUNT(SongRequestDownvotes.UserId) AS CountDownvotes " +
                               "FROM SongRequestDownvotes INNER JOIN SongRequest ON SongRequestDownvotes.RequestId = SongRequest.Id" +
                               "GROUP BY SongRequest.Title";

            var sqlWhere = $" WHERE SongRequest.Id = {id}";

            if (id != null)
            {
                sqlAllRequests = sqlAllRequests + sqlWhere;
            }


            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlAllRequests, conn))
            {
                 SqlDataReader reader = cmd.ExecuteReader();

                

                while (reader.Read())
                {
                    //List<Event> eventsList = new List<Event>();

                    AllRequests.Add(new SongRequests()
                        {
                            Id = Convert.ToInt32(reader["SongRequestId"]),
                            Title = reader["Title"].ToString(),
                            Artist = reader["Artist"].ToString(),
                            DateTime = DateTime.Parse(reader["DateTime"].ToString()),
                            Username = reader["Username"].ToString(),
                            Upvotes = Convert.ToInt32(sqlUpvotes),
                            Downvotes = Convert.ToInt32(sqlDownvotes)
                    }
                    );

                }

            }
            DBConnect.Dispose(conn);
            string j = JsonConvert.SerializeObject(AllRequests);

                return AllRequests != null
                ? (ActionResult)new OkObjectResult(j)
                : new BadRequestObjectResult("No songs were found");
        }

        [FunctionName("DeleteSongRequest")]
        public static async Task<IActionResult> DeleteSongRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Request/Delete/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE SongRequest WHERE Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();


            try
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.ExecuteNonQuery();
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the user");
                }
            }
            catch (InvalidCastException e)
            {
                return (ActionResult)new BadRequestObjectResult(e);
            }
        }
    }
}
