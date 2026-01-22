using System;
using System.Collections.Generic;

namespace Sales_Management.Models;

public partial class WalletTransaction
{
    public int TransactionId { get; set; }

    public int WalletId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = null!;

    public string? Method { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public decimal? AmountMoney { get; set; }

    public int? InvoiceId { get; set; }

    public virtual Invoice? Invoice { get; set; }

}
