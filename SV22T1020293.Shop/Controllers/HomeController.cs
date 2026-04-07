using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Catalog;
using SV22T1020293.Models.Common;
using SV22T1020293.Shop.Models;

namespace SV22T1020293.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var input = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 8 
            };

            var result = await CatalogDataService.ListProductsAsync(input);

            return View(result.DataItems);
        }

        /// <summary>
        /// Trang privacy
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Trang lỗi
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}