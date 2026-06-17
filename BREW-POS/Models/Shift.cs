using System.ComponentModel.DataAnnotations;

namespace BREW_POS.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        public string ShiftName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public decimal TotalSales { get; set; }

        public bool IsClosed { get; set; }
    }
}