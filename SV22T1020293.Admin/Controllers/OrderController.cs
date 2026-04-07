using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV221020645.BusinessLayers;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Catalog;
using SV22T1020293.Models.Sales;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến nghiệp vụ bán hàng
    /// </summary>
    /// 

    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class OrderController : Controller
    {
        public const string SEARCH_ORDER = "SearchOrder";
        //private const int PAGE_SIZE = 10;

        /// <summary>
        /// Giao diện để nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = 10,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            }

            return View(input);
        }

        // Order/Search
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input.PageSize <= 0)
                input.PageSize = 10;

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(SEARCH_ORDER, input);

            return View(result);
        }

        private const string SEARCH_PRODUCT = "SearchProductToSale";

        //Giao diện thực hiện các chức năng để lập đơn hàng mới
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = 3;

            input.SearchValue ??= "";

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return PartialView(result);
        }

        //hiển thị giỏ hàng
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return PartialView(cart);
        }

        public IActionResult Action(int id)
        {
            return View();
        }

        /// <summary>
        /// Hiển thị thông tin của một đơn hàng và điều hướng đến các chức năng
        /// </summary>
        /// <param name="id">Mã của đơn hàng </param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                return RedirectToAction("Index"); // Nấu không thấy đơn hàng thì quay về danh sách
            }

            // 2. Lấy danh sách mặt hàng thuộc đơn hàng này
            var details = await SalesDataService.ListDetailsAsync(id);

            // Truyền chi tiết qua ViewBag, còn thông tin đơn truyền bằng Model
            ViewBag.Details = details;

            return View(order);
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            //TODO: kiem tra du lieu
            if (productId <= 0)
                return Json(new ApiResult(0, "Mặt hàng không hợp lệ"));

            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

            if (price <= 0)
                return Json(new ApiResult(0, "Giá bán phải lớn hơn 0"));

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
                return Json(new ApiResult(0, "Mat hang nay khong ton tai"));

            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mat hang nay da ngung ban"));

            // them hang vao gio
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
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Cập nhật thông tin của một mặt hàng trong giỏ hàng 
        /// </summary>
        /// <param name="id">0: Xử lý giỏ hàng, khác 0: xử lý cho đơn hàng</param>
        /// <param name="productId">Mã mặt hàng cần xử lý</param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            if (item == null)
                return Content("Không tìm thấy mặt hàng trong giỏ");

            return PartialView(item);
        }

        [HttpPost]
        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            //TODO: kiểm tra dữ liệu
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

            if (salePrice <= 0)
                return Json(new ApiResult(0, "Giá hàng phải lớn hơn 0"));

            //Update trong giỏ hàng
            ShoppingCartHelper.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

        //#endregion
        // xem và xử lý đơn hàng

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            //TODO: kiểm tra dữ liệu hợp lệ
            var cart = ShoppingCartHelper.GetShoppingCart();
            // Kiểm tra giỏ hàng
            if (cart == null || cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng đang trống, không thể lập đơn."));
            }
            if (string.IsNullOrWhiteSpace(province))
                return Json(new ApiResult(0, "Vui lòng chọn tỉnh/thành"));

            if (string.IsNullOrWhiteSpace(address))
                return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));

            // Lập đơn hàng và ghi chi tiết đơn hàng
            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address
            };

            int orderID = await SalesDataService.AddOrderAsync(order);
            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice,
                });
            }

            ShoppingCartHelper.ClearCart();
            return Json(new ApiResult(orderID, ""));
        }

        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ hàng hoặc ra khỏi đơn hàng
        /// </summary>
        /// <param name="id">0: Xử lý trong giỏ hàng, khác 0: Xử lý cho đơn hàng</param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            //POST: Xoá khỏi giỏ
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }
            //GET: Hiển thị hộp thoại để xác nhận
            ViewBag.ProductID = productId;
            return PartialView();
        }

        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }

            return PartialView();
        }

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Accept(int id) 
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Accept(int id, IFormCollection form)
        {
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            await SalesDataService.AcceptOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Chuyển hàng cho người giao hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần chuyển</param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Shipping(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0)
            {
                TempData["Message"] = "Vui lòng chọn người giao hàng!";
                return RedirectToAction("Detail", new { id = id });
            }
            await SalesDataService.ShipOrderAsync(id, shipperID);
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Kết thúc thành công
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Finish(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Finish(int id, IFormCollection form)
        {
            await SalesDataService.CompleteOrderAsync(id);
            return RedirectToAction("Detail", new { id = id });
        }


        [HttpGet]
        public IActionResult Reject(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Reject(int id, IFormCollection form)
        {
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            await SalesDataService.RejectOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }



        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// 
        [HttpGet]
        public IActionResult Cancel(int id)  // 👈 ĐỔI TÊN
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, IFormCollection form)
        {
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);
            await SalesDataService.CancelOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xóa</param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id, IFormCollection form)
        {
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result)
                return RedirectToAction("Index"); // Xóa thành công thì đá về trang danh sách

            return RedirectToAction("Detail", new { id = id });
        }
    }
}