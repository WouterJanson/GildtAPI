using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GildtAPI.Model;


namespace GildtAPI.DAO
{
    class SongRequestDAO : Singleton<SongRequestDAO>
    {
        // all songrequests
        private static List<SongRequest> songRequests = new List<SongRequest>();

        public async Task<List<SongRequest>> GetAllSongrequests()
        {
            string sqlAllRequests =
                $"SELECT sr.Id AS RequestId,sr.DateTime ,sr.UserId, sr.Title, sr.Artist," +
                " CASE WHEN uv.Upvotes IS NULL THEN 0 ELSE uv.Upvotes END as Upvotes, CASE WHEN dv.Downvotes IS NULL THEN 0 ELSE dv.Downvotes END as Downvotes " +
                "FROM SongRequest AS sr " +
                "LEFT JOIN( " +
                "SELECT d.RequestId AS RequestID, COUNT(UserId) AS Downvotes FROM SongRequestUserVotes AS d WHERE d.Vote< 0 GROUP BY " +
                "d.RequestId, d.Vote) as dv ON dv.RequestID = sr.Id " +
                "LEFT JOIN( " +
                "SELECT u.RequestId AS RequestID, COUNT(UserId) AS Upvotes FROM SongRequestUserVotes AS u WHERE u.Vote > 0 GROUP BY " +
                "u.RequestId, u.Vote ) as uv ON sr.Id = uv.RequestID ";

            SqlConnection conn = DBConnect.GetConnection();
            await addSongRequestList(sqlAllRequests, conn);
            DBConnect.Dispose(conn);
            return songRequests;

        }

        //single songrequest 
        public async Task<SongRequest> GetSongrequest(int id)
        {
            List<SongRequest> songRequestsList = await GetAllSongrequests();

            foreach (SongRequest songrequests in songRequestsList)
            {
                if (songrequests.Id == id)
                {
                    return songrequests;
                }
            }

            return null;
        }

        public async Task<int> DeleteSongrequest(int id)
        {
            int rowsAffected;
            string sqlStr = $"DELETE SongRequest WHERE Id = @id";
            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }
            return rowsAffected;
        }

        public async Task<int> Upvote(int RequestId, int UserId)
        {
            int vote = 1;
            int rowsAffected;

            string sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = @vote " +
                                $" WHERE RequestId = @RequestId AND UserId = @UserId;";

            //rij toevoegen als die nog niet bestaat
            string sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values (@RequestId, @UserId, @vote)";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();


            // insert a usersvote to a song
            using (SqlCommand cmd = new SqlCommand(sqlUpdateVote, conn))
            {

                cmd.Parameters.AddWithValue("@RequestId", RequestId);
                cmd.Parameters.AddWithValue("@UserId", UserId);
                cmd.Parameters.AddWithValue("@vote", vote);
                try
                {
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {

                        using (SqlCommand cmd2 = new SqlCommand(sqlStr, conn))
                        {
                            cmd2.Parameters.AddWithValue("@RequestId", RequestId);
                            cmd2.Parameters.AddWithValue("@UserId", UserId);
                            cmd2.Parameters.AddWithValue("@vote", vote);
                            rowsAffected = await cmd2.ExecuteNonQueryAsync();
                        }
                    }
                    DBConnect.Dispose(conn);
                    return rowsAffected;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return 0;
                }
            }
        }

        public async Task<int> Downvote(int RequestId, int UserId)
        {
            int vote = -1;
            int rowsAffected;
            //update van upvote naar downvote
            string sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = @vote " +
                                $" WHERE RequestId = @RequestId AND UserId = @UserId;";
            //insert nieuwe rij
            string sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values (@RequestId, @UserId, @vote)";

            SqlConnection conn = DBConnect.GetConnection();

            // insert a usersvote to a song
            using (SqlCommand cmd = new SqlCommand(sqlUpdateVote, conn))
            {

                cmd.Parameters.AddWithValue("@RequestId", RequestId);
                cmd.Parameters.AddWithValue("@UserId", UserId);
                cmd.Parameters.AddWithValue("@vote", vote);
                try
                {
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {

                        using (SqlCommand cmd2 = new SqlCommand(sqlStr, conn))
                        {
                            cmd2.Parameters.AddWithValue("@RequestId", RequestId);
                            cmd2.Parameters.AddWithValue("@UserId", UserId);
                            cmd2.Parameters.AddWithValue("@vote", vote);
                            rowsAffected = await cmd2.ExecuteNonQueryAsync();
                        }
                    }
                    DBConnect.Dispose(conn);
                    return rowsAffected;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return 0;
                }
            }
        }

        public async Task<int> AddSongRequest(SongRequest song)
        {
            int rowsAffected;

            var sqlStr =
                $"INSERT INTO SongRequest (Title, Artist, DateTime, UserId) " +
                $"VALUES (@Title, @Artist, @DateTime, @UserId)";


            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Title", song.Title);
                cmd.Parameters.AddWithValue("@Artist", song.Artist);
                cmd.Parameters.AddWithValue("@DateTime", song.DateTime);
                cmd.Parameters.AddWithValue("@UserId", song.UserId);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);
            return rowsAffected;
        }


        private async Task addSongRequestList(string sqlAllRequests, SqlConnection conn)
        {
            songRequests.Clear();
            using (SqlCommand cmd = new SqlCommand(sqlAllRequests, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    songRequests.Add(
                        new SongRequest()
                        {
                            Id = Convert.ToInt32(reader["RequestId"]),
                            Title = reader["Title"].ToString(),
                            Artist = reader["Artist"].ToString(),
                            DateTime = DateTime.Parse(reader["DateTime"].ToString()),
                            UserId = Convert.ToInt32(reader["UserId"]),
                            Upvotes = Convert.ToInt32(reader["Upvotes"]),
                            Downvotes = Convert.ToInt32(reader["Downvotes"])
                        }
                    );
                }
                reader.Close();
            }
        }


    }
}
