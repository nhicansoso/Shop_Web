using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;
using SV22T1020293.Models.Security;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace SV22T1020293.Admin.Controllers
{   ///<summary>
    /// Tên biến session lưu điều kiện tìm kiếm khách hàng
    /// chỉ sales và admin được phép vào / ds các quyền cách nhau bởi dẩu ","
    /// </summary>

    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class CustomerController : Controller
    {

        //private const int PAGE_SIZE = 10;

        /// <summary>
        /// Giao diện để nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        /// 
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("CustomerSearchInput");
            if(input == null)
            input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = ApplicationContext.PageSize,
                SearchValue = "",
            };
            return View(input);
        }

        /// <summary>
        /// TÌm kiếm khách hàng và trả về kq dưới dạng phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            //Lấy trang và giá trị tìm kiếm
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = ApplicationContext.PageSize;

            input.SearchValue ??= "";

            //Gọi xuống ListCustomerAsync (BusinessLayers)
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData("CustomerSearchInput", input);

            return PartialView(result);
        }

        /// <summary>
        /// Bổ sung khách hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit",model);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin Khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            //sử dụng ModeState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên của khách hàng");
            if(string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng cho biết Email của khách hàng");
            else if(!(await PartnerDataService.ValidatelCustomerEmailAsync(data.Email,data.CustomerID)))
                ModelState.AddModelError(nameof(data.Address), "Email này đã được sử dụng");

            // tinh thanh
            if(string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng nhập tỉnh/thành");

            // cac ô có thể để tróng thì :
            //điều chỉnh lại các giá trị dữ liệu khác theo quy định/quy ước của app
            if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";


            if(!ModelState.IsValid)
            {
                return View("Edit", data);
            }    




            // Yêu cầu DL vào csdl
            if (data.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(data);
            }    
            else
            {
                await PartnerDataService.UpdateCustomerAsync(data);
            }
            return RedirectToAction("Index");
        }

       // Catch (Exception ex)
       // {

            // Lưu log lỗi trong ex
         //   ModelState.AddModelError("Erro");
       // }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)

        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            //
            ViewBag.AllowDelete =  !(await PartnerDataService.IsUsedCustomerAsync(id));

            return View(model);
        }

        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần đổi mật khẩu</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Mật khẩu khách hàng";

            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return RedirectToAction("Index");

            var model = new ResetPasswordModel()
            {
                Id = customer.CustomerID,
                DisplayName = customer.CustomerName,
                Email = customer.Email,
                IsActive = !(customer.IsLocked ?? false)
            };

            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ResetPasswordModel model)
        {
            ViewBag.Title = "Mật khẩu khách hàng";

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Vui lòng nhập lại mật khẩu");

            if (!string.IsNullOrWhiteSpace(model.NewPassword) &&
                !string.IsNullOrWhiteSpace(model.ConfirmPassword) &&
                model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
            }

            var customer = await PartnerDataService.GetCustomerAsync(model.Id);
            if (customer == null)
                return RedirectToAction("Index");

            model.DisplayName = customer.CustomerName;
            model.Email = customer.Email;
            model.IsActive = !(customer.IsLocked ?? false);

            if (!ModelState.IsValid)
                return View(model);

            string password = CryptHelper.HashMD5(model.NewPassword);
            bool result = await SecurityDataService.ChangePasswordAsync(customer.Email, password);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu thất bại");
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công";
            return RedirectToAction("ChangePassword", new { id = model.Id });
        }

    }
}