namespace BREW_POS.Models
{
    public class CartItem
    {
        public int MenuId { get; set; }

        public string MenuName { get; set; }

        public decimal Price { get; set; }

        public int Qty { get; set; }
        public string? Note { get; set; }

        public decimal Subtotal
        {
            get { return Price * Qty; }
        }
    }
}