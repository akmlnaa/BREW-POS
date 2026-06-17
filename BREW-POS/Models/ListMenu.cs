using System;
using System.Collections.Generic;

namespace BREW_POS.Models;

public partial class ListMenu
{
    public int MenuId { get; set; }

    public string? MenuName { get; set; }

    public string? Category { get; set; }

    public decimal? Price { get; set; }

    public int? Stock { get; set; }

    public string? Description { get; set; }

    public bool? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<BillDetail> BillDetails { get; set; } = new List<BillDetail>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
