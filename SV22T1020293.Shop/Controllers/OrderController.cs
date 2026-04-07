using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV221020645.BusinessLayers;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Sales;

namespace SV22T1020293.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {

        /// <summary>
        /// Lịch sử đơn hàng của khách
        /// </summary>
        public async Task<IActionResult> History(string status = "")
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
                return RedirectToAction("Login", "Account");

            int customerID = int.Parse(userData.UserId);
            var orders = await SalesDataService.ListOrdersByCustomerAsync(customerID);

            // Lọc trạng thái nếu có
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatusEnum>(status, out var orderStatus))
            {
                orders = orders.Where(o => o.Status == orderStatus).ToList();
            }

            // Tính tổng tiền từng đơn
            var totalAmounts = new Dictionary<int, decimal>();
            foreach (var order in orders)
            {
                var details = await SalesDataService.ListDetailsAsync(order.OrderID);
                totalAmounts[order.OrderID] = details?.Sum(d => d.TotalPrice) ?? 0;
            }

            ViewBag.TotalAmounts = totalAmounts;
            ViewBag.SelectedStatus = status;

            return View(orders);
        }

        /// <summary>
        /// Xem chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var userData = User.GetUserData();

            if (userData == null || string.IsNullOrEmpty(userData.UserId))
                return RedirectToAction("Login", "Account");

            int customerID = int.Parse(userData.UserId);

            // Lấy thông tin đơn hàng
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng");

            // Chặn xem đơn của người khác
            if (order.CustomerID != customerID)
                return Forbid();

            // Lấy chi tiết sản phẩm trong đơn
            var details = await SalesDataService.ListDetailsAsync(id);

            // Lấy thông tin shipper
            if (order.ShipperID.HasValue && order.ShipperID.Value > 0)
            {
                var shipper = await PartnerDataService.GetShipperAsync(order.ShipperID.Value);
                ViewBag.Shipper = shipper;
            }

            ViewBag.Order = order;
            return View(details);
        }
    }
}