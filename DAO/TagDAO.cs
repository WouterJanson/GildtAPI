using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class TagDAO : Singleton<TagDAO>
    {

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            // get all events Query
            string sqlStr = "SELECT * FROM Tags";
            List<Tag> tags;

            using (var conn = DBConnect.GetConnection()) {
                tags = new List<Tag>();
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    var reader = await cmd.ExecuteReaderAsync();

                    while (reader.Read()) {
                        var t = new Tag(Convert.ToInt32(reader["Id"]), reader["Name"].ToString());
                        tags.Add(t);
                    }
                }
            }

            return tags;
        }

        public async Task<int> DeleteTagAsync(int Id)
        {
            //queries
            string sqlStr = $"DELETE Tags WHERE Id = @Id";
            int rowsAffected;

            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@Id", Id);
                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected;
        }

        public async Task<int> CreateTagAsync(string tag)
        {
            int RowsAffected;

            // Queries
            string sqlStr = $"INSERT INTO Tags (Name) VALUES (@tag)";
            string sqlTagCheckStr = $"SELECT Name FROM Tags WHERE Name = @tag";

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                // check if tag already exist in the database to avoid dublicate entries
                using (var cmd2 = new SqlCommand(sqlTagCheckStr, conn)) {
                    cmd2.Parameters.AddWithValue("@tag", tag);

                    using (var reader = cmd2.ExecuteReader()) {
                        if (reader.HasRows) {
                            return 0;
                        }
                        reader.Close();
                    }
                }

                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@tag", tag);

                    RowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }

        public async Task<int> EditTagAsync(string tag, string id)
        {
            int RowsAffected;

            //query om te updaten
            string sqlStrUpdate = $"UPDATE Tags SET " +
                                  $"Name = COALESCE({(tag == null ? "NULL" : "@Tag")}, Name)" +
                                  $"Where Id= @Id";

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStrUpdate, conn)) {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Tag", tag);

                    RowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            return RowsAffected;
        }
    }
}
