using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Common;
using SV22T1020293.Models.Partner;

namespace SV22T1020293.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến người giao hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        //private const int PAGE_SIZE = 5;

        /// <summary>
        /// Giao diện nhập điều kiện tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("ShipperSearchInput");

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
        /// Tìm kiếm người giao hàng
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = ApplicationContext.PageSize;

            input.SearchValue ??= "";

            var result = await PartnerDataService.ListShippersAsync(input);

            ApplicationContext.SetSessionData("ShipperSearchInput", input);

            return PartialView(result);
        }

        /// <summary>
        /// Bổ sung người giao hàng mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            //sử dụng ModeState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên của người giao hàng");

            // cac ô có thể để trống thì :
            //điều chỉnh lại các giá trị dữ liệu khác theo quy định/quy ước của app
            if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";

            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            // Yêu cầu DL vào csdl
            if (data.ShipperID == 0)
            {
                await PartnerDataService.AddShipperAsync(data);
            }
            else
            {
                await PartnerDataService.UpdateShipperAsync(data);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa người giao hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            //
            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));

            return View(model);
        }
    }
}