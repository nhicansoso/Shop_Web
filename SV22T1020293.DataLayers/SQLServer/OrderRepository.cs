using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Sales;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm</param>
        /// <returns>Danh sách đơn hàng</returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                keyword = "%" + input.SearchValue + "%",
                status = input.Status,
                dateFrom = input.DateFrom,
                dateTo = input.DateTo,
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
          FROM Orders o
          LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
          WHERE c.CustomerName LIKE @keyword
                AND (@status = 0 OR o.Status = @status)
                AND (@dateFrom IS NULL OR CAST(o.OrderTime AS DATE) >= @dateFrom)
                AND (@dateTo IS NULL OR CAST(o.OrderTime AS DATE) <= @dateTo)", parameters);

            var data = await connection.QueryAsync<OrderViewInfo>(
                @"SELECT o.*, c.CustomerName
          FROM Orders o
          LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
          WHERE c.CustomerName LIKE @keyword
                AND (@status = 0 OR o.Status = @status)
                AND (@dateFrom IS NULL OR CAST(o.OrderTime AS DATE) >= @dateFrom)
                AND (@dateTo IS NULL OR CAST(o.OrderTime AS DATE) <= @dateTo)
          ORDER BY o.OrderTime DESC
          OFFSET @offset ROWS
          FETCH NEXT @pagesize ROWS ONLY", parameters);

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin đơn hàng</returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT 
                    O.*,
                    C.CustomerName,
                    C.ContactName AS CustomerContactName,
                    C.Email AS CustomerEmail,
                    C.Phone AS CustomerPhone,
                    C.Address AS CustomerAddress,
                    E.FullName AS EmployeeName,
                    S.ShipperName,
                    S.Phone AS ShipperPhone
                FROM Orders O
                LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID
                LEFT JOIN Shippers S ON O.ShipperID = S.ShipperID
                WHERE O.OrderID = @orderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        /// <summary>
        /// Bổ sung đơn hàng mới
        /// </summary>
        /// <param name="data">Thông tin đơn hàng</param>
        /// <returns>Mã đơn hàng vừa tạo</returns>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Orders
                           (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress,
                            EmployeeID, AcceptTime, ShipperID, ShippedTime,
                            FinishedTime, Status)
                           VALUES
                           (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress,
                            @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime,
                            @FinishedTime, @Status);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        /// <param name="data">Thông tin đơn hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Orders
                SET CustomerID = @CustomerID,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress
                WHERE OrderID = @OrderID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Orders
                  WHERE OrderID = @orderID",
                new { orderID });

            return rows > 0;
        }

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách mặt hàng</returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<OrderDetailViewInfo>(
                @"SELECT d.*, p.ProductName
                  FROM OrderDetails d
                  JOIN Products p ON d.ProductID = p.ProductID
                  WHERE d.OrderID = @orderID",
                new { orderID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin chi tiết mặt hàng</returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                @"SELECT d.*, p.ProductName
                  FROM OrderDetails d
                  JOIN Products p ON d.ProductID = p.ProductID
                  WHERE d.OrderID = @orderID AND d.ProductID = @productID",
                new { orderID, productID });
        }

        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns>true nếu thêm thành công</returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                  VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)", data);

            return rows > 0;
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE OrderDetails
                  SET Quantity = @Quantity,
                      SalePrice = @SalePrice
                  WHERE OrderID = @OrderID
                    AND ProductID = @ProductID", data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM OrderDetails
                  WHERE OrderID = @orderID
                    AND ProductID = @productID",
                new { orderID, productID });

            return rows > 0;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng theo khách hàng
        /// </summary>
        /// <param name="customerID">Mã khách hàng</param>
        /// <returns></returns>
        public async Task<List<OrderViewInfo>> ListByCustomerAsync(int customerID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<OrderViewInfo>(
                @"SELECT o.*, c.CustomerName
          FROM Orders o
          LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
          WHERE o.CustomerID = @customerID
          ORDER BY o.OrderTime DESC",
                new { customerID });

            return data.ToList();
        }
    }
}