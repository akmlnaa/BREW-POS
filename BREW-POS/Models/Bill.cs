using System;
using System.Collections.Generic;

namespace BREW_POS.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int? OrderId { get; set; }

    public string? BillCode { get; set; }

    public DateTime? BillDate { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public decimal? GrandTotal { get; set; }

    public string? Status { get; set; }

    public string? BillName { get; set; }

    public virtual ICollection<BillDetail> BillDetails { get; set; } = new List<BillDetail>();

    public virtual Order? Order { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
