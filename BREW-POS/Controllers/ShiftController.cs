using BREW_POS.Data;
using BREW_POS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BREW_POS.Controllers
{
    public class ShiftController : Controller
    {
        private readonly AppDbContext _context;

        public ShiftController(AppDbContext context)
        {
            _context = context;
        }

        // CURRENT SHIFT
        public async Task<IActionResult> Current()
        {
            var currentShift = await _context.Shifts
                .FirstOrDefaultAsync(s => !s.IsClosed);

            if (currentShift == null)
            {
                return View(null);
            }

            var payments = await _context.Payments
                .Include(p => p.Bill)
                .Where(p =>
                    p.PaymentStatus == "SUCCESS" &&
                    p.PaymentDate >= currentShift.StartTime)
                .ToListAsync();

            ViewBag.SoldItems = await _context.BillDetails
                .Include(d => d.Bill)
                .Where(d =>
                    d.Bill.Status == "PAID" &&
                    d.Bill.BillDate >= currentShift.StartTime)
                .SumAsync(d => d.Qty ?? 0);

            ViewBag.CashSales = payments
                .Where(p => p.PaymentMethod == "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.NonCashSales = payments
                .Where(p => p.PaymentMethod != "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.TotalExpected = payments
                .Sum(p => p.PaidAmount ?? 0);

            return View(currentShift);
        }
        [HttpPost]
        public IActionResult StartShift(string shiftName)
        {
            var activeShift = _context.Shifts
                .FirstOrDefault(s => !s.IsClosed);

            if (activeShift != null)
            {
                return RedirectToAction("Current");
            }

            var shift = new Shift
            {
                ShiftName = shiftName,
                StartTime = DateTime.Now,
                TotalSales = 0,
                IsClosed = false
            };

            _context.Shifts.Add(shift);
            _context.SaveChanges();

            return RedirectToAction("Current");
        }
        [HttpPost]
        public IActionResult EndShift(int id)
        {
            var shift = _context.Shifts
                .FirstOrDefault(s => s.ShiftId == id);

            if (shift != null)
            {
                shift.EndTime = DateTime.Now;
                shift.IsClosed = true;

                _context.SaveChanges();
            }

            return RedirectToAction("Current");
        }
        public async Task<IActionResult> Details(int id)
        {
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
            {
                return RedirectToAction("History");
            }

            var endTime = shift.EndTime ?? DateTime.Now;

            var payments = await _context.Payments
                .Include(p => p.Bill)
                .Where(p =>
                    p.PaymentStatus == "SUCCESS" &&
                    p.PaymentDate >= shift.StartTime &&
                    p.PaymentDate <= endTime)
                .ToListAsync();

            ViewBag.SoldItems = await _context.BillDetails
                .Include(d => d.Bill)
                .Where(d =>
                    d.Bill.Status == "PAID" &&
                    d.Bill.BillDate >= shift.StartTime &&
                    d.Bill.BillDate <= endTime)
                .SumAsync(d => d.Qty ?? 0);

            ViewBag.CashSales = payments
                .Where(p => p.PaymentMethod == "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.NonCashSales = payments
                .Where(p => p.PaymentMethod != "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.TotalExpected = payments
                .Sum(p => p.PaidAmount ?? 0);

            return View(shift);
        }
        public async Task<IActionResult> History()
        {
            var shifts = await _context.Shifts
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return View(shifts);
        }
    }
}