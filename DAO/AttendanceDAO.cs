using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
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
        /*
        private async Task addCouponsToList(string sqlStrCoupons, SqlConnection conn)
        {
            couponsList.Clear();
            using (SqlCommand cmdCoupons = new SqlCommand(sqlStrCoupons, conn))
            {
                SqlDataReader readerCoupons = await cmdCoupons.ExecuteReaderAsync();
                while (readerCoupons.Read())
                {
                    couponsList.Add(
                        new UsersCoupon()
                        {
                            CouponId = Convert.ToInt32(readerCoupons["CouponId"]),
                            Name = readerCoupons["Name"].ToString(),
                            Description = readerCoupons["Description"].ToString(),
                            StartDate = DateTime.Parse(readerCoupons["StartDate"].ToString()),
                            EndDate = DateTime.Parse(readerCoupons["EndDate"].ToString()),
                            Type = Convert.ToInt32(readerCoupons["Type"].ToString()),
                            TotalUsed = Convert.ToInt32(readerCoupons["TotalUsed"]),
                            Image = readerCoupons["Image"].ToString(),
                            UserId = Convert.ToInt32(readerCoupons["UserId"])
                        }
                    );
                }
                readerCoupons.Close();
            }
        }*/
    }
}
