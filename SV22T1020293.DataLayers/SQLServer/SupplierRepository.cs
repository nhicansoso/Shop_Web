using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Suppliers
    /// sử dụng SQL Server và thư viện Dapper
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng SupplierRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào CSDL
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần thêm</param>
        /// <returns>Mã SupplierID của bản ghi được thêm</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Suppliers
                           (SupplierName, ContactName, Province, Address, Phone, Email)
                           VALUES
                           (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);
            return id;
        }

        /// <summary>
        /// Xóa một nhà cung cấp khỏi CSDL
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Suppliers WHERE SupplierID = @id";

            int rowsAffected = await connection.ExecuteAsync(sql, new { id });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin của một nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Thông tin nhà cung cấp hoặc null nếu không tồn tại</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Suppliers
                           WHERE SupplierID = @id";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng hay không
        /// (ví dụ: có sản phẩm liên quan)
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>true nếu đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE SupplierID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Danh sách nhà cung cấp theo trang</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = 0;
            int offset = (input.Page - 1) * input.PageSize;

            string countSql = @"SELECT COUNT(*)
                                FROM Suppliers
                                WHERE SupplierName LIKE @search";

            string dataSql = @"SELECT *
                               FROM Suppliers
                               WHERE SupplierName LIKE @search
                               ORDER BY SupplierName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

            var parameters = new
            {
                search = $"%{input.SearchValue ?? ""}%",
                offset = offset,
                pagesize = input.PageSize
            };

            count = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var data = await connection.QueryAsync<Supplier>(dataSql, parameters);

            return new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của nhà cung cấp
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Suppliers
                           SET SupplierName = @SupplierName,
                               ContactName = @ContactName,
                               Province = @Province,
                               Address = @Address,
                               Phone = @Phone,
                               Email = @Email
                           WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);

            return rowsAffected > 0;
        }
    }
}