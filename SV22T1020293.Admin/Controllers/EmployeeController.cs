using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.HR;
using SV22T1020293.Models.Security;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến nhân viên
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class EmployeeController : Controller
    {
        // private const int PAGE_SIZE = 10; //Số dòng hiển thị trên mỗi trang

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách nhân viên
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "")
        {
            var input = new PaginationSearchInput()
            {
                Page = page,
                PageSize = ApplicationContext.PageSize,
                SearchValue = searchValue ?? ""
            };

            var result = await HRDataService.ListEmployeesAsync(input);
            ViewBag.SearchValue = input.SearchValue;

            return View(result);
        }

        /// <summary>
        /// Bổ sung nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu (thêm / cập nhật)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0
                    ? "Bổ sung nhân viên"
                    : "Cập nhật thông tin nhân viên";

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }

                    data.Photo = fileName;
                }

                // Tiền xử lý dữ liệu
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";
                data.RoleNames = "employee";
                // Lưu DB
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ.");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa nhân viên";

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Xác nhận xóa nhân viên
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(Employee data)
        {
            await HRDataService.DeleteEmployeeAsync(data.EmployeeID);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            var model = new ResetPasswordModel()
            {
                Id = employee.EmployeeID,
                DisplayName = employee.FullName,
                Email = employee.Email,
                IsActive = employee.IsWorking
            };

            return View(model);
        }

        /// <summary>
        /// Xử lý đổi mật khẩu cho nhân viên
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ResetPasswordModel model)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";

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

            var employee = await HRDataService.GetEmployeeAsync(model.Id);
            if (employee == null)
                return RedirectToAction("Index");

            model.DisplayName = employee.FullName;
            model.Email = employee.Email;
            model.IsActive = employee.IsWorking;

            if (!ModelState.IsValid)
                return View(model);

            string password = CryptHelper.HashMD5(model.NewPassword);
            bool result = await SecurityDataService.ChangePasswordAsync(employee.Email, password);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu thất bại");
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công";
            return RedirectToAction("ChangePassword", new { id = model.Id });
        }

        /// <summary>
        /// Phân quyền cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần phân quyền</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Thay đổi quyền hạn nhân viên";

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, List<string> roles)
        {
            ViewBag.Title = "Thay đổi quyền hạn nhân viên";

            var roleList = (roles ?? new List<string>()).ToList();

            if (!roleList.Contains("employee"))
                roleList.Add("employee");

            string roleNames = string.Join(",", roleList);
            bool result = await HRDataService.UpdateEmployeeRolesAsync(id, roleNames);

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Đổi quyền thất bại");
                return View(employee);
            }

            TempData["SuccessMessage"] = "Đổi quyền thành công";

            return RedirectToAction("ChangeRole", new { id = id });
        }
    }
}