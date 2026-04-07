using System;
using Microsoft.AspNetCore.Mvc;
using SV221020645.BusinessLayers;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Sales;

namespace SV22T1020293.Shop.Controllers
{
    public class CartController : Controller
    {
        private const string CART_SESSION_KEY = "ShoppingCart";

        /// <summary>
        /// Trang chính giỏ hàng
        /// </summary>
        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            if (productId <= 0)
                return Json(new ApiResult(0, "Mặt hàng không hợp lệ"));

            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

            if (price <= 0)
                return Json(new ApiResult(0, "Giá bán phải lớn hơn 0"));

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng này không tồn tại"));

            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng này đã ngừng bán"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = price
            };

            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, "Đã thêm vào giỏ hàng"));
        }

        /// <summary>
        /// Mua ngay: thêm vào giỏ rồi chuyển sang cart
        /// </summary>
        public async Task<IActionResult> BuyNow(int id, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null || !product.IsSelling)
                return RedirectToAction("Detail", "Product", new { id });

            var item = new OrderDetailViewInfo()
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "no-image.png",
                Quantity = quantity,
                SalePrice = product.Price
            };

            ShoppingCartHelper.AddItemToCart(item);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị form sửa mặt hàng trong giỏ
        /// </summary>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            if (item == null)
                return Content("Không tìm thấy mặt hàng trong giỏ");

            return PartialView(item);
        }

        /// <summary>
        /// Cập nhật số lượng / giá
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

            if (salePrice <= 0)
                return Json(new ApiResult(0, "Giá hàng phải lớn hơn 0"));

            ShoppingCartHelper.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, "Cập nhật thành công"));
        }

        /// <summary>
        /// Cập nhật nhanh số lượng từ trang giỏ hàng
        /// </summary>
        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            if (item == null)
                return RedirectToAction("Index");

            if (quantity <= 0)
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
            }
            else
            {
                ShoppingCartHelper.UpdateCartItem(productId, quantity, item.SalePrice);
            }

            TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa 1 mặt hàng khỏi giỏ
        /// </summary>
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            if (productId <= 0)
            {
                TempData["ErrorMessage"] = "Sản phẩm không hợp lệ";
                return RedirectToAction("Index");
            }

            ShoppingCartHelper.RemoveItemFromCart(productId);
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        public IActionResult Clear()
        {
            ShoppingCartHelper.ClearCart();
            TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Trang checkout
        /// </summary>
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();

            if (cart == null || !cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đang trống";
                return RedirectToAction("Index");
            }

            return View(cart);
        }

        /// <summary>
        /// Tạo đơn hàng theo dữ liệu checkout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(string province = "", string address = "")
        {
            var userData = User.GetUserData();
            if (userData == null)
                return RedirectToAction("Login");
            var customerID = Convert.ToInt32(userData.UserId);
            var cart = ShoppingCartHelper.GetShoppingCart();

            if (cart == null || cart.Count == 0)
            {
                ModelState.AddModelError("", "Giỏ hàng đang trống");
                return View("Checkout", cart);
            }

            if (string.IsNullOrWhiteSpace(province))
                ModelState.AddModelError(nameof(province), "Vui lòng chọn tỉnh/thành phố");

            if (string.IsNullOrWhiteSpace(address))
                ModelState.AddModelError(nameof(address), "Vui lòng nhập địa chỉ giao hàng");

            if (!ModelState.IsValid)
            {
                ViewBag.Province = province;
                ViewBag.Address = address;
                return View("Checkout", cart);
            }

            var order = new Order()
            {
             
                CustomerID = customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
            };

            int orderID = await SalesDataService.AddOrderAsync(order);

            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                });
            }

            ShoppingCartHelper.ClearCart();
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("Success", new { id = orderID });
        }

        /// <summary>
        /// Trang thành công
        /// </summary>
        public IActionResult Success(int id = 0)
        {
            ViewBag.OrderID = id;
            return View();
        }
    }
}