using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.DataLayers.SQLServer;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Sales;

namespace SV22T1020293.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nghiệp vụ bán hàng
    /// </summary>
    public static class OrderDataService
    {
        private static IOrderRepository orderDB
        {
            get { return new OrderRepository(Configuration.ConnectionString); }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public static async Task<int> CreateOrderAsync(Order data)
        {
            data.OrderTime = DateTime.Now;
            data.Status = OrderStatusEnum.New;
            return await orderDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            return await orderDB.DeleteAsync(orderID);
        }

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public static async Task<bool> AddOrderDetailAsync(OrderDetail data)
        {
            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderDetailAsync(OrderDetail data)
        {
            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderDetailAsync(int orderID, int productID)
        {
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New) return false;

            order.Status = OrderStatusEnum.Accepted;
            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Chuyển hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShippingOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.Accepted) return false;

            order.Status = OrderStatusEnum.Shipping;
            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> FinishOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.Shipping) return false;

            order.Status = OrderStatusEnum.Completed;
            order.FinishedTime = DateTime.Now;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status != OrderStatusEnum.New) return false;

            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;
            if (order.Status == OrderStatusEnum.Completed ||
                order.Status == OrderStatusEnum.Cancelled ||
                order.Status == OrderStatusEnum.Rejected)
                return false;

            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }
    }
}