using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.DataLayers.Interfaces;
using SV22T1020293.DataLayers.SQLServer;
using SV22T1020293.Models.Partner;
using SV22T1020293.Models.Security;
using SV22T1020293.Shop;

namespace SV22T1020293.Shop.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tài khoản
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// Giao diện đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// xử lý đăng nhập
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            //Kiểm tra người dùng đã nhập đủ thông tin cần thiết chưa
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Nhập đủ tên và mật khẩu");
                return View();
            }
            // mã hóa MD5 password
            password = CryptHelper.HashMD5(password);
           //Kiểm tra người dùng đăng nhập đúng chưa
            var userAccount = await SecurityDataService.AuthorizeAsync(username, password);
            //Xử lý nếu đăng nhập sai
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Sai khoản hoặc mật khẩu");
                return View();
            }
            //Kiểm tra người dùng có phải sài tài khoản customer không
            if (!userAccount.RoleNames.Contains("Customer"))
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại");
                return View();
            }
            // DL sẽ dùng để ghi vào giấy chứng nhận  (principal)
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
            };
            // thiết lập phiên đăng nhập (cấp giấy chứng nhận )
            await HttpContext.SignInAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                userData.CreatePrincipal()
            );

            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // Kiểm tra mật khẩu rỗng
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu");
                return View(model);
            }
            // Kiểm tra độ dài mật khẩu
            if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự");
                return View(model);
            }
            // Kiểm tra xác nhận mật khẩu
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");
                return View(model);
            }
            //Kiểm tra số điện thoại
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại");
                return View(model);
            }


            // Xóa khoảng trắng
            model.Phone = model.Phone.Trim();
            model.CustomerName = model.CustomerName.Trim();
            model.ContactName = model.ContactName.Trim();
            model.Email = model.Email.Trim();
            // Chỉ cho nhập số
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^0\d{9}$|^0\d{10}$"))
            {
                ModelState.AddModelError("Phone", "Số điện thoại không hợp lệ");
                return View(model);
            }
            // Kiểm tra email có tồn tại chưa
            bool emailValid = await PartnerDataService.ValidatelCustomerEmailAsync(model.Email);
            if (!emailValid)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View(model);
            }
            // Hash password
            string passwordHash = CryptHelper.HashMD5(model.Password);
            // Tạo khách hàng mới
            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                ContactName = model.ContactName,
                Email = model.Email,
                Password = passwordHash,
                Phone = model.Phone ?? "",
                Address = model.Address ?? "",
                Province = model.Province ?? "",
                IsLocked = false
            };
            int customerId = await PartnerDataService.AddCustomerAsync(customer);
            if (customerId <= 0)
            {
                ModelState.AddModelError("Error", "Đăng ký thất bại, vui lòng thử lại sau");
                return View(model);
            }
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordModel());
        }

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(model.OldPassword))
                ModelState.AddModelError(nameof(model.OldPassword), "Vui lòng nhập mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Vui lòng nhập lại mật khẩu mới");

            if (model.NewPassword.Length < 6)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu phải có ít nhất 6 ký tự");
                return View(model);
            }
            if (!string.IsNullOrWhiteSpace(model.NewPassword) &&
                !string.IsNullOrWhiteSpace(model.ConfirmPassword) &&
                model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
            }

            if (!ModelState.IsValid)
                return View(model);

            var userData = User.GetUserData();
            if (userData == null)
                return RedirectToAction("Login");

            string oldPasswordHash = CryptHelper.HashMD5(model.OldPassword);
            var account = await SecurityDataService.AuthorizeAsync(userData.UserName ?? "", oldPasswordHash);
            if (account == null)
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Mật khẩu cũ không đúng");
                return View(model);
            }

            // Hash mật khẩu mới
            string newPasswordHash = CryptHelper.HashMD5(model.NewPassword);
            bool result = await SecurityDataService.ChangePasswordAsync(userData.UserName ?? "", newPasswordHash);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu thất bại");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Đổi mật khẩu thành công");
            return View(model);
        }
        /// <summary>
        /// Hiển thị thông tin cá nhân của người dùng đang đăng nhập
        /// </summary>
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();

            if (userData == null)
            {
                return RedirectToAction("Login");
            }
            var model = await PartnerDataService.GetCustomerAsync(Convert.ToInt32(userData.UserId));
            return View(model);
        }
        /// <summary>
        /// Cập nhật thông tin cá nhân
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Customer model)
        {
            var userData = User.GetUserData();

            if (userData == null)
                return RedirectToAction("Login");

            model.CustomerID = Convert.ToInt32(userData.UserId);

            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError(nameof(model.CustomerName), "Vui lòng nhập họ tên");

            if (string.IsNullOrWhiteSpace(model.ContactName))
                ModelState.AddModelError(nameof(model.ContactName), "Vui lòng nhập tên giao dịch");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Vui lòng nhập email");

            if (string.IsNullOrWhiteSpace(model.Province))
                ModelState.AddModelError(nameof(model.Province), "Vui lòng chọn tỉnh / thành");

            bool emailValid = await PartnerDataService.ValidatelCustomerEmailAsync(model.Email, model.CustomerID);
            if (!emailValid)
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");

            if (!ModelState.IsValid)
                return View("Profile", model);

            bool result = await PartnerDataService.UpdateCustomerAsync(model);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Cập nhật thông tin thất bại");
                return View("Profile", model);
            }

            var newUserData = new WebUserData()
            {
                UserId = model.CustomerID.ToString(),
                UserName = userData.UserName,
                DisplayName = model.CustomerName,
                Email = model.Email
            };

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                newUserData.CreatePrincipal()
            );

            ModelState.AddModelError(string.Empty, "Cập nhật thông tin thành công");

            return View("Profile", model);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}