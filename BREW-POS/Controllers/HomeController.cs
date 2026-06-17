using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BREW_POS.Models;
using Microsoft.EntityFrameworkCore;
using BREW_POS.Data;
using Microsoft.AspNetCore.Http;

namespace BREW_POS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    public IActionResult AdminDashboard()
    {
        return View();
    }

    public async Task<IActionResult> KasirDashboard()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var paidBillsToday = await _context.Bills
            .Where(b =>
                b.Status == "PAID" &&
                b.BillDate >= today &&
                b.BillDate < tomorrow)
            .ToListAsync();

        ViewBag.TotalSalesToday =
            paidBillsToday.Sum(b => b.GrandTotal ?? 0);

        ViewBag.TotalOrdersToday =
            paidBillsToday.Count;

        ViewBag.OpenBills =
            await _context.Bills.CountAsync(b => b.Status == "OPEN");

        ViewBag.ActiveShift =
            await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);

        ViewBag.TopSelling = await _context.BillDetails
    .Include(x => x.Menu)
    .Include(x => x.Bill)
    .Where(x =>
        x.Bill.Status == "PAID" &&
        x.Bill.BillDate >= today &&
        x.Bill.BillDate < tomorrow)
    .GroupBy(x => x.Menu.MenuName)
    .Select(g => new
    {
        MenuName = g.Key,
        QtySold = g.Sum(x => x.Qty ?? 0)
    })
    .OrderByDescending(x => x.QtySold)
    .Take(5)
    .ToListAsync();

        ViewBag.ActiveOpenBills = await _context.Bills
            .Where(x => x.Status == "OPEN")
            .OrderByDescending(x => x.BillDate)
            .Take(5)
            .ToListAsync();

        ViewBag.SoldOutMenus = await _context.ListMenus
            .Where(x => x.IsAvailable == false)
            .Take(10)
            .ToListAsync();

        return View();
    }
    public IActionResult Setting()
    {
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Role = HttpContext.Session.GetString("Role");

        return View();
    }
}
