namespace SV22T1020293.Models.Security
{
    /// <summary>
    /// Dữ liệu dùng để đổi mật khẩu
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// Mật khẩu cũ
        /// </summary>
        public string OldPassword { get; set; } = "";

        /// <summary>
        /// Mật khẩu mới
        /// </summary>
        public string NewPassword { get; set; } = "";

        /// <summary>
        /// Xác nhận mật khẩu mới
        /// </summary>
        public string ConfirmPassword { get; set; } = "";
    }
}