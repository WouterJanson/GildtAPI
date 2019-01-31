using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class AttendanceDAO : Singleton<AttendanceDAO>
    {
        // Verify single user/event
        public async Task<bool> CheckVerificationAsync(int userId, int eventId)
        {
            //Get query to check if verification already exists
            var sqlGet =
                "SELECT COUNT(*) FROM AttendanceVerification " +
                "WHERE (UserId = @UserId AND EventId = @EventId)";

            using (var conn = DBConnect.GetConnection()) {
                var checkVer = new SqlCommand(sqlGet, conn);
                checkVer.Parameters.AddWithValue("@UserId", userId);
                checkVer.Parameters.AddWithValue("@EventId", eventId);

                int existingVer = (int)await checkVer.ExecuteScalarAsync();
                if (existingVer > 0) {
                    return true;
                }
                return false;
            }
        }

        public async Task<List<Attendance>> GetUserAttendanceListAsync(int userId, int count)
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

            var attendanceList = new List<Attendance>();

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlAttendance, conn)) {
                    var reader = await cmd.ExecuteReaderAsync();

                    while (reader.Read()) {
                        attendanceList.Add(
                            new Attendance(
                                int.Parse(reader["UserId"].ToString()),
                                int.Parse(reader["EventId"].ToString()),
                                reader["Username"].ToString()));
                    }
                }
            }

            return attendanceList;
        }

        public async Task<List<Attendance>> GetAttendanceListAsync(int? eventId, int count)
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
            if (eventId != null) {
                //Add WHERE if id parameter exists
                sqlAttendance = sqlAttendance + sqlWhere;
            }

            var attendanceList = new List<Attendance>();

            //Connects with the database
            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlAttendance, conn)) {

                    var reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read()) {
                        attendanceList.Add(
                            new Attendance(
                                int.Parse(reader["UserId"].ToString()),
                                int.Parse(reader["EventId"].ToString()),
                                reader["Username"].ToString()));
                    }
                }
            }

            return attendanceList;
        }

        public async Task CreateVerificationAsync(int userId, int eventId)
        {
            // Queries
            //Query to insert new row into AttendanceVerification
            var sqlStr =
            "INSERT INTO AttendanceVerification " +
                $"(UserId, EventId) " +
            "VALUES " +
                $"(@UserId, @EventId)";

            using (var conn = DBConnect.GetConnection()) {
                using (var cmd = new SqlCommand(sqlStr, conn)) {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@EventId", eventId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> DeleteVerificationAsync(int userId, int eventId)
        {
            // Queries
            var sqlStr =
            "DELETE FROM AttendanceVerification " +
            $"WHERE UserId = {userId} AND EventId = {eventId}";

            int affectedRows = -1;

            using (SqlConnection conn = DBConnect.GetConnection()) {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn)) {
                    affectedRows = await cmd.ExecuteNonQueryAsync();
                    return affectedRows;
                }
            }
        }
    }
}
