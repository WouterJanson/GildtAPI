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

namespace GildtAPI
{
    public static class SongRequest
    {
        [FunctionName("SongRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var sqlUpvotes = "SELECT SongRequest.Title, COUNT(SongRequestUpvotes.UserId) AS CountUpvotes" +
                             " FROM SongRequestUpvotes INNER JOIN SongRequest ON SongRequestUpvotes.RequestId = SongRequest.Id" +
                             "GROUP BY SongRequest.Title";

            var sqlDownvotes = "SELECT SongRequest.Title, COUNT(SongRequestDownvotes.UserId) AS CountDownvotes " +
                               "FROM SongRequestDownvotes INNER JOIN SongRequest ON SongRequestDownvotes.RequestId = SongRequest.Id" +
                               "GROUP BY SongRequest.Title";



            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlDownvotes,conn))
            {

                List<SongRequestDownvotes> downvotes = new List<SongRequestDownvotes>();

                downvotes.Add(
                    new SongRequestDownvotes());

            }

            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }



    }
}
