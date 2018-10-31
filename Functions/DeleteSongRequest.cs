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

namespace GildtAPI.Functions
{
    public static class DeleteSongRequest
    {
        [FunctionName("DeleteSongRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Request/Delete/{id}")] HttpRequest req,
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
