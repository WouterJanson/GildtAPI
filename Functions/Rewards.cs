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

namespace GildtAPI
{
    public static class Rewards
    {
        #region Get methods
        const int DEFAULTCOUNT = 20;

        [FunctionName(nameof(Rewards) + "-" + nameof(GetAllRewards))]
        public static async Task<IActionResult> GetAllRewards([HttpTrigger(AuthorizationLevel.Function, "get", Route = nameof(Rewards))] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetAllRewards));
            string qCount = req.Query["count"];
            int count = DEFAULTCOUNT;
            if (qCount != null)
            {
                Int32.TryParse(qCount, out count);
                if (count < 1)
                {
                    return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                }
            }
            string rewardsJson = JsonConvert.SerializeObject(GetAllRewards());
            return new OkObjectResult(rewardsJson);
        }


        [FunctionName(nameof(Rewards) + "-" + nameof(GetRewardsForUser))]
        public static async Task<IActionResult> GetRewardsForUser([HttpTrigger(AuthorizationLevel.Function, "get", Route = "User/{userId}/Rewards")] HttpRequest req, ILogger log, 
            int userId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetRewardsForUser));
            string qCount = req.Query["count"];
            int count = DEFAULTCOUNT;
            if (qCount != null)
            {
                if (!Int32.TryParse(qCount, out count) || count < 1)
                {
                    return new BadRequestObjectResult("Invalid count. Count must be 1 or higher.");
                }
            }
            Reward[] userRewards = GetUserRewards(count, userId);

            if (userRewards.Length == 0)
            {
                return (ActionResult)new OkObjectResult("No rewards for this user.");
            }
            string rewardsJson = JsonConvert.SerializeObject(userRewards);
            return (ActionResult)new OkObjectResult(rewardsJson);
        }

        private static Reward[] GetAllRewards()
        {
            //SQL query to get rewards and their names+description for selected user
            string sqlQuery = 
                $"SELECT TOP {1000} Rewards.Id, Rewards.Name, Rewards.Description " +
                "FROM Rewards";

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
            return rewardsList.ToArray();
        }

        private static Reward[] GetUserRewards(int count, int userId)
        {
            //SQL query to get rewards and their names+description for selected user
            string sqlQuery = $"SELECT TOP {count} Rewards.Id, Rewards.Name, Rewards.Description " +
                "FROM UsersRewards" +
                "INNER JOIN Rewards ON UsersRewards.RewardId = Rewards.Id" +
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
            return rewardsList.ToArray();
        }

        [FunctionName(nameof(Rewards) + "-" + nameof(GetSingleReward))]
        public static async Task<IActionResult> GetSingleReward([HttpTrigger(AuthorizationLevel.Function, "get", Route = "Rewards/{rewardId}")] HttpRequest req, ILogger log,
            int rewardId)
        {
            log.LogInformation("C# HTTP trigger function processed a request: " + nameof(GetSingleReward));

            string qRewardId = req.Query["rewardId"];
            string qRewardName = req.Query["rewardName"];
            if (qRewardId == null && String.IsNullOrWhiteSpace(qRewardName))
            {
                //no valid rewardid or rewardname
                return new BadRequestObjectResult("Invalid parameters. Add a rewardId or rewardName parameter.");
            }


            if (qRewardId == null && qRewardName != null)
            {
                //No rewardId entered: get reward by name
                if (!String.IsNullOrWhiteSpace(qRewardName))
                {

                    //SQL query to get newest reward with name
                    string sqlQuery =
                        "SELECT TOP (1) [Id],[Name],[Description] " +
                        "FROM [dbo].[Rewards] " +
                       $"WHERE Rewards.Name = '{qRewardName}' " +
                       //order by descending id to get newest entry
                        "ORDER BY Rewards.Id DESC";

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
                    //
                    if (rewardsList.Count == 0 || rewardsList.Count > 1)
                    {
                        return (ActionResult)new OkObjectResult(
                            rewardsList.Count == 0 
                                ? "No rewards with this name."
                                : "Multiple rewards found. Something went wrong in the SQL query.");
                    }

                    string rewardsJson = JsonConvert.SerializeObject(rewardsList.ToArray());
                    return (ActionResult)new OkObjectResult(rewardsJson);
                }
            }
            else if (qRewardId != null)
            {
                //get reward by ID

                //SQL query to get newest reward with name
                string sqlQuery =
                    "SELECT TOP (1) " +
                        "Id, " +
                        "Name, " +
                        "Description " +
                    "FROM Rewards " +
                   $"WHERE Rewards.Id = {rewardId}";

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
                //
                if (rewardsList.Count == 0 || rewardsList.Count > 1)
                {
                    return (ActionResult)new OkObjectResult(
                        rewardsList.Count == 0
                            ? "No rewards with this name."
                            : "Multiple rewards found. Something went wrong in the SQL query.");
                }

                string rewardsJson = JsonConvert.SerializeObject(rewardsList.ToArray());
                return (ActionResult)new OkObjectResult(rewardsJson);
            }
            
            return new BadRequestObjectResult("Something weird happened.");

        }

        #endregion

        #region Post methods

        [FunctionName( nameof(Rewards) + "-" + nameof(CreateReward))]
        public static async Task<IActionResult> CreateReward([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Rewards/Create")] HttpRequest req, ILogger log)
        {
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
            // Queries
            var sqlStr =
            "INSERT INTO Rewards " +
                $"(Name, Description) " +
            "VALUES " +
                $"('{name}', '{description}')";

            var sqlGet =
            "SELECT COUNT(*) FROM Rewards " +
            $"WHERE (Name = '{name}')";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            //Checks if reward with name already exists
            SqlCommand checkRewards = new SqlCommand(sqlGet, conn);
            checkRewards.Parameters.AddWithValue("Name", name);
            int existingRewards = (int)checkRewards.ExecuteScalar();
            if (existingRewards > 0)
            {
                return new BadRequestObjectResult($"Reward named {name} already exists!");
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Close the database connection
                DBConnect.Dispose(conn);
                return new OkObjectResult("Successfully created the reward.");
            }
        }

        [FunctionName( nameof(Rewards) + "-" + nameof(DeleteReward))]
        public static async Task<IActionResult> DeleteReward([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Rewards/Delete")] HttpRequest req, ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request: {nameof(DeleteReward)}");

            string rewardIdString = req.Query["rewardId"];
            if (String.IsNullOrEmpty(rewardIdString) || !int.TryParse(rewardIdString, out int rewardId))
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
                    int affectedRows = cmd.ExecuteNonQuery();

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

        #endregion


    }
}
