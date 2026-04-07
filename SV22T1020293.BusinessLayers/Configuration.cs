using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020293.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ các thông tin cấu hình sử dụng trong BusinessLayers
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = "";

        /// <summary>
        /// Hàm có chức năng khởi tạo cấu hình cho Business Layers
        /// (Hàm này phải được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy chuỗi tham số kết nối đến CSDL
        /// (Configuration.ConnectionString)
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
