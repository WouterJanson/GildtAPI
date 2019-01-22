using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class TagDAO : Singleton<TagDAO>
    {
        EventDAO eventDAO;

        public async Task<int> DeleteTag(int Id)
        {
            //queries
            string sqlStr = $"DELETE Tags WHERE Id = @Id";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Id", Id);
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Createtag(string tag)
        {
            int TagAlreadyExist = 400;
            int RowsAffected;

            // Queries
            string sqlStr = $"INSERT INTO Tags (Name) VALUES (@tag)";
            string sqlTagCheckStr = $"SELECT Name FROM Tags WHERE Name = @tag";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            // check if tag already exist in the database to avoid dublicate entries
            using (SqlCommand cmd2 = new SqlCommand(sqlTagCheckStr, conn))
            {
                cmd2.Parameters.AddWithValue("@tag", tag);

                using (SqlDataReader reader = cmd2.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        DBConnect.Dispose(conn);
                        return TagAlreadyExist;
                    }
                    reader.Close();
                }
            }

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@tag", tag);

                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);
            return RowsAffected;
        }

        public async Task<int> EditTag(string tag, string id)
        {
            int RowsAffected;

            //query om te updaten
            string sqlStrUpdate = $"UPDATE Tags SET " +
                                  $"Name = COALESCE({(tag == null ? "NULL" : "@Tag")}, Name)" +
                                  $"Where Id= @Id";

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Tag", tag);

                RowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);

            return RowsAffected;
        }
    }
}
