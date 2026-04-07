using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Customers
    /// sử dụng SQL Server và thư viện Dapper
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng CustomerRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một khách hàng mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin khách hàng</param>
        /// <returns>Mã CustomerID của bản ghi vừa thêm</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Customers
                           (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                           VALUES
                           (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);
            return id;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Customers
                           SET CustomerName = @CustomerName,
                               ContactName = @ContactName,
                               Province = @Province,
                               Address = @Address,
                               Phone = @Phone,
                               Email = @Email,
                               IsLocked = @IsLocked
                           WHERE CustomerID = @CustomerID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Customers
                           WHERE CustomerID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin của một khách hàng theo mã
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Đối tượng Customer nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT CustomerID, CustomerName, ContactName, Province,
                                  Address, Phone, Email, IsLocked
                           FROM Customers
                           WHERE CustomerID = @id";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu khách hàng đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE CustomerID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách khách hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả danh sách khách hàng</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            //Input đang có 3 thông tin: Page, PageSize, SearchValue
            //Page: trang cần lấy
            //PageSize: số bản ghi trên mỗi trang
            //SearchValue: giá trị tìm kiếm (theo tên khách hàng)
            using var connection = new SqlConnection(_connectionString);

            int offset = (input.Page - 1) * input.PageSize; //Tính số bản ghi cần bỏ qua để lấy trang hiện tại

            string countSql = @"SELECT COUNT(*)
                                FROM Customers
                                WHERE CustomerName LIKE @search"; //Câu lệnh SQL để đếm tổng số bản ghi

            string dataSql = @"SELECT CustomerID, CustomerName, ContactName,
                                      Province, Address, Phone, Email, IsLocked
                               FROM Customers
                               WHERE CustomerName LIKE @search
                               ORDER BY CustomerName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY"; //Câu lệnh SQL để lấy dữ liệu có phân trang

            var parameters = new
            {
                search = $"%{input.SearchValue ?? ""}%",
                offset,
                pagesize = input.PageSize
            };

            int count = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var data = await connection.QueryAsync<Customer>(dataSql, parameters);

            return new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Kiểm tra email của khách hàng có hợp lệ hay không
        /// (email không được trùng với khách hàng khác)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: kiểm tra khi thêm mới
        /// Nếu id <> 0: kiểm tra khi cập nhật
        /// </param>
        /// <returns>True nếu email hợp lệ</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql;

            if (id == 0)
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @email";
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @email
                          AND CustomerID <> @id";
            }

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });

            return count == 0;
        }
    }
}