using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Catalog;
using SV22T1020293.Models.Common;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Categories
    /// sử dụng SQL Server và thư viện Dapper
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng CategoryRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một loại hàng mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin loại hàng cần thêm</param>
        /// <returns>Mã CategoryID của bản ghi được thêm</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Categories (CategoryName, Description)
                           VALUES (@CategoryName, @Description);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);
            return id;
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Thông tin loại hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Categories
                           SET CategoryName = @CategoryName,
                               Description = @Description
                           WHERE CategoryID = @CategoryID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Xóa loại hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Categories
                           WHERE CategoryID = @id";

            int rowsAffected = await connection.ExecuteAsync(sql, new { id });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin của một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Đối tượng Category nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Categories
                           WHERE CategoryID = @id";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu loại hàng đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE CategoryID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách loại hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả danh sách loại hàng</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int offset = (input.Page - 1) * input.PageSize;

            string countSql = @"SELECT COUNT(*)
                                FROM Categories
                                WHERE CategoryName LIKE @search";

            string dataSql = @"SELECT *
                               FROM Categories
                               WHERE CategoryName LIKE @search
                               ORDER BY CategoryName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

            var parameters = new
            {
                search = $"%{input.SearchValue ?? ""}%",
                offset = offset,
                pagesize = input.PageSize
            };

            int count = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var data = await connection.QueryAsync<Category>(dataSql, parameters);

            return new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }
    }
}