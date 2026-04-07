using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Catalog;

namespace SV22T1020293.Shop.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            var input = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 8,
                SearchValue = "",
                CategoryID = 0,
                SupplierID = 0,
                MinPrice = 0,
                MaxPrice = 0
            };

            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = 8;

            input.SearchValue ??= "";

            var result = await CatalogDataService.ListProductsAsync(input);

            return PartialView("Search", result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            ViewBag.Title = "Chi tiết sản phẩm";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
    }
}
