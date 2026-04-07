using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.DataLayers.SQLServer;
using SV22T1020293.Models.Security;

namespace SV22T1020293.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bảo mật và tài khoản
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository accountDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SecurityDataService()
        {
            accountDB = new UserAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Kiểm tra thông tin đăng nhập của tài khoản
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu đã mã hóa</param>
        /// <returns></returns>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            return await accountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu mới đã mã hóa</param>
        /// <returns></returns>
        public static async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            return await accountDB.ChangePasswordAsync(userName, password);
        }

    }
}