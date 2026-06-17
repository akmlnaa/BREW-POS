using System;
using System.Collections.Generic;

namespace BREW_POS.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BillId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? ChangeAmount { get; set; }

    public string? PaymentStatus { get; set; }

    public string? ProofImage { get; set; }

    public virtual Bill? Bill { get; set; }
}
