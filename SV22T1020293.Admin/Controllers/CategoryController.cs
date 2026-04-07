using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Catalog;
using SV22T1020293.Models.Common;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý loại hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        //private const int PAGE_SIZE = 10;

        /// <summary>
        /// Giao diện nhập điều kiện tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("CategorySearchInput");

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
        /// Tìm kiếm loại hàng
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = ApplicationContext.PageSize;

            input.SearchValue ??= "";

            var result = await CatalogDataService.ListCategoriesAsync(input);

            ApplicationContext.SetSessionData("CategorySearchInput", input);

            return PartialView(result);
        }

        /// <summary>
        /// Bổ sung loại hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            //sử dụng ModeState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên của loại hàng");

            // cac ô có thể để trống thì :
            //điều chỉnh lại các giá trị dữ liệu khác theo quy định/quy ước của app
            if (string.IsNullOrEmpty(data.Description)) data.Description = "";

            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            // Yêu cầu DL vào csdl
            if (data.CategoryID == 0)
            {
                await CatalogDataService.AddCategoryAsync(data);
            }
            else
            {
                await CatalogDataService.UpdateCategoryAsync(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            //
            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));

            return View(model);
        }
    }
}