using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến nhà cung cấp
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
       // private const int PAGE_SIZE = 5;

        /// <summary>
        /// Giao diện để nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("SupplierSearchInput");

            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm nhà cung cấp và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = ApplicationContext.PageSize;

            input.SearchValue ??= "";

            var result = await PartnerDataService.ListSuppliersAsync(input);

            ApplicationContext.SetSessionData("SupplierSearchInput", input);

            return PartialView(result);
        }

        /// <summary>
        /// Bổ sung nhà cung cấp mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            // --- BẮT BUỘC ---
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên của nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng cho biết Email của nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng nhập tỉnh/thành");

            // --- KHÔNG BẮT BUỘC ---
            if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";

            // Nếu có lỗi → quay lại form
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            // Lưu DB
            if (data.SupplierID == 0)
            {
                await PartnerDataService.AddSupplierAsync(data);
            }
            else
            {
                await PartnerDataService.UpdateSupplierAsync(data);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            //
            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));

            return View(model);
        }
    }
}