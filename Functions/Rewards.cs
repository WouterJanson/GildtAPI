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
using System.Net.Http;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using GildtAPI.Model;

namespace GildtAPI.Functions
{
    public static class Rewards
    {
        
        #region Functions

        [FunctionName("GetAllRewards")]
        public static async Task<IActionResult> GetAllRewards([HttpTrigger(AuthorizationLevel.Anonymous, "get", 
            Route = nameof(Rewards) + "/All")] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetAllRewards));
            string qCount = req.Query["count"];
            int count = Constants.DEFAULTCOUNT;
            if (qCount != null)
            {
                Int32.TryParse(qCount, out count);
                if (count < 1)
                {
                    return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                }
            }
            Reward[] rewards = await GetAllRewards();
            string rewardsJson = JsonConvert.SerializeObject(rewards);
            return new OkObjectResult(rewardsJson);
        }

        [FunctionName(nameof(Rewards) + "-" + nameof(GetRewardsForUser))]
        public static async Task<IActionResult> GetRewardsForUser([HttpTrigger(AuthorizationLevel.Anonymous, "get", 
            Route = "User/{userId}/Rewards")] HttpRequest req, ILogger log, 
            string userId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetRewardsForUser));
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
            Reward[] userRewards = await GetUserRewards(count, id);

            if (userRewards.Length == 0)
            {
                return (ActionResult)new OkObjectResult("No rewards for this user.");
            }
            string rewardsJson = JsonConvert.SerializeObject(userRewards);
            return (ActionResult)new OkObjectResult(rewardsJson);
        }
        
        [FunctionName(nameof(Rewards) + "-" + nameof(GetSingleReward))]
        public static async Task<IActionResult> GetSingleReward([HttpTrigger(AuthorizationLevel.Anonymous, "get", 
            Route = "Rewards")] HttpRequest req, ILogger log){

            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetSingleReward));
            
            string qRewardName = req.Query["rewardName"];
            string qRewardId = req.Query["rewardId"];

            if (qRewardId == null && String.IsNullOrWhiteSpace(qRewardName))
            {
                //no valid rewardid or rewardname
                return new BadRequestObjectResult("Missing parameters. Add a rewardId or rewardName parameter.");
            }


            if (qRewardId == null && qRewardName != null)
            {
                //No rewardId entered: get reward by name
                if (!String.IsNullOrWhiteSpace(qRewardName))
                {
                    //
                    var reward = await GetRewardByName(qRewardName);
                    if (reward == null)
                    {
                        return new NotFoundObjectResult("No rewards with this name.");
                    }
                    return new OkObjectResult(reward);
                }
            }
            else if (int.TryParse(qRewardId, out int rewardId))
            {
                Reward reward = await GetRewardById(rewardId);
                if (reward == null)
                {
                    return new NotFoundObjectResult("Reward not found.");
                }
                string rewardsJson = JsonConvert.SerializeObject(reward);
                return new OkObjectResult(rewardsJson);
            }

            return new BadRequestObjectResult("No valid id entered");

        }

        [FunctionName(nameof(Rewards) + "-" + nameof(CreateReward))]
        public static async Task<IActionResult> CreateReward([HttpTrigger(AuthorizationLevel.Anonymous, "post", 
            Route = "Rewards/Create")] HttpRequest req, ILogger log){
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(CreateReward)}");
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
            bool success = await CreateReward(name, description);
            if (success) return new OkObjectResult(
                JsonConvert.SerializeObject(new Reward() { Name = name, Description = description }));
            else return new BadRequestObjectResult($"Reward named {name} already exists!");
        }
        
        [FunctionName( nameof(Rewards) + "-" + nameof(DeleteReward))]
        public static async Task<IActionResult> DeleteReward([HttpTrigger(AuthorizationLevel.Anonymous, "delete", 
            Route = "Rewards/{rewardId}/Delete")] HttpRequest req, ILogger log,
            int rewardId)
        {
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(DeleteReward)}");
            
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

        [FunctionName(nameof(Rewards) + "-" + nameof(EditReward))]
        public static async Task<IActionResult> EditReward([HttpTrigger(AuthorizationLevel.Anonymous, "put", 
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
            var query = "UPDATE " +
                            "Rewards " +
                        "SET " +
                            "Name = " +
                                $"COALESCE({(name == null ? "NULL" : $"'{name}'")}, Name), " +
                            "Rewards.Description = " +
                                $"COALESCE({(description == null ? "NULL" : $"'{description}'")}, Rewards.Description) " +
                        "WHERE " +
                            $"Rewards.Id = {rewardId}";

            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                try
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return new BadRequestObjectResult($"No reward with id {rewardId} exists!");
                    }
                }
                catch(Exception e)
                {
                    return new BadRequestObjectResult($"Editing reward failed: {e.Message}");
                }
            }

            return new OkObjectResult("Successfully edited the reward.");
        }

        #endregion

        #region Get

        private static async Task<Reward[]> GetUserRewards(int count, int userId)
        {
            //SQL query to get rewards and their names+description for selected user
            string sqlQuery = $"SELECT TOP {count} Rewards.Id, Rewards.Name, Rewards.Description FROM UsersRewards " +
                $"INNER JOIN Rewards " +
                $"ON UsersRewards.RewardId = Rewards.Id " +
                $"INNER JOIN Users ON UserId = Users.Id " +
                $"WHERE Users.Id = {userId}";

            SqlConnection conn = DBConnect.GetConnection();

            List<Reward> rewardsList = new List<Reward>();
            using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
            {

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
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
            return rewardsList.ToArray();
        }

        private static async Task<Reward[]> GetAllRewards()
        {
            //SQL query to get rewards and their names+description for selected user
            string sqlQuery = 
                $"SELECT TOP {1000} Rewards.Id, Rewards.Name, Rewards.Description " +
                "FROM Rewards";

            SqlConnection conn = DBConnect.GetConnection();

            List<Reward> rewardsList = new List<Reward>();
            using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
            {

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
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
            return rewardsList.ToArray();
        }

        private static async Task<Reward> GetRewardById(int rewardId)
        {
            //SQL query to get newest reward with name
            string sqlQuery = "SELECT TOP (1) " +
                "Id, Name, Description " +
                "FROM Rewards " +
               $"WHERE Rewards.Id = {rewardId}";

            SqlConnection conn = DBConnect.GetConnection();

            List<Reward> rewardsList = new List<Reward>();
            using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
            {

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    return
                        new Reward()
                        {
                            Id = int.Parse(reader["Id"].ToString()),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString()
                        };
                }
            }
            if (rewardsList.Count == 0)
            {
                //no reward found
                return null;
            }

            return rewardsList.ToArray()[0];
        }

        private static async Task<Reward> GetRewardByName(string rewardName)
        {
            //SQL query to get newest reward with name
            string sqlQuery =
                $"SELECT TOP (1) [Id],[Name],[Description] " +
                "FROM [dbo].[Rewards] " +
               $"WHERE Rewards.Name = '{rewardName}' " +
                //order by descending id to get newest entry
                "ORDER BY Rewards.Id DESC";

            SqlConnection conn = DBConnect.GetConnection();

            List<Reward> rewardsList = new List<Reward>();
            using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
            {

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {

                    return
                        new Reward()
                        {
                            Id = int.Parse(reader["Id"].ToString()),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString()
                        };
                }
            }
            return null;
        }

        #endregion Get

        private static async Task<bool> CreateReward(string name, string description)
        {
            // Queries
            //Query to insert new row into rewards with name and description
            var sqlStr =
            "INSERT INTO Rewards " +
                $"(Name, Description) " +
            "VALUES " +
                $"('{name}', '{description}')";
            //Get query to check if reward with name already exists
            var sqlGet =
            "SELECT COUNT(*) FROM Rewards " +
            $"WHERE (Name = '{name}')";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //Checks if reward with name already exists
            SqlCommand checkRewards = new SqlCommand(sqlGet, conn);
            checkRewards.Parameters.AddWithValue("Name", name);
            int existingRewards = (int) await checkRewards.ExecuteScalarAsync();
            if (existingRewards > 0)
            {
                // Close the database connection
                DBConnect.Dispose(conn);
                return false;
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Close the database connection
                DBConnect.Dispose(conn);
                return true;
            }
        }


    }
}
