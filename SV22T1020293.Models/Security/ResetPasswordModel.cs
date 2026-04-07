namespace SV22T1020293.Models.Security
{
    /// <summary>
    /// Dữ liệu dùng để đổi mật khẩu cho khách hàng / nhân viên
    /// </summary>
    public class ResetPasswordModel
    {
        /// <summary>
        /// Mã đối tượng (CustomerID hoặc EmployeeID)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tên hiển thị
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Email đăng nhập
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái hiển thị
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Mật khẩu mới
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Xác nhận mật khẩu
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}