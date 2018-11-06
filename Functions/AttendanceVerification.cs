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
            Route = "Attendance/{eventId}/Verify/{userId}")] HttpRequest req, ILogger log,
            int userId, int eventId)
        {
            // Queries
            //Query to insert new row into AttendanceVerification
            var sqlStr =
            "INSERT INTO AttendanceVerification " +
                $"(UserId, EventId) " +
            "VALUES " +
                $"('{userId}', '{eventId}')";
            //Get query to check if verification already exists
            var sqlGet =
            "SELECT COUNT(*) FROM AttendanceVerification " +
            $"WHERE (UserId = '{userId}' AND EventId = '{eventId}')"; ;

            SqlConnection conn = DBConnect.GetConnection();
            //check if verification exists
            SqlCommand checkVer = new SqlCommand(sqlGet, conn);
            checkVer.Parameters.AddWithValue("UserId", userId);
            checkVer.Parameters.AddWithValue("EventId", eventId);
            int existingVer = (int)await checkVer.ExecuteScalarAsync();
            if (existingVer > 0)
            {
                // Close the database connection
                DBConnect.Dispose(conn);
                return new BadRequestObjectResult($"Verification already exists for user {userId} and event {eventId}");
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Close the database connection
                DBConnect.Dispose(conn);

                return new OkObjectResult($"Verification succesfully created for {userId}");
            }
        }

/*        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(CheckVerifications))]
        public static async Task<IActionResult> CheckVerifications([HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "Attendance/{eventId?}")] HttpRequest req, ILogger log,
            int? eventId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(CheckVerifications));

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
        */
        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(DeleteVerification))]
        public static async Task<IActionResult> DeleteVerification([HttpTrigger(AuthorizationLevel.Function, "delete",
            Route = "Attendance/{eventId}/Delete/{userId}")] HttpRequest req, ILogger log,
            int userId, int eventId){
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(DeleteVerification)}");
            if (eventId < 1 || userId < 1)
            {
                return new BadRequestObjectResult("Invalid parameters.");
            }

            // Queries
            var sqlStr =
            "DELETE FROM AttendanceVerification " +
            $"WHERE UserId = {userId} AND EventId = {eventId}";
            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    int affectedRows = await cmd.ExecuteNonQueryAsync();

                    if (affectedRows == 0)
                    {
                        DBConnect.Dispose(conn);
                        return new BadRequestObjectResult($"Deleting verification failed: verification with UserId {userId} " +
                            $"and EventId {eventId} does not exist!");
                    }
                    if (affectedRows > 1)
                    {
                        //multiple rows affected: something went wrong
                        log.LogInformation($"Deleted multiple rewards when executing query to delete single verification: " +
                            $"UserId = {userId}, EventId = {eventId}");
                    }
                }
                catch (Exception e)
                {
                    return new BadRequestObjectResult($"SQL query failed: {e.Message}");
                }
            }
            DBConnect.Dispose(conn);

            return new OkObjectResult("Successfully deleted the verification.");
        }
    }
}
