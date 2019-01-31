using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using GildtAPI.DAO;
using GildtAPI.Model;

namespace GildtAPI.Functions
{
    public static class AttendanceVerification
    {
        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(GetVerificationsAsync))]
        public static async Task<IActionResult> GetVerificationsAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "Attendance/{eventId?}")] HttpRequest req, ILogger log,
            int? eventId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetVerificationsAsync));

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

            List<Attendance> attendanceList = await AttendanceDAO.Instance.GetAttendanceListAsync(eventId, count);
            if (attendanceList.Count == 0)
            {
                return new BadRequestObjectResult($"No verifications found for event with id = {eventId}");
            }
            string jAttendance = JsonConvert.SerializeObject(attendanceList.ToArray());
            return new OkObjectResult(jAttendance);
        }

        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(GetUserVerificationsAsync))]
        public static async Task<IActionResult> GetUserVerificationsAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "User/{userId}/Attendance/")] HttpRequest req, ILogger log,
            int userId)

        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetUserVerificationsAsync));

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

            List<Attendance> attendanceList = await AttendanceDAO.Instance.GetUserAttendanceListAsync(userId, count);
            if (attendanceList.Count == 0)
            {
                return new BadRequestObjectResult($"No verifications found for user#{userId}");
            }
            string jAttendance = JsonConvert.SerializeObject(attendanceList.ToArray());
            return new OkObjectResult(jAttendance);
        }

        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(VerifyAsync))]
        public static async Task<IActionResult> VerifyAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "Attendance/{eventId}/Verify/{userId}")] HttpRequest req, ILogger log,
            int userId, int eventId)
        {
            //check if verification exists
            if (await AttendanceDAO.Instance.CheckVerificationAsync(userId, eventId))
            {
                return new BadRequestObjectResult($"Verification already exists for user #{userId} at event #{eventId}");
            }
            else
            {
                try
                {
                    await AttendanceDAO.Instance.CreateVerificationAsync(userId, eventId);
                }
                catch
                {
                    return new BadRequestObjectResult($"Something went wrong while trying to add verification for user #{userId} at event {eventId}");
                }

                return new OkObjectResult($"Verification succesfully created for {userId} at event {eventId}");
            }
        }

        [FunctionName(nameof(AttendanceVerification) + "-" + nameof(DeleteVerificationAsync))]
        public static async Task<IActionResult> DeleteVerificationAsync([HttpTrigger(AuthorizationLevel.Anonymous, "delete",
            Route = "Attendance/{eventId}/Delete/{userId}")] HttpRequest req, ILogger log,
            int userId, int eventId)
        {
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(DeleteVerificationAsync)}");
            if (eventId < 1 || userId < 1)
            {
                return new BadRequestObjectResult("Invalid parameters.");
            }
            int affectedRows;
            try
            {
                affectedRows = await AttendanceDAO.Instance.DeleteVerificationAsync(userId, eventId);
            }
            catch
            {
                affectedRows = -1;
            }
            switch (affectedRows)
            {
                case 1:
                    return new OkObjectResult($"Successfully deleted the verification for user#{userId} at event #{eventId}");
                case 0:
                    return new BadRequestObjectResult($"Deleting verification failed: verification with UserId {userId} " +
                        $"and EventId {eventId} does not exist!");
                case -1:
                    return new BadRequestObjectResult($"SQL query failed: delete verification for user#{userId} at event#{eventId}");
                    break;
                default:
                    return new OkObjectResult("Deleted the verification. Duplicate verification was found and also deleted.");
            }
        }
    }
}
