using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020293.BusinessLayers;
using SV22T1020293.Models.Catalog;

namespace SV22T1020293.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        //private const int PAGE_SIZE = 10;

        /// <summary>
        /// Giao diện nhập điều kiện tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>("ProductSearchInput");

            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách mặt hàng
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.Page < 1)
                input.Page = 1;

            if (input.PageSize <= 0)
                input.PageSize = ApplicationContext.PageSize;

            input.SearchValue ??= "";

            var result = await CatalogDataService.ListProductsAsync(input);

            ApplicationContext.SetSessionData("ProductSearchInput", input);

            return PartialView(result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            ViewBag.Title = "Chi tiết sản phẩm";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Thêm sản phẩm";
            var model = new Product()
            {
                ProductID = 0,
                ProductName = "",
                ProductDescription = "",
                SupplierID = 0,
                CategoryID = 0,
                Unit = "",
                Price = 0,
                Photo = "",
                IsSelling = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần cập nhật</param>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật sản phẩm";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.ProductID = id;
            ViewBag.ProductPhotos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductAttributes = await CatalogDataService.ListAttributesAsync(id);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {

            try
            {

                ViewBag.Title = data.ProductID == 0 ? "Thêm sản phẩm" : "Cập nhật sản phẩm";

                //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

                //sử dụng ModeState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên mặt hàng");

                if (data.CategoryID == null || data.CategoryID <= 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

                if (data.SupplierID == null || data.SupplierID <= 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");

                if (data.Price <= 0)
                    ModelState.AddModelError(nameof(data.Price), "Vui lòng nhập giá bán hợp lệ");

                // cac ô có thể để tróng thì :
                //điều chỉnh lại các giá trị dữ liệu khác theo quy định/quy ước của app
                if (string.IsNullOrEmpty(data.ProductDescription)) data.ProductDescription = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "";

                // xử lý upload ảnh
                if (uploadPhoto != null && uploadPhoto.Length > 0)
                {
                    string extension = Path.GetExtension(uploadPhoto.FileName);
                    string fileName = $"{Guid.NewGuid()}{extension}";
                    string folder = Path.Combine(ApplicationContext.WWWRootPath, "images", "products");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string filePath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }

                    data.Photo = fileName;
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ProductID = data.ProductID;
                    ViewBag.ProductPhotos = data.ProductID > 0
                        ? await CatalogDataService.ListPhotosAsync(data.ProductID)
                        : new List<ProductPhoto>();

                    ViewBag.ProductAttributes = data.ProductID > 0
                        ? await CatalogDataService.ListAttributesAsync(data.ProductID)
                        : new List<ProductAttribute>();

                    return View("Edit", data);
                }

                // Yêu cầu DL vào csdl
                if (data.ProductID == 0)
                {
                    await CatalogDataService.AddProductAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateProductAsync(data);
                }
                return RedirectToAction("Index");
            }

            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần xóa</param>
        public async Task<IActionResult> Delete(int id)
        {
            ViewBag.Title = "Xóa sản phẩm";

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedProductAsync(id));

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Product data)
        {
            await CatalogDataService.DeleteProductAsync(data.ProductID);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị danh sách các thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần lấy thuộc tính</param>
        public async Task<IActionResult> ListAttribute(int id)
        {
            ViewBag.Title = "Thuộc tính sản phẩm";
            ViewBag.ProductID = id;
            var model = await CatalogDataService.ListAttributesAsync(id);
            return View(model);
        }

        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";
            var model = new ProductAttribute()
            {
                AttributeID = 0,
                ProductID = id,
                AttributeName = "",
                AttributeValue = "",
                DisplayOrder = 1
            };
            return View("EditAttribute", model);
        }

        public async Task<IActionResult> EditAttribute(int id, int attribute)
        {
            ViewBag.Title = "Cập nhật thuộc tính";
            var model = await CatalogDataService.GetAttributeAsync(attribute);
            if (model == null)
                return RedirectToAction("Edit", new { id });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            //sử dụng ModeState để kiểm soát thông báo lỗi và gửi thông báo lỗi cho view
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Vui lòng nhập tên thuộc tính");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Vui lòng nhập giá trị thuộc tính");

            if (data.DisplayOrder <= 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Vui lòng nhập thứ tự hiển thị");

            if (!ModelState.IsValid)
            {
                return View("EditAttribute", data);
            }

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Xóa một thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có thuộc tính cần xóa</param>
        /// <param name="attribute">Mã thuộc tính cần xóa</param>
        public async Task<IActionResult> DeleteAttribute(int id, int attribute)
        {
            ViewBag.Title = "Xóa thuộc tính";
            await CatalogDataService.DeleteAttributeAsync(attribute);
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Hiển thị danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần lấy danh sách ảnh</param>
        public async Task<IActionResult> ListPhoto(int id)
        {
            ViewBag.Title = "Hình ảnh sản phẩm";
            ViewBag.ProductID = id;
            var model = await CatalogDataService.ListPhotosAsync(id);
            return View(model);
        }

        /// <summary>
        /// Bổ sung ảnh cho mặt hàng
        /// </summary>
        /// <param name="id">Mã của mặt hàng cần bổ sung ảnh</param>
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung ảnh";
            var model = new ProductPhoto()
            {
                PhotoID = 0,
                ProductID = id,
                Photo = "",
                Description = "",
                DisplayOrder = 1,
                IsHidden = false
            };
            return View("EditPhoto", model);
        }

        /// <summary>
        /// Cập nhật ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có ảnh cần cập nhật</param>
        /// <param name="photoId">Mã ảnh cần cập nhật</param>
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            ViewBag.Title = "Cập nhật ảnh";
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null)
                return RedirectToAction("Edit", new { id });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";

            //TODO : Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dl không hợp lệ

            if (data.ProductID <= 0)
                ModelState.AddModelError(nameof(data.ProductID), "Mặt hàng chưa được lưu. Vui lòng lưu mặt hàng trước khi bổ sung ảnh.");

            if (data.DisplayOrder <= 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Vui lòng nhập thứ tự hiển thị");

            if (string.IsNullOrWhiteSpace(data.Description))
                data.Description = "";

            if (uploadPhoto != null && uploadPhoto.Length > 0)
            {
                string extension = Path.GetExtension(uploadPhoto.FileName);
                string fileName = $"{Guid.NewGuid()}{extension}";
                string folder = Path.Combine(ApplicationContext.WWWRootPath, "images", "products");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                data.Photo = fileName;
            }

            if (data.PhotoID == 0 && string.IsNullOrWhiteSpace(data.Photo))
                ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn ảnh");

            if (!ModelState.IsValid)
            {
                return View("EditPhoto", data);
            }

            if (data.PhotoID == 0)
                await CatalogDataService.AddPhotoAsync(data);
            else
                await CatalogDataService.UpdatePhotoAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Xóa một ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có ảnh cần xóa</param>
        /// <param name="photoId">Mã ảnh cần xóa</param>
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToAction("Edit", new { id });
        }
    }
}