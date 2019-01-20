using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GildtAPI.Controllers;
using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class CouponDAO : Singleton<CouponDAO>
    {
        private static List<Coupon> coupons = new List<Coupon>();

        public async Task<List<Coupon>> GetAll()
        {
            string sqlStr = $"SELECT * FROM Coupons";

            SqlConnection conn = DBConnect.GetConnection();

            await AddCouponsToList(sqlStr, conn);

            DBConnect.Dispose(conn);

            return coupons;
        }

        public async Task<Coupon> Get(int id)
        {
            List<Coupon> couponsList = await GetAll();

            foreach (Coupon coupon in couponsList)
            {
                if (coupon.Id == id)
                {
                    return coupon;
                }
            }

            return null;
        }

        public async Task<int> Create(Coupon coupon)
        {
            int rowsAffected;
            string sqlStr =
                $"INSERT INTO Coupons (Name, Description, StartDate, EndDate, Type, TotalUsed, Image) VALUES ('{coupon.Name}', '{coupon.Description}', '{coupon.StartDate}', '{coupon.EndDate}', '{coupon.Type}', '0', '{coupon.Image}')";

            List<Coupon> couponsList = await GetAll();

            foreach(Coupon c in couponsList)
            {
                if(c.Name == coupon.Name)
                {
                    return 0;
                }
            }

            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Delete(int id)
        {
            int rowsAffected;
            string sqlStr = $"DELETE Coupons WHERE Id = '{id}'";

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();

            }
            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Edit(Coupon coupon)
        {
            int rowsAffected;
            string sqlStrUpdate = $"UPDATE Coupons SET " +
                    $"Name = COALESCE({(coupon.Name == null ? "NULL" : $"'{coupon.Name}'")}, Name), " +
                    $"Description = COALESCE({(coupon.Description == null ? "NULL" : $"'{coupon.Description}'")}, Description), " +
                    $"StartDate = COALESCE({(coupon.StartDate.ToString() == null ? "NULL" : $"'{coupon.StartDate.ToString()}'")}, StartDate), " +
                    $"EndDate = COALESCE({(coupon.EndDate.ToString() == null ? "NULL" : $"'{coupon.EndDate.ToString()}'")}, EndDate), " +
                    $"Type = COALESCE({(coupon.Type.ToString() == null ? "NULL" : $"'{coupon.Type.ToString()}'")}, Type), " +
                    $"Image = COALESCE({(coupon.Image == null ? "NULL" : $"'{coupon.Image}'")}, Image) " +
                    $" WHERE Id = {coupon.Id}";

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        // @TODO: Finish it up - Check if it's possible to not require the sqlGet method, instead GetAll();
        public async Task<int> Signup(int userId, int couponId)
        {
            var sqlStr = $"INSERT INTO UsersCoupons (UserId, CouponId) VALUES ('{userId}', '{couponId}')";
            var sqlGet =
                $"SELECT COUNT(*) FROM UsersCoupons WHERE CouponId = '{couponId}' AND UserId = '{userId}'";

            SqlConnection conn = DBConnect.GetConnection();



            SqlCommand checkCoupon = new SqlCommand(sqlGet, conn);
            checkCoupon.Parameters.AddWithValue("CouponId", couponId);
            checkCoupon.Parameters.AddWithValue("UserId", userId);
            int CouponExist = (int)checkCoupon.ExecuteScalar();

            // @TODO: Temp return value, change it!
            return 0;
        }

        public async Task AddCouponsToList(string sqlStr, SqlConnection conn)
        {
            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    coupons.Add(
                        new Coupon()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                            EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                            Type = Convert.ToInt32(reader["Type"].ToString()),
                            TotalUsed = Convert.ToInt32(reader["TotalUsed"]),
                            Image = reader["Image"].ToString()
                        }
                    );
                }
                reader.Close();
            }
        }
    }
}
