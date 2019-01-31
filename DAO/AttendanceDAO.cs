using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class AttendanceDAO : Singleton<AttendanceDAO>
    {
        // Verify single user/event
        public async Task<bool> CheckVerification(int userId, int eventId)
        {
            //Get query to check if verification already exists
            var sqlGet =
                "SELECT COUNT(*) FROM AttendanceVerification " +
                "WHERE (UserId = @UserId AND EventId = @EventId)";
            var conn = DBConnect.GetConnection();
            SqlCommand checkVer = new SqlCommand(sqlGet, conn);
            checkVer.Parameters.AddWithValue("@UserId", userId);
            checkVer.Parameters.AddWithValue("@EventId", eventId);
            int existingVer = (int)await checkVer.ExecuteScalarAsync();
            if (existingVer > 0)
            {
                // Close the database connection
                DBConnect.Dispose(conn);
                return true;
            }

            return false;
        }

        public async Task<List<Attendance>> GetUserAttendanceList(int userId, int count)
        {
            var sqlAttendance =
                $"SELECT TOP {count} " +
                $"att.EventId AS EventId, " +
                $"Users.Id as UserId, " +
                $"Users.Username AS Username " +
                $"FROM AttendanceVerification as att " +
                $"INNER JOIN Users " +
                $"ON att.UserId = Users.Id " +
                $"WHERE att.UserId = {userId}";

            List<Attendance> attendanceList = new List<Attendance>();

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlAttendance, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {

                    attendanceList.Add(
                        new Attendance(
                            int.Parse(reader["UserId"].ToString()),
                            int.Parse(reader["EventId"].ToString()),
                            reader["Username"].ToString()));
                }
            }
            return attendanceList;
        }

        public async Task<List<Attendance>> GetAttendanceList(int? eventId, int count)
        {
            var sqlAttendance =
                $"SELECT TOP {count} " +
                $"att.EventId AS EventId, " +
                $"Users.Id as UserId, " +
                $"Users.Username AS Username " +
                $"FROM AttendanceVerification as att " +
                $"INNER JOIN Users " +
                $"ON att.UserId = Users.Id ";
            var sqlWhere = $"WHERE att.EventId = {eventId}";
            // Checks if an id parameter is filled in
            if (eventId != null)
            {
                //Add WHERE if id parameter exists
                sqlAttendance = sqlAttendance + sqlWhere;
            }

            List<Attendance> attendanceList = new List<Attendance>();

            //Connects with the database
            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlAttendance, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {

                    attendanceList.Add(
                        new Attendance(
                            int.Parse(reader["UserId"].ToString()),
                            int.Parse(reader["EventId"].ToString()),
                            reader["Username"].ToString()));
                }
            }

            return attendanceList;
        }

        public async Task CreateVerification(int userId, int eventId)
        {
            // Queries
            //Query to insert new row into AttendanceVerification
            var sqlStr =
            "INSERT INTO AttendanceVerification " +
                $"(UserId, EventId) " +
            "VALUES " +
                $"(@UserId, @EventId)";

            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EventId", eventId);
                await cmd.ExecuteNonQueryAsync();
            }

            // Close the database connection
            DBConnect.Dispose(conn);
        }

        public async Task<int> DeleteVerification(int userId, int eventId)
        {
            // Queries
            var sqlStr =
            "DELETE FROM AttendanceVerification " +
            $"WHERE UserId = {userId} AND EventId = {eventId}";
            int affectedRows = -1;
            SqlConnection conn = DBConnect.GetConnection();
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                affectedRows = await cmd.ExecuteNonQueryAsync();
                DBConnect.Dispose(conn);
                return affectedRows;
            }
        }
    }
}
