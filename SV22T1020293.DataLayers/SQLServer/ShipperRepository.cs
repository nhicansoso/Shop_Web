using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Shippers
    /// sử dụng SQL Server và thư viện Dapper
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng ShipperRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một người giao hàng mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin người giao hàng</param>
        /// <returns>Mã ShipperID của bản ghi vừa được thêm</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Shippers (ShipperName, Phone)
                           VALUES (@ShipperName, @Phone);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);
            return id;
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="data">Thông tin người giao hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Shippers
                           SET ShipperName = @ShipperName,
                               Phone = @Phone
                           WHERE ShipperID = @ShipperID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        /// <summary>
        /// Xóa người giao hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Shippers WHERE ShipperID = @id";

            int rowsAffected = await connection.ExecuteAsync(sql, new { id });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin của một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Đối tượng Shipper nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Shippers
                           WHERE ShipperID = @id";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong đơn hàng hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE ShipperID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách người giao hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả danh sách người giao hàng</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int count;
            int offset = (input.Page - 1) * input.PageSize;

            string countSql = @"SELECT COUNT(*)
                                FROM Shippers
                                WHERE ShipperName LIKE @search";

            string dataSql = @"SELECT *
                               FROM Shippers
                               WHERE ShipperName LIKE @search
                               ORDER BY ShipperName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

            var parameters = new
            {
                search = $"%{input.SearchValue ?? ""}%",
                offset = offset,
                pagesize = input.PageSize
            };

            count = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var data = await connection.QueryAsync<Shipper>(dataSql, parameters);

            return new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }
    }
}