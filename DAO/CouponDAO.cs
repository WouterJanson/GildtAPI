using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
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
                $"INSERT INTO Coupons (Name, Description, StartDate, EndDate, Type, TotalUsed, Image) VALUES (@Name, @Description, @StartDate, @EndDate, @Type, '0', @Image)";

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
                cmd.Parameters.AddWithValue("@Name", coupon.Name);
                cmd.Parameters.AddWithValue("@Description", coupon.Description);
                cmd.Parameters.AddWithValue("@StartDate", coupon.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", coupon.EndDate);
                cmd.Parameters.AddWithValue("@Type", coupon.Type);
                cmd.Parameters.AddWithValue("@Image", coupon.Image);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Delete(int id)
        {
            int rowsAffected;
            string sqlStr = $"DELETE Coupons WHERE Id = @Id";

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                rowsAffected = await cmd.ExecuteNonQueryAsync();

            }
            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Edit(Coupon coupon)
        {
            int rowsAffected;
            string sqlStrUpdate = $"UPDATE Coupons SET " +
                    $"Name = COALESCE({(coupon.Name == null ? "NULL" : "@Name")}, Name), " +
                    $"Description = COALESCE({(coupon.Description == null ? "NULL" : "@Description")}, Description), " +
                    $"StartDate = COALESCE({(coupon.StartDate.ToString() == null ? "NULL" : "@StartDate")}, StartDate), " +
                    $"EndDate = COALESCE({(coupon.EndDate.ToString() == null ? "NULL" : "@EndDate")}, EndDate), " +
                    $"Type = COALESCE({(coupon.Type.ToString() == null ? "NULL" : "@Type")}, Type), " +
                    $"Image = COALESCE({(coupon.Image == null ? "NULL" : "@Image")}, Image) " +
                    $" WHERE Id = @Id";

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                cmd.Parameters.AddWithValue("@Id", ((object)coupon.Id) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", ((object)coupon.Name) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", ((object)coupon.Description) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", ((object)coupon.StartDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", ((object)coupon.EndDate) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", ((object)coupon.Type) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Image", ((object)coupon.Image) ?? DBNull.Value);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> SignUp(int userId, int couponId)
        {
            var sqlStr = $"INSERT INTO UsersCoupons (UserId, CouponId) VALUES (@UserId, @CouponId)";
            var sqlGet =
                $"SELECT COUNT(*) FROM UsersCoupons WHERE CouponId = @CouponId AND UserId = @UserId";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            SqlCommand checkCoupon = new SqlCommand(sqlGet, conn);
            checkCoupon.Parameters.AddWithValue("@CouponId", couponId);
            checkCoupon.Parameters.AddWithValue("@UserId", userId);
            int CouponExist = (int)checkCoupon.ExecuteScalar();

            if (CouponExist > 0)
            {
                return 0;
            }
            else
            {
                using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
                {
                    cmd.Parameters.AddWithValue("@CouponId", couponId);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task AddCouponsToList(string sqlStr, SqlConnection conn)
        {
            coupons.Clear();
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
