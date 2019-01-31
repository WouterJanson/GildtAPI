using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using GildtAPI.Model;

namespace GildtAPI.DAO
{
    class UserDAO : Singleton<UserDAO>
    {
        private static List<User> users = new List<User>();
        private static List<UsersCoupon> couponsList = new List<UsersCoupon>();

        // Get all users 
        public async Task<List<User>> GetAll()
        {
            string sqlStrUsers = $"SELECT * FROM Users ";
            string sqlStrCoupons = "SELECT * FROM UsersCoupons INNER JOIN Coupons ON UsersCoupons.CouponId = Coupons.Id";

            SqlConnection conn = DBConnect.GetConnection();

            await addCouponsToList(sqlStrCoupons, conn);
            await addUsersToList(sqlStrUsers, conn);

            DBConnect.Dispose(conn);

            return users;
        }

        // Get single user
        public async Task<User> Get(int id)
        {
            List<User> usersList = await GetAll();

            foreach(User user in usersList)
            {
                if(user.Id == id)
                {
                    return user;
                }
            }

            return null;
        }

        // Delete single user
        public async Task<int> Delete(int id)
        {
            int rowsAffected;
            string sqlStr = $"DELETE FROM Users WHERE Users.Id = @Id";

            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                rowsAffected = await cmd.ExecuteNonQueryAsync();

            }
            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Create(User user)
        {
            string sqlStr =
            $"INSERT INTO Users (IsAdmin, Username, Email, Password) VALUES ('false', @Username, @Email, @Password)";
            int rowsAffected;

            List<User> usersList = await GetAll();

            foreach (User u in usersList)
            {
                if (u.Username == user.Username || u.Email == user.Email)
                {
                    return 0;
                }
            }

            SqlConnection conn = DBConnect.GetConnection();

            using (SqlCommand cmd = new SqlCommand(sqlStr, conn))
            {
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Password", user.Password);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }

        public async Task<int> Edit(User user)
        {
            string sqlStrUpdate = $"UPDATE Users SET " +
            $"Username = COALESCE({(user.Username == null ? "NULL" : "@Username")}, Username), " +
            $"Email = COALESCE({(user.Email == null ? "NULL" : "@Email")}, Email), " +
            $"Password = COALESCE({(user.Password == null ? "NULL" : "@Password")}, Password), " +
            $"IsAdmin = COALESCE({(user.IsAdmin.ToString() == null ? "NULL" : "@IsAdmin")}, IsAdmin) " +
            $"WHERE Id = @Id";
            int rowsAffected;

            SqlConnection conn = DBConnect.GetConnection();

            using(SqlCommand cmd = new SqlCommand(sqlStrUpdate, conn))
            {
                cmd.Parameters.AddWithValue("@Id", ((object)user.Id) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Username", ((object)user.Username) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", ((object)user.Email) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Password", ((object)user.Password) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsAdmin", ((object)user.IsAdmin) ?? DBNull.Value);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            DBConnect.Dispose(conn);

            return rowsAffected;
        }


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
        }

        private async Task addUsersToList(string sqlStrUsers, SqlConnection conn)
        {
            users.Clear();
            using(SqlCommand cmdUsers = new SqlCommand(sqlStrUsers, conn))
            {
                SqlDataReader readerUsers = await cmdUsers.ExecuteReaderAsync();

                while (readerUsers.Read())
                {
                    List<UsersCoupon> tempList = new List<UsersCoupon>();

                    foreach (UsersCoupon coupons in couponsList)
                    {
                        if (coupons.UserId == Convert.ToInt32(readerUsers["id"]))
                        {
                            tempList.Add(coupons);
                        }
                    }
                    users.Add(new User()
                    {
                        Id = Convert.ToInt32(readerUsers["id"]),
                        Username = readerUsers["Username"].ToString(),
                        Email = readerUsers["Email"].ToString(),
                        Password = readerUsers["Password"].ToString(),
                        Coupons = tempList
                    });
                }
                readerUsers.Close();
            }
        }

    }
}
