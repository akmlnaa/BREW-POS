using BREW_POS.Data;
using BREW_POS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BREW_POS.Controllers
{
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;

        public MenuController(AppDbContext context)
        {
            _context = context;
        }

        // LIST MENU
        public async Task<IActionResult> Index()
        {
            var menu = await _context.ListMenus.ToListAsync();
            return View(menu);
        }

        // FORM CREATE
        public IActionResult Create()
        {
            return View();
        }

        // SAVE CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListMenu menu)
        {
            if (ModelState.IsValid)
            {
                _context.ListMenus.Add(menu);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Menu berhasil ditambahkan";

                // 🔥 BALIK KE HALAMAN POS
                return RedirectToAction(
                    "Create",
                    "Order"
                );
            }

            TempData["Error"] =
                "Gagal menambahkan menu";

            return RedirectToAction(
                "Create",
                "Order"
            );
        }
        // FORM EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.ListMenus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            return View(menu);
        }

        // SAVE EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ListMenu menu)
        {
            if (id != menu.MenuId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(menu);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(menu);
        }
        // DELETE MENU
        public async Task<IActionResult> Delete(int id)
        {
            var menu = await _context.ListMenus.FindAsync(id);

            if (menu != null)
            {
                _context.ListMenus.Remove(menu);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var menu = await _context.ListMenus.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            menu.IsAvailable = !(menu.IsAvailable ?? true);

            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "Order");
        }
    }
}