using SV22T1020293.Models.Sales;

namespace SV22T1020293.Shop
{
    /// <summary>
    /// Lớp cung cấp các chức năng xử lý trên dỏ hàng
    /// (giỏ hàng được lưu trong session)
    /// </summary>
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";

        // Lấy giỏ hàng từ session
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);

            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        //Lấy thông tin 1 mặt hàng từ giỏ hàng
        /// <summary>
        /// Laays thong tin 1 mawtj hangf trong gior hangf
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            return item;
        }

        //Thêm hàng vào giỏ hàng
        /// <summary>
        /// Thêm hàng vào giỏ hàng
        /// </summary>
        /// <param name="item"></param>
        public static void AddItemToCart(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existItem = cart.Find(m => m.ProductID == item.ProductID);

            // neeus tim khong thY BO SUNG MOI
            if (existItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existItem.Quantity += item.Quantity;
                existItem.SalePrice = item.SalePrice;
            }

            ApplicationContext.SetSessionData(CART, cart);
        }

        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);

            if (item == null)
                return;

            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
            }

            ApplicationContext.SetSessionData(CART, cart);
        }

        //xóa mặt hàng ra khỏi giỏ hàng
        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ hang
        /// </summary>
        /// <param name="productID"></param>
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);

            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        //xóa toan bộ giỏ hàng
        /// <summary>
        /// xóa toàn bộ giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            var newCart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, newCart);
        }
    }
}