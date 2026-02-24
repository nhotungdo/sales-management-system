namespace Sales_Management.ViewModels
{
    public class CustomerProfileViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CustomerLevel { get; set; }
        public decimal WalletBalance { get; set; }
        public string WalletStatus { get; set; }
        public DateTime? WalletUpdatedDate { get; set; }
        public List<WalletTransactionViewModel> Transactions { get; set; }
        public List<OrderHistoryViewModel> Orders { get; set; }
    }

    public class OrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string ProductNames { get; set; }
    }

    public class WalletTransactionViewModel
    {
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}