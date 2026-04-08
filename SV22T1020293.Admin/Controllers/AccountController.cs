using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tai khaonr
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// đăng nhập
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

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Nhập đủ tên và mật khẩu");
                return View();
            }

            // mã hóa MD5 password
            password = CryptHelper.HashMD5(password);

            var userAccount = await SecurityDataService.AuthorizeAsync(username, password);

            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại");
                return View();
            }
            if (userAccount.RoleNames.Contains("Customer"))
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
                Photo = string.IsNullOrWhiteSpace(userAccount.Photo) ? "nophoto.png" : userAccount.Photo,
                Roles = string.IsNullOrWhiteSpace(userAccount.RoleNames)
                            ? new List<string>()
                            : userAccount.RoleNames.Split(',').Select(x => x.Trim()).ToList()
            };

            // thiết lập phiên đăng nhập (cấp giấy chứng nhận )
            await HttpContext.SignInAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                userData.CreatePrincipal()
            );

            return RedirectToAction("Index", "Home");
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
            // Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(model.OldPassword))
                ModelState.AddModelError(nameof(model.OldPassword), "Vui lòng nhập mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Vui lòng nhập lại mật khẩu mới");

            if (!string.IsNullOrWhiteSpace(model.NewPassword) &&
                !string.IsNullOrWhiteSpace(model.ConfirmPassword) &&
                model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Mật khẩu xác nhận không khớp");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Lấy thông tin user
            var userData = User.GetUserData();
            if (userData == null)
                return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ
            string oldPasswordHash = CryptHelper.HashMD5(model.OldPassword);
            var account = await SecurityDataService.AuthorizeAsync(userData.UserName ?? "", oldPasswordHash);
            if (account == null)
            {
                ModelState.AddModelError(nameof(model.OldPassword), "Mật khẩu cũ không đúng");
                return View(model);
            }

            // Đổi mật khẩu mới
            string newPasswordHash = CryptHelper.HashMD5(model.NewPassword);
            bool result = await SecurityDataService.ChangePasswordAsync(userData.UserName ?? "", newPasswordHash);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu thất bại");
                return View(model);
            }
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công";

            return RedirectToAction("ChangePassword");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}