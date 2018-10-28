using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace Company.Function
{
    public static class DeleteUser
    {
        [FunctionName("DeleteUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Users/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            var sqlStr = $"DELETE User WHERE Id = '{id}'";
            
            SqlConnection conn = DBConnect.GetConnection();

            try
            {
                 using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
                 {
                    cmd.ExecuteNonQuery();
                    DBConnect.Dispose(conn);
                    return (ActionResult)new OkObjectResult("Sucessfully deleted the user");
                 }
            }
            catch(InvalidCastException e)
            {
                return (ActionResult)new BadRequestObjectResult(e);
            }
        }
    }
}
