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
    public static class AttendanceVerification
    {
        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(GetVerifications))]
        public static async Task<IActionResult> GetVerifications([HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "Attendance/{eventId?}")] HttpRequest req, ILogger log,
            int? eventId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetVerifications));
            List<User> users = new List<User>();
            List<UsersCoupon> couponsList = new List<UsersCoupon>();

            string qCount = req.Query["count"];
            int count;
            if (qCount != null)
            {
                Int32.TryParse(qCount, out count);
                if (count < 1)
                {
                    return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                }
            }
            else
            {
                count = Constants.DEFAULTCOUNT;
            }

            var sqlAttendance =
                $"SELECT TOP {count} " +
                    $"att.EventId AS EventId, " +
                    $"Users.Id as UserId, " +
                    $"Users.Username AS Username " +
                $"FROM AttendanceVerification as att " +
                $"INNER JOIN Users " +
                    $"ON att.UserId = Users.Id ";
            var sqlWhere = $"WHERE att.EventId = {eventId}";
            // Checks if an id parameter is filled in
            if (eventId != null)
            {
                //Add WHERE if id parameter exists
                sqlAttendance = sqlAttendance + sqlWhere;
            }

            List<Attendance> attendanceList = new List<Attendance>();

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlAttendance, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {

                    attendanceList.Add(
                        new Attendance(
                            int.Parse(reader["UserId"].ToString()), 
                            int.Parse(reader["EventId"].ToString()), 
                            reader["Username"].ToString()));
                }
            }
            if (attendanceList.Count == 0)
            {
                return new BadRequestObjectResult($"No verifications found for event with id = {eventId}");
            }
            string jAttendance = JsonConvert.SerializeObject(attendanceList.ToArray());
            return new OkObjectResult(jAttendance);
        }
        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(Verify))]
        public static async Task<IActionResult> Verify([HttpTrigger(AuthorizationLevel.Function, "post", 
            Route = "Attendance/{userId}/Verify")] HttpRequest req, ILogger log,
            int userId)
        {
            throw new NotImplementedException();
        }

    }
}
