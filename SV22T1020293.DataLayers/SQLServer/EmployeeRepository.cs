using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.HR;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu đối với bảng Employees
    /// sử dụng SQL Server và thư viện Dapper
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng EmployeeRepository
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm nhân viên mới vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin nhân viên</param>
        /// <returns>Mã EmployeeID của bản ghi vừa được thêm</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Employees
                (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames)
                VALUES
                (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking, @RoleNames);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);
            return id;
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking,
                    RoleNames = @RoleNames
                WHERE EmployeeID = @EmployeeID
            ";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa nhân viên khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                DELETE FROM Employees
                WHERE EmployeeID = @id
            ";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin của một nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Đối tượng Employee nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT EmployeeID, FullName, BirthDate,
                       Address, Phone, Email, Photo, IsWorking, RoleNames
                FROM Employees
                WHERE EmployeeID = @id
            ";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra nhân viên có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu nhân viên đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE EmployeeID = @id
            ";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });
            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách nhân viên có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả danh sách nhân viên</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            int offset = (input.Page - 1) * input.PageSize;

            string countSql = @"
                SELECT COUNT(*)
                FROM Employees
                WHERE FullName LIKE @search
            ";

            string dataSql = @"
                SELECT EmployeeID, FullName, BirthDate,
                       Address, Phone, Email, Photo, IsWorking, RoleNames
                FROM Employees
                WHERE FullName LIKE @search
                ORDER BY FullName
                OFFSET @offset ROWS
                FETCH NEXT @pagesize ROWS ONLY
            ";

            var parameters = new
            {
                search = $"%{input.SearchValue ?? ""}%",
                offset,
                pagesize = input.PageSize
            };

            int count = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var data = await connection.QueryAsync<Employee>(dataSql, parameters);

            return new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Kiểm tra email của nhân viên có hợp lệ hay không
        /// (email không được trùng với nhân viên khác)
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
                sql = @"
                    SELECT COUNT(*)
                    FROM Employees
                    WHERE Email = @email
                ";
            }
            else
            {
                sql = @"
                    SELECT COUNT(*)
                    FROM Employees
                    WHERE Email = @email
                      AND EmployeeID <> @id
                ";
            }

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
            return count == 0;
        }

        /// <summary>
        /// Cập nhật quyền của nhân viên
        /// </summary>
        /// <param name="employeeID">Mã nhân viên</param>
        /// <param name="roles">Danh sách quyền dạng chuỗi, ví dụ: admin,datamanager</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateEmployeeRolesAsync(int employeeID, string roles)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees
                SET RoleNames = @Roles
                WHERE EmployeeID = @EmployeeID
            ";

            int result = await connection.ExecuteAsync(sql, new
            {
                EmployeeID = employeeID,
                Roles = roles
            });

            return result > 0;
        }
    }
}