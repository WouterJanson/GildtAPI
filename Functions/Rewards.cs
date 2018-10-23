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
using System.Collections.Generic;

namespace GildtAPI
{
    public static class Rewards
    {
        [FunctionName("Rewards")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log) {

            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(Rewards));

            string qUserId = req.Query["User"];
            int userId = -1;
            if (!Int32.TryParse(qUserId, out userId))
            {
                //userId not a number
                return new BadRequestObjectResult("UserId is not a number. What are you doing?");
            }
            string sqlQuery = "SELECT TOP 100 Rewards.Id, Rewards.Name, Rewards.Description " +
                "FROM[GildtDB].[dbo].[UsersRewards] " +
                "INNER JOIN Rewards ON dbo.UsersRewards.RewardId = Rewards.Id" +
                "INNER JOIN Users ON UserId = Users.Id" +
                $"WHERE Users.Id = {userId}";

            SqlConnection conn = DBConnect.GetConnection();

            List<Reward> rewardsList = new List<Reward>();
            using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
            {

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                    rewardsList.Add(
                        new Reward()
                        {
                            Id = int.Parse(reader["Id"].ToString()),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString()
                        }
                    );
                }
            }
            if (rewardsList.Count == 0)
            {
                return (ActionResult)new OkObjectResult("No rewards for this user.");
            }
            string rewardsJson = JsonConvert.SerializeObject(rewardsList.ToArray());
            return (ActionResult)new OkObjectResult(rewardsJson);
        }
    }
}
