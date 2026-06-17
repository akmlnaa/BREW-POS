using BREW_POS.Data;
using BREW_POS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;

namespace BREW_POS.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var paidBillsToday = await _context.Bills
                .Where(b => b.Status == "PAID"
                    && b.BillDate >= today
                    && b.BillDate < tomorrow)
                .ToListAsync();

            ViewBag.TotalSalesToday =
                paidBillsToday.Sum(b => b.GrandTotal ?? 0);

            ViewBag.TotalTransactionsToday =
                paidBillsToday.Count;

            ViewBag.TotalMenus =
                await _context.ListMenus.CountAsync();

            ViewBag.SoldOutMenus =
                await _context.ListMenus
                    .CountAsync(m => m.IsAvailable == false);

            ViewBag.ActiveShift =
                await _context.Shifts
                    .FirstOrDefaultAsync(s => !s.IsClosed);

            ViewBag.RecentTransactions =
                await _context.Bills
                    .Where(b => b.Status == "PAID")
                    .OrderByDescending(b => b.BillDate)
                    .Take(5)
                    .ToListAsync();

            return View();
        }
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .ToListAsync();

            return View(users);
        }
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Users");
            }

            return View(user);
        }
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return RedirectToAction("Users");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (existingUser == null)
            {
                return RedirectToAction("Users");
            }

            existingUser.FullName = user.FullName;
            existingUser.Username = user.Username;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                existingUser.PasswordHash = user.PasswordHash;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Users");
        }
        // =========================
        // MENU MANAGEMENT ADMIN
        // =========================
        public async Task<IActionResult> Menus()
        {
            var menus = await _context.ListMenus
                .OrderBy(m => m.MenuName)
                .ToListAsync();

            return View(menus);
        }

        public IActionResult CreateMenu()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenu(ListMenu menu)
        {
            if (ModelState.IsValid)
            {
                menu.IsAvailable = true;

                _context.ListMenus.Add(menu);
                await _context.SaveChangesAsync();

                return RedirectToAction("Menus");
            }

            return View(menu);
        }

        public async Task<IActionResult> EditMenu(int id)
        {
            var menu = await _context.ListMenus
                .FirstOrDefaultAsync(m => m.MenuId == id);

            if (menu == null)
            {
                return RedirectToAction("Menus");
            }

            return View(menu);
        }

        [HttpPost]
        public async Task<IActionResult> EditMenu(ListMenu menu)
        {
            var existingMenu = await _context.ListMenus
                .FirstOrDefaultAsync(m => m.MenuId == menu.MenuId);

            if (existingMenu == null)
            {
                return RedirectToAction("Menus");
            }

            existingMenu.MenuName = menu.MenuName;
            existingMenu.Price = menu.Price;
            existingMenu.Category = menu.Category;
            existingMenu.IsAvailable = menu.IsAvailable;

            await _context.SaveChangesAsync();

            return RedirectToAction("Menus");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.ListMenus
                .FirstOrDefaultAsync(m => m.MenuId == id);

            if (menu == null)
            {
                return RedirectToAction("Menus");
            }

            var usedInTransaction = await _context.BillDetails
                .AnyAsync(d => d.MenuId == id);

            if (usedInTransaction)
            {
                menu.IsAvailable = false;
                await _context.SaveChangesAsync();

                TempData["Error"] = "Menu tidak bisa dihapus karena sudah pernah digunakan pada transaksi. Menu dinonaktifkan menjadi Sold Out.";
                return RedirectToAction("Menus");
            }

            _context.ListMenus.Remove(menu);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu berhasil dihapus.";
            return RedirectToAction("Menus");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMenuAvailability(int id)
        {
            var menu = await _context.ListMenus
                .FirstOrDefaultAsync(m => m.MenuId == id);

            if (menu == null)
            {
                return RedirectToAction("Menus");
            }

            menu.IsAvailable = !(menu.IsAvailable ?? true);

            await _context.SaveChangesAsync();

            return RedirectToAction("Menus");
        }
        // =========================
        // SALES REPORT ADMIN
        // =========================
        public async Task<IActionResult> SalesReport(DateTime? startDate, DateTime? endDate)
        {
            var bills = _context.Bills
                .Include(b => b.Payments)
                .Where(b => b.Status == "PAID")
                .AsQueryable();

            if (startDate.HasValue)
            {
                bills = bills.Where(b => b.BillDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.AddDays(1);
                bills = bills.Where(b => b.BillDate < end);
            }

            var result = await bills
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalSales = result.Sum(b => b.GrandTotal ?? 0);
            ViewBag.TotalTransactions = result.Count;

            return View(result);
        }
        // =========================
        // SHIFT HISTORY ADMIN
        // =========================
        public async Task<IActionResult> ShiftHistory()
        {
            var shifts = await _context.Shifts
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return View(shifts);
        }
        public async Task<IActionResult> TransactionDetail(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                    .ThenInclude(d => d.Menu)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
            {
                return RedirectToAction("SalesReport");
            }

            return View(bill);
        }
        // =========================
        // SHIFT DETAIL ADMIN
        // =========================
        public async Task<IActionResult> ShiftDetail(int id)
        {
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
            {
                return RedirectToAction("ShiftHistory");
            }

            var start = shift.StartTime;
            var end = shift.EndTime ?? DateTime.Now;

            var payments = await _context.Payments
                .Include(p => p.Bill)
                .Where(p =>
                    p.PaymentStatus == "SUCCESS" &&
                    p.PaymentDate >= start &&
                    p.PaymentDate <= end)
                .ToListAsync();

            ViewBag.CashSales = payments
                .Where(p => p.PaymentMethod == "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.NonCashSales = payments
                .Where(p => p.PaymentMethod != "Cash")
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.TotalSales = payments
                .Sum(p => p.PaidAmount ?? 0);

            ViewBag.TotalTransactions = payments.Count;

            ViewBag.TotalItemSold = await _context.BillDetails
                .Include(d => d.Bill)
                .Where(d =>
                    d.Bill.Status == "PAID" &&
                    d.Bill.BillDate >= start &&
                    d.Bill.BillDate <= end)
                .SumAsync(d => d.Qty ?? 0);

            ViewBag.Payments = payments;

            return View(shift);
        }
        public async Task<IActionResult> ExportSalesReportExcel(DateTime? startDate, DateTime? endDate)
        {
            var bills = _context.Bills
                .Include(b => b.Payments)
                .Where(b => b.Status == "PAID")
                .AsQueryable();

            if (startDate.HasValue)
            {
                bills = bills.Where(b => b.BillDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.AddDays(1);
                bills = bills.Where(b => b.BillDate < end);
            }

            var data = await bills
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sales Report");

            worksheet.Cell(1, 1).Value = "BREW POS - Sales Report";
            worksheet.Range(1, 1, 1, 6).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Cell(3, 1).Value = "Bill Code";
            worksheet.Cell(3, 2).Value = "Bill Name";
            worksheet.Cell(3, 3).Value = "Date";
            worksheet.Cell(3, 4).Value = "Payment Method";
            worksheet.Cell(3, 5).Value = "Total";
            worksheet.Cell(3, 6).Value = "Status";

            var header = worksheet.Range(3, 1, 3, 6);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 4;

            foreach (var item in data)
            {
                var payment = item.Payments.FirstOrDefault();

                worksheet.Cell(row, 1).Value = item.BillCode;
                worksheet.Cell(row, 2).Value = item.BillName;
                worksheet.Cell(row, 3).Value = item.BillDate?.ToString("dd MMM yyyy HH:mm");
                worksheet.Cell(row, 4).Value = payment?.PaymentMethod ?? "-";
                worksheet.Cell(row, 5).Value = item.GrandTotal ?? 0;
                worksheet.Cell(row, 6).Value = item.Status;

                row++;
            }

            worksheet.Cell(row + 1, 4).Value = "Total Sales";
            worksheet.Cell(row + 1, 4).Style.Font.Bold = true;
            worksheet.Cell(row + 1, 5).Value = data.Sum(x => x.GrandTotal ?? 0);
            worksheet.Cell(row + 1, 5).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BREW_POS_Sales_Report.xlsx"
            );
        }
        // =========================
        // PRODUCT SALES REPORT ADMIN
        // =========================
        public async Task<IActionResult> ProductSalesReport(DateTime? startDate, DateTime? endDate)
        {
            var details = _context.BillDetails
                .Include(d => d.Bill)
                .Include(d => d.Menu)
                .Where(d => d.Bill.Status == "PAID")
                .AsQueryable();

            if (startDate.HasValue)
            {
                details = details.Where(d => d.Bill.BillDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.AddDays(1);
                details = details.Where(d => d.Bill.BillDate < end);
            }

            var report = await details
                .GroupBy(d => new
                {
                    d.MenuId,
                    MenuName = d.Menu.MenuName
                })
                .Select(g => new
                {
                    MenuName = g.Key.MenuName,
                    QtySold = g.Sum(x => x.Qty ?? 0),
                    TotalSales = g.Sum(x => x.Subtotal ?? 0)
                })
                .OrderByDescending(x => x.QtySold)
                .ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalQty = report.Sum(x => x.QtySold);
            ViewBag.TotalSales = report.Sum(x => x.TotalSales);

            return View(report);
        }
        public async Task<IActionResult> ExportProductSalesReportExcel(
    DateTime? startDate,
    DateTime? endDate)
        {
            var details = _context.BillDetails
                .Include(d => d.Bill)
                .Include(d => d.Menu)
                .Where(d => d.Bill.Status == "PAID")
                .AsQueryable();

            if (startDate.HasValue)
            {
                details = details.Where(d =>
                    d.Bill.BillDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.AddDays(1);

                details = details.Where(d =>
                    d.Bill.BillDate < end);
            }

            var report = await details
                .GroupBy(d => new
                {
                    d.MenuId,
                    MenuName = d.Menu.MenuName
                })
                .Select(g => new
                {
                    MenuName = g.Key.MenuName,
                    QtySold = g.Sum(x => x.Qty ?? 0),
                    TotalSales = g.Sum(x => x.Subtotal ?? 0)
                })
                .OrderByDescending(x => x.QtySold)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Product Sales");

            worksheet.Cell(1, 1).Value = "BREW POS - Product Sales Report";
            worksheet.Range(1, 1, 1, 4).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Cell(3, 1).Value = "No";
            worksheet.Cell(3, 2).Value = "Menu";
            worksheet.Cell(3, 3).Value = "Qty Sold";
            worksheet.Cell(3, 4).Value = "Total Sales";

            var header = worksheet.Range(3, 1, 3, 4);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 4;
            int no = 1;

            foreach (var item in report)
            {
                worksheet.Cell(row, 1).Value = no++;
                worksheet.Cell(row, 2).Value = item.MenuName;
                worksheet.Cell(row, 3).Value = item.QtySold;
                worksheet.Cell(row, 4).Value = item.TotalSales;

                row++;
            }

            worksheet.Cell(row + 1, 2).Value = "TOTAL";
            worksheet.Cell(row + 1, 2).Style.Font.Bold = true;

            worksheet.Cell(row + 1, 3).Value = report.Sum(x => x.QtySold);
            worksheet.Cell(row + 1, 3).Style.Font.Bold = true;

            worksheet.Cell(row + 1, 4).Value = report.Sum(x => x.TotalSales);
            worksheet.Cell(row + 1, 4).Style.Font.Bold = true;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BREW_POS_Product_Sales_Report.xlsx"
            );
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Users");
        }
    }
}