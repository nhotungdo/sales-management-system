namespace Sales_Management.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string SessionId { get; set; } 
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiryTime { get; set; } 
        public virtual Product Product { get; set; }
    }
}