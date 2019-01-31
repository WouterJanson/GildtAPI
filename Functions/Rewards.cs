using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using GildtAPI.Model;
using GildtAPI.DAO;

namespace GildtAPI.Functions
{
    public static class Rewards
    {
        [FunctionName(nameof(Rewards) + "-" + nameof(GetRewardsAsync))]
        public static async Task<IActionResult> GetRewardsAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", 
            Route = nameof(Rewards))] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetRewardsAsync));
            string qCount = req.Query["count"];
            string qRewardName = req.Query["name"];
            string qRewardId = req.Query["id"];
            string rewardsJson;

            bool validId = int.TryParse(qRewardId, out int rewardId);
            bool validName = !String.IsNullOrWhiteSpace(qRewardName);

            if (!validId && !validName)
            {
                //no name or ID: return all rewards
                int count = Constants.DEFAULTCOUNT;
                if (qCount != null)
                {
                    Int32.TryParse(qCount, out count);
                    if (count < 1)
                    {
                        return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                    }
                }
                Reward[] rewards = await RewardsDAO.Instance.GetAllRewardsAsync();
                rewardsJson = JsonConvert.SerializeObject(rewards);
                return new OkObjectResult(rewardsJson);
            }
            else if (!validId && validName)
            {
                //No rewardId entered: get reward by name
                var reward = await RewardsDAO.Instance.GetRewardByNameAsync(qRewardName);
                if (reward == null)
                {
                    return new NotFoundObjectResult("No rewards with this name.");
                }
                rewardsJson = JsonConvert.SerializeObject(reward);
            }
            else
            {
                Reward reward = await RewardsDAO.Instance.GetRewardByIdAsync(rewardId);
                if (reward == null)
                {
                    return new NotFoundObjectResult("Reward not found.");
                }
                rewardsJson = JsonConvert.SerializeObject(reward);
            }

            return new OkObjectResult(rewardsJson);

        }

        [FunctionName(nameof(Rewards) + "-" + nameof(GetRewardsForUserAsync))]
        public static async Task<IActionResult> GetRewardsForUserAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", 
            Route = "User/{userId}/Rewards")] HttpRequest req, ILogger log, 
            string userId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetRewardsForUserAsync));
            string qCount = req.Query["count"];
            int count = Constants.DEFAULTCOUNT;
            if (qCount != null)
            {
                if (!Int32.TryParse(qCount, out count) || count < 1)
                {
                    return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                }
            }
            if(!int.TryParse(userId, out int id))
            {
                return new BadRequestObjectResult("Invalid input");
            }
            Reward[] userRewards = await RewardsDAO.Instance.GetUserRewardsAsync(count, id);

            if (userRewards.Length == 0)
            {
                return (ActionResult)new OkObjectResult("No rewards for this user.");
            }
            string rewardsJson = JsonConvert.SerializeObject(userRewards);
            return (ActionResult)new OkObjectResult(rewardsJson);
        }


        [FunctionName(nameof(Rewards) + "-" + nameof(CreateRewardAsync))]
        public static async Task<IActionResult> CreateRewardAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", 
            Route = "Rewards/Create")] HttpRequest req, ILogger log){
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(CreateRewardAsync)}");
            // Read data from input
            //var data = req.Content.ReadAsStringAsync().Result;
            string name = req.Query["name"];
            string description = req.Query["description"];


            // Returns bad request if one of the input fields are not filled in
            bool nameMissing = String.IsNullOrEmpty(name);
            bool descMissing = String.IsNullOrEmpty(description);
            if (nameMissing || descMissing)
            {
                string missingFieldsSummary = "Missing fields: " +
                    (nameMissing
                    ? (descMissing
                        ? "name, description"
                        : "name")
                    : "description");
                return new BadRequestObjectResult(missingFieldsSummary);
            }
            bool success = await RewardsDAO.Instance.CreateRewardAsync(name, description);
            if (success)
            {
                return new OkObjectResult($"Reward \"{name}\" created!");
            }
            else return new BadRequestObjectResult($"Reward \"{name}\" already exists!");
        }

        [FunctionName(nameof(Rewards) + "-" + nameof(EditRewardAsync))]
        public static async Task<IActionResult> EditRewardAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put", 
            Route = "Rewards/{rewardId}/Edit")] HttpRequest req, ILogger log, 
            int rewardId)
        {
            string name = req.Query["name"];
            string description = req.Query["description"];

            if (String.IsNullOrWhiteSpace(name) && description == null)
            {
                return new BadRequestObjectResult("No name or description entered.");
            }
            if (rewardId < 1)
            {
                return new BadRequestObjectResult("Invalid id.");
            }

            int rowsaffected;
            try
            {
                rowsaffected = await RewardsDAO.Instance.EditRewardAsync(rewardId, name, description);
            }
            catch(Exception e)
            {
                return new BadRequestObjectResult($"Editing reward failed: {e.Message}");
            }
            switch (rowsaffected)
            {
                case 0:
                    return new BadRequestObjectResult($"No reward with id {rewardId} exists!");
                case 1:
                    return new OkObjectResult("Successfully edited the reward.");
                default:
                    return new BadRequestObjectResult($"Something went wrong while editing reward #{rewardId}");
            }

        }

        [FunctionName( nameof(Rewards) + "-" + nameof(DeleteRewardAsync))]
        public static async Task<IActionResult> DeleteRewardAsync([HttpTrigger(AuthorizationLevel.Anonymous, "delete", 
            Route = "Rewards/{rewardId}/Delete")] HttpRequest req, ILogger log,
            int rewardId)
        {
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(DeleteRewardAsync)}");
            
            if (rewardId < 1)
            {
                return new BadRequestObjectResult("Invalid rewardId parameter.");
            }

            // Queries
            var sqlStr =
            "DELETE FROM Rewards " +
            $"WHERE Id = {rewardId}";
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
                        return new BadRequestObjectResult($"Deleting reward failed: reward with id {rewardId} does not exist!");
                    }
                    if (affectedRows > 1)
                    {
                        //multiple rows affected: something went wrong
                        log.LogInformation($"Deleted multiple rewards when executing query to delete single reward: RewardId = {rewardId}");
                    }
                }
                catch(Exception e)
                {
                    return new BadRequestObjectResult($"SQL query failed: {e.Message}");
                }
            }
            DBConnect.Dispose(conn);

            return new OkObjectResult("Successfully deleted the reward.");
        }
    }
}