using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class RewardsDAO : Singleton<RewardsDAO>
    {
        public async Task<Reward> GetRewardByIdAsync(int rewardId)
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

        public async Task<Reward> GetRewardByNameAsync(string rewardName)
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

        public async Task<Reward[]> GetAllRewardsAsync()
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

        public async Task<Reward[]> GetUserRewardsAsync(int count, int userId)
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

        public async Task<bool> CreateRewardAsync(string name, string description)
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
            int existingRewards = (int)await checkRewards.ExecuteScalarAsync();
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

        public async Task<int> EditRewardAsync(int rewardId, string name, string description)
        {
            var query = "UPDATE Rewards " +
                        "SET Name = " +
                        $"COALESCE({(name == null ? "NULL" : $"@name")}, Name), " +
                        "Rewards.Description = " +
                        $"COALESCE({(description == null ? "NULL" : $"@description")}, Rewards.Description) " +
                        $"WHERE Rewards.Id = {rewardId}";

            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@description", description);
                int affectedRows = await cmd.ExecuteNonQueryAsync();
                return affectedRows;
            }
        }

    }
}
