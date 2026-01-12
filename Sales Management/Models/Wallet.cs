using System;
using System.Collections.Generic;

namespace Sales_Management.Models;

public partial class Wallet
{
    public int WalletId { get; set; }

    public int CustomerId { get; set; }

    public decimal? Balance { get; set; }

    public string? Status { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
