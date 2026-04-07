using System.ComponentModel.DataAnnotations;

namespace SV22T1020293.Models.Security
{
    /// <summary>
    /// Model dùng cho form đăng ký tài khoản khách hàng
    /// </summary>
    public class RegisterModel
    {
        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;
        /// <summary>
        /// Tên giao dịch
        /// </summary>
        public string ContactName { get; set; } = string.Empty;
        /// <summary>
        /// Tỉnh/thành
        /// </summary>
        public string? Province { get; set; }
        /// <summary>
        /// Địa chỉ
        /// </summary>
        public string? Address { get; set; }
        /// <summary>
        /// Điện thoại
        /// </summary>
        public string? Phone { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public string ConfirmPassword { get; set; } = "";

        public string? Gender { get; set; }  


    }
}