using System;
using System.Collections.Generic;

namespace BREW_POS.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? MenuId { get; set; }

    public int? Qty { get; set; }

    public decimal? Price { get; set; }

    public decimal? Subtotal { get; set; }

    public virtual ListMenu? Menu { get; set; }

    public virtual Order? Order { get; set; }
}
