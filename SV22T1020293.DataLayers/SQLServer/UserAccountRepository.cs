using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Security;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản người dùng
    /// </summary>
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public UserAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Kiểm tra thông tin đăng nhập của người dùng
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>
        /// Trả về thông tin tài khoản nếu đăng nhập hợp lệ,
        /// ngược lại trả về null
        /// </returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            var account = await connection.QueryFirstOrDefaultAsync<UserAccount>(
                @"SELECT 
                        CAST(EmployeeID AS NVARCHAR(50)) AS UserId,
                        Email AS UserName,
                        FullName AS DisplayName,
                        Email,
                        ISNULL(Photo, 'nophoto.png') AS Photo,
                        ISNULL(RoleNames, '') AS RoleNames
                  FROM Employees
                  WHERE Email = @userName
                        AND Password = @password
                        AND IsWorking = 1

                  UNION

                  SELECT 
                        CAST(CustomerID AS NVARCHAR(50)) AS UserId,
                        Email AS UserName,
                        CustomerName AS DisplayName,
                        Email,
                        '' AS Photo,
                        'Customer' AS RoleNames
                  FROM Customers
                  WHERE Email = @userName
                        AND Password = @password
                        AND (IsLocked = 0 OR IsLocked IS NULL)",
                new { userName, password });

            return account;
        }

        /// <summary>
        /// Đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu đổi mật khẩu thành công</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows1 = await connection.ExecuteAsync(
                @"UPDATE Employees
                  SET Password = @password
                  WHERE Email = @userName",
                new { userName, password });

            int rows2 = await connection.ExecuteAsync(
                @"UPDATE Customers
                  SET Password = @password
                  WHERE Email = @userName",
                new { userName, password });

            return (rows1 + rows2) > 0;
        }



    }
}