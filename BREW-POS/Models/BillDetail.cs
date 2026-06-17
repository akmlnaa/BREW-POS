using System;
using System.Collections.Generic;

namespace BREW_POS.Models;

public partial class BillDetail
{
    public int BillDetailId { get; set; }

    public int? BillId { get; set; }

    public int? MenuId { get; set; }

    public int? Qty { get; set; }

    public decimal? Price { get; set; }

    public decimal? Subtotal { get; set; }
    public string? Note { get; set; }

    public virtual Bill? Bill { get; set; }

    public virtual ListMenu? Menu { get; set; }
}
