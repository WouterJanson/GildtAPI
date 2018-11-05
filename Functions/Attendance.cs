using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GildtAPI.Model;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace GildtAPI.Functions
{
    public static class Attendance
    {
        [FunctionName(nameof(Attendance) + " - " + nameof(GetVerifications))]
        public static async Task<IActionResult> GetVerifications([HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "Attendance/{userId?}")] HttpRequest req, ILogger log,
            int? eventId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetVerifications));
            List<User> users = new List<User>();
            List<UsersCoupon> couponsList = new List<UsersCoupon>();

            string qCount = req.Query["count"];
            //Default count number - Used to select number of rows
            if (qCount == null)
            {
                qCount = "20";
            }

            var sqlStrUsers = $"SELECT TOP {qCount} * FROM AttendanceVerification as att " +
                $"INNER JOIN Users ON att.UserId = Users.Id ";
            var sqlWhere = $"WHERE Id = {eventId}";

            // Checks if an id parameter is filled in
            if (eventId != null)
            {
                sqlStrUsers = sqlStrUsers + sqlWhere;
            }

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();
            throw new NotImplementedException();
            return new OkObjectResult("");
        }
        [FunctionName(nameof(Attendance) + " - " + nameof(Verify))]
        public static async Task<IActionResult> Verify([HttpTrigger(AuthorizationLevel.Function, "post", 
            Route = "Attendance/{userId}/Verify")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

    }
}
