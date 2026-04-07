using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.Models.Catalog;
using SV22T1020293.Models.Common;

namespace SV22T1020293.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm</param>
        /// <returns>Danh sách mặt hàng</returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                keyword = "%" + (input.SearchValue ?? "") + "%",
                categoryId = input.CategoryID,
                supplierId = input.SupplierID,
                minPrice = input.MinPrice,
                maxPrice = input.MaxPrice,
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize
            };

            string where = @"
        WHERE ProductName LIKE @keyword
        AND (@categoryId = 0 OR CategoryID = @categoryId)
        AND (@supplierId = 0 OR SupplierID = @supplierId)
        AND (@minPrice = 0 OR Price >= @minPrice)
        AND (@maxPrice = 0 OR Price <= @maxPrice)
    ";

            // 🔢 Đếm tổng số dòng
            int rowCount = await connection.ExecuteScalarAsync<int>($@"
        SELECT COUNT(*)
        FROM Products
        {where}
    ", parameters);

            // 📦 Lấy dữ liệu
            var data = await connection.QueryAsync<Product>($@"
        SELECT *
        FROM Products
        {where}
        ORDER BY ProductName
        OFFSET @offset ROWS
        FETCH NEXT @pagesize ROWS ONLY
    ", parameters);

            return new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin mặt hàng</returns>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Product>(
                @"SELECT *
                  FROM Products
                  WHERE ProductID = @productID",
                new { productID });
        }

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        /// <param name="data">Thông tin mặt hàng</param>
        /// <returns>Mã mặt hàng vừa được tạo</returns>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Products
                           (ProductName, ProductDescription, SupplierID, CategoryID,
                            Unit, Price, Photo, IsSelling)
                           VALUES
                           (@ProductName, @ProductDescription, @SupplierID, @CategoryID,
                            @Unit, @Price, @Photo, @IsSelling);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="data">Thông tin mặt hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE Products
                  SET ProductName = @ProductName,
                      ProductDescription = @ProductDescription,
                      SupplierID = @SupplierID,
                      CategoryID = @CategoryID,
                      Unit = @Unit,
                      Price = @Price,
                      Photo = @Photo,
                      IsSelling = @IsSelling
                  WHERE ProductID = @ProductID", data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM Products
                  WHERE ProductID = @productID",
                new { productID });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra mặt hàng có dữ liệu liên quan hay không
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>true nếu mặt hàng đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            int count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*)
                  FROM OrderDetails
                  WHERE ProductID = @productID",
                new { productID });

            return count > 0;
        }

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách thuộc tính</returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<ProductAttribute>(
                @"SELECT *
                  FROM ProductAttributes
                  WHERE ProductID = @productID",
                new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin của một thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>Thông tin thuộc tính</returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(
                @"SELECT *
                  FROM ProductAttributes
                  WHERE AttributeID = @attributeID",
                new { attributeID });
        }

        /// <summary>
        /// Bổ sung thuộc tính cho mặt hàng
        /// </summary>
        /// <param name="data">Thông tin thuộc tính</param>
        /// <returns>Mã thuộc tính vừa tạo</returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                           VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                           SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        /// <param name="data">Thông tin thuộc tính</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE ProductAttributes
                  SET AttributeName = @AttributeName,
                      AttributeValue = @AttributeValue,
                      DisplayOrder = @DisplayOrder
                  WHERE AttributeID = @AttributeID", data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa thuộc tính của mặt hàng
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM ProductAttributes
                  WHERE AttributeID = @attributeID",
                new { attributeID });

            return rows > 0;
        }

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách ảnh</returns>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            var data = await connection.QueryAsync<ProductPhoto>(
                @"SELECT *
                  FROM ProductPhotos
                  WHERE ProductID = @productID",
                new { productID });

            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin một ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>Thông tin ảnh</returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(
                @"SELECT *
                  FROM ProductPhotos
                  WHERE PhotoID = @photoID",
                new { photoID });
        }

        /// <summary>
        /// Bổ sung ảnh cho mặt hàng
        /// </summary>
        /// <param name="data">Thông tin ảnh</param>
        /// <returns>Mã ảnh vừa tạo</returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                           VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                           SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin ảnh của mặt hàng
        /// </summary>
        /// <param name="data">Thông tin ảnh</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"UPDATE ProductPhotos
                  SET Photo = @Photo,
                      Description = @Description,
                      DisplayOrder = @DisplayOrder,
                      IsHidden = @IsHidden
                  WHERE PhotoID = @PhotoID", data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            int rows = await connection.ExecuteAsync(
                @"DELETE FROM ProductPhotos
                  WHERE PhotoID = @photoID",
                new { photoID });

            return rows > 0;
        }
    }
}