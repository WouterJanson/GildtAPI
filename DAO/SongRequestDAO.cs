using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using GildtAPI.Model;
using Microsoft.AspNetCore.Http;


namespace GildtAPI.DAO
{
    class SongRequestDAO : Singleton<SongRequestDAO>
    {
        // all songrequests
        private static List<SongRequest> songRequests = new List<SongRequest>();

        public async Task<List<SongRequest>> GetAll()
        {
            var sqlAllRequests =
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
        public async Task<SongRequest> Get(int id)
        {
            List<SongRequest> songRequestsList = await GetAll();

            foreach (SongRequest songrequests in songRequestsList)
            {
                if (songrequests.Id == id)
                {
                    return songrequests;
                }
            }

            return null;
        }

        public async Task<int> Delete(int id)
        {
            int rowsAffected;
            var sqlStr = $"DELETE SongRequest WHERE Id = '{id}'";
            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }
            return rowsAffected;
        }

        public async Task<int> Upvote(int RequestId, int UserId)
        {
            int vote = 1;
            int rowsAffected;

            //rij toevoegen als die nog niet bestaat
            var sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values ('{RequestId}', '{UserId}', '{vote}')";

            //alle songrequests met een upvote
            var sqlGet1 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '1' ";
            //alle songrequests met een downvote
            var sqlGet2 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '-1' ";

            //update downvote naar upvote
            var sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = {vote} " +
                                $" WHERE RequestId = {RequestId} AND UserId = {UserId};";


            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();


            using (SqlCommand cmd2 = new SqlCommand(sqlGet1, conn))
            //check if user have already upvoted
            using (SqlDataReader reader = cmd2.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    DBConnect.Dispose(conn);
                    return 0;
                }
                reader.Close();

            }

            // Update usersvote to upvote if it where downvote
            using (SqlCommand cmd3 = new SqlCommand(sqlGet2, conn))

            using (SqlDataReader reader = cmd3.ExecuteReader())
            {
                // controleer of er al een vote was in dit geval downvote
                if (reader.HasRows)
                {
                    reader.Close();
                    // update database row
                    using (SqlCommand cmd4 = new SqlCommand(sqlUpdateVote, conn))
                    {
                        rowsAffected = await cmd4.ExecuteNonQueryAsync();
                        DBConnect.Dispose(conn);
                        return rowsAffected;

                    }
                }
            }


            // insert a usersvote to a song
            using (SqlCommand cmd5 = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    rowsAffected = await cmd5.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    return rowsAffected;
                }
                catch (Exception e)
                {
                    return 0;
                }

            }
        }

        public async Task<int> Downvote(int RequestId, int UserId)
        {
            int vote = -1;
            int rowsAffected;
            var sqlStr = $"INSERT INTO SongRequestUserVotes (RequestId, UserId, Vote) Values ('{RequestId}', '{UserId}', '{vote}')";

            //alle songrequests met een downvote
            var sqlGet1 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '-1' ";
            //alle songrequests met een upvote
            var sqlGet2 = $"SELECT RequestId, UserId, Vote FROM SongRequestUserVotes WHERE RequestId = '{RequestId}' AND UserId = '{UserId}' AND Vote = '1' ";
            //update van upvote naar downvote
            var sqlUpdateVote = $"UPDATE SongRequestUserVotes SET " +
                                $"Vote = {vote} " +
                                $" WHERE RequestId = {RequestId} AND UserId = {UserId};";

            SqlConnection conn = DBConnect.GetConnection();


            using (SqlCommand cmd2 = new SqlCommand(sqlGet1, conn))
            //check if user have already upvoted
            using (SqlDataReader reader = cmd2.ExecuteReader())
            {

                if (reader.HasRows)
                {
                    // Close the database connection
                    DBConnect.Dispose(conn);
                    return 0;
                }
                reader.Close();

            }

            // Update usersvote to upvote if it where downvote
            using (SqlCommand cmd3 = new SqlCommand(sqlGet2, conn))

            using (SqlDataReader reader = cmd3.ExecuteReader())
            {

                // controlleer of er al een vote was in dit geval downvote
                if (reader.HasRows)
                {
                    reader.Close();
                    // update database row
                    using (SqlCommand cmd4 = new SqlCommand(sqlUpdateVote, conn))
                    {
                        rowsAffected = await cmd4.ExecuteNonQueryAsync();
                        DBConnect.Dispose(conn);
                        return rowsAffected;
                    }

                }

            }


            // insert a usersvote to a song
            using (SqlCommand cmd5 = new SqlCommand(sqlStr, conn))
            {
                try
                {
                    rowsAffected = await cmd5.ExecuteNonQueryAsync();
                    DBConnect.Dispose(conn);
                    return rowsAffected;
                    

                }
                catch (Exception e)
                {
                    return 0;
                }

            }
        }

        public async Task<int> AddSongRequest(SongRequest song)
        {
            int rowsaffected;

            var sqlStr =
                $"INSERT INTO SongRequest (Title, Artist, DateTime, UserId) " +
                $"VALUES ('{song.Title}', '{song.Artist}', '{DateTime.UtcNow}', '{song.UserId}')";

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsaffected = await cmd.ExecuteNonQueryAsync();
            }
            return rowsaffected;


            return rowsaffected;
        }


        private async Task addSongRequestList(string sqlAllRequests, SqlConnection conn)
        {
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
