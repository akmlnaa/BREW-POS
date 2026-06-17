using BREW_POS.Data;
using BREW_POS.Models;
using BREW_POS.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BREW_POS.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // HALAMAN KASIR
        // =========================
        public async Task<IActionResult> Create(string search)
        {
            var menus = from m in _context.ListMenus
                        select m;

            if (!string.IsNullOrEmpty(search))
            {
                menus = menus.Where(m =>
                    m.MenuName.Contains(search));
            }

            return View(await menus.ToListAsync());
        }

        // =========================
        // ADD TO CART
        // =========================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int MenuId, int Qty)
        {
            var menu = await _context.ListMenus
                .FirstOrDefaultAsync(m => m.MenuId == MenuId);

            if (menu == null)
                return RedirectToAction("Create");

            // 🔥 AMBIL CART DARI SESSION
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            // 🔥 CEK APAKAH MENU SUDAH ADA
            var existingItem = cart
                .FirstOrDefault(c => c.MenuId == MenuId);

            if (existingItem != null)
            {
                existingItem.Qty += Qty;
            }
            else
            {
                cart.Add(new CartItem
                {
                    MenuId = menu.MenuId,
                    MenuName = menu.MenuName,
                    Price = menu.Price ?? 0,
                    Qty = Qty
                });
            }

            // 🔥 SIMPAN KE SESSION
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Create");
        }

        // =========================
        // GET CART PARTIAL
        // =========================
        public IActionResult GetCart()
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            return PartialView("_CartPartial", cart);
        }

        // =========================
        // REMOVE CART ITEM
        // =========================
        [HttpPost]
        public IActionResult RemoveCartItem(int id)
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.MenuId == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Create");
        }

        // =========================
        // INCREASE CART ITEM
        // =========================
        [HttpPost]
        public IActionResult IncreaseCartItem(int id)
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.MenuId == id);

            if (item != null)
            {
                item.Qty++;
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Create");
        }

        // =========================
        // DECREASE CART ITEM
        // =========================
        [HttpPost]
        public IActionResult DecreaseCartItem(int id)
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.MenuId == id);

            if (item != null)
            {
                item.Qty--;

                if (item.Qty <= 0)
                {
                    cart.Remove(item);
                }
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Create");
        }

        // =========================
        // UPDATE NOTE
        // =========================
        [HttpPost]
        public IActionResult UpdateCartNote(int id, string note)
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.MenuId == id);

            if (item != null)
            {
                item.Note = note;
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Create");
        }

        // =========================
        // SAVE CART AS BILL
        // =========================
        [HttpPost]
        public async Task<IActionResult> SaveCartAsBill(string billName)
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Create");

            decimal subtotal = cart.Sum(x => x.Subtotal);
            decimal tax = subtotal * 0.10m;
            decimal grandTotal = subtotal + tax;

            var bill = new Bill
            {
                BillName = string.IsNullOrWhiteSpace(billName)
                ? "Guest"
                : billName,
                BillCode = "BILL-" + DateTime.Now.Ticks,
                BillDate = DateTime.Now,
                Status = "OPEN",
                SubTotal = subtotal,
                GrandTotal = grandTotal
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            // 🔥 SAVE DETAIL
            foreach (var item in cart)
            {
                var detail = new BillDetail
                {
                    BillId = bill.BillId,
                    MenuId = item.MenuId,
                    Qty = item.Qty,
                    Price = item.Price,
                    Subtotal = item.Subtotal,
                    Note = item.Note
                };

                _context.BillDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("CurrentBill");

            TempData["Success"] = "Bill berhasil disimpan!";

            return RedirectToAction("Create");
        }

        // =========================
        // BILL LIST
        // =========================
        public async Task<IActionResult> BillList()
        {
            var bills = await _context.Bills
                .AsNoTracking()
                .Where(b => b.Status == "OPEN")
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            return PartialView("_BillListPartial", bills);
        }

        // =========================
        // OPEN BILL
        // =========================
        public async Task<IActionResult> OpenBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                .ThenInclude(d => d.Menu)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
                return RedirectToAction("BillList");

            var cart = bill.BillDetails.Select(x => new CartItem
            {
                MenuId = x.MenuId ?? 0,
                MenuName = x.Menu?.MenuName ?? "",
                Price = x.Price ?? 0,
                Qty = x.Qty ?? 0,
                Note = x.Note
            }).ToList();

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            HttpContext.Session.SetString("CurrentBill", bill.BillName);
            HttpContext.Session.SetInt32("CurrentBillId", bill.BillId);

            return RedirectToAction("Create");
        }
        // =========================
        // NEW BILL
        // =========================
        public IActionResult NewBill()
        {
            HttpContext.Session.Remove("Cart");

            HttpContext.Session.Remove("CurrentBill");
            HttpContext.Session.Remove("CurrentBillId");

            TempData["Success"] = "Bill baru siap dibuat!";

            return RedirectToAction("Create");
        }
        // =========================
        // PAYMENT MODAL
        // =========================
        public IActionResult PaymentModal()
        {
            var cart = HttpContext.Session
                .GetObjectFromJson<List<CartItem>>("Cart")
                ?? new List<CartItem>();

            ViewBag.BillId =
                HttpContext.Session.GetInt32("CurrentBillId") ?? 0;

            return PartialView("_PaymentModalPartial", cart);
        }

        // =========================
        // DELETE BILL
        // =========================
        [HttpPost]
        public async Task<IActionResult> DeleteBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
                return RedirectToAction("BillList");

            _context.BillDetails.RemoveRange(bill.BillDetails);

            _context.Bills.Remove(bill);

            await _context.SaveChangesAsync();

            return RedirectToAction("Create");
        }

        // =========================
        // PAY BILL
        // =========================
        public async Task<IActionResult> PayBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                .ThenInclude(d => d.Menu)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
                return RedirectToAction("BillList");

            return View(bill);
        }

        // =========================
        // PROCESS PAYMENT
        // =========================
        [HttpPost]
        public async Task<JsonResult> ProcessBillPayment(
    int BillId,
    decimal Cash,
    string PaymentMethod)
        {
            // 🔥 JIKA BELUM ADA BILL
            if (BillId == 0)
            {
                var cart = HttpContext.Session
                    .GetObjectFromJson<List<CartItem>>("Cart")
                    ?? new List<CartItem>();

                if (!cart.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cart kosong!"
                    });
                }

                decimal subtotal = cart.Sum(x => x.Subtotal);
                decimal tax = subtotal * 0.10m;
                decimal grandTotal = subtotal + tax;

                var newBill = new Bill
                {
                    BillName = "Direct Payment",
                    BillCode = "BILL-" + DateTime.Now.Ticks,
                    BillDate = DateTime.Now,
                    Status = "OPEN",
                    SubTotal = subtotal,
                    GrandTotal = grandTotal
                };

                _context.Bills.Add(newBill);

                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    _context.BillDetails.Add(new BillDetail
                    {
                        BillId = newBill.BillId,
                        MenuId = item.MenuId,
                        Qty = item.Qty,
                        Price = item.Price,
                        Subtotal = item.Subtotal,
                        Note = item.Note
                    });
                }

                await _context.SaveChangesAsync();

                BillId = newBill.BillId;
            }

            // 🔥 AMBIL BILL
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BillId == BillId);

            if (bill == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Bill tidak ditemukan!"
                });
            }

            decimal total = bill.GrandTotal ?? 0;

            decimal change = Cash - total;

            // 🔥 VALIDASI CASH
            if (PaymentMethod == "Cash" && change < 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Uang tidak cukup!"
                });
            }

            // 🔥 NON CASH AUTO PAS
            if (PaymentMethod != "Cash")
            {
                Cash = total;
                change = 0;
            }

            // 🔥 UPDATE STATUS BILL
            bill.Status = "PAID";

            _context.Entry(bill).State =
                EntityState.Modified;

            await _context.SaveChangesAsync();

            // 🔥 SAVE PAYMENT
            var payment = new Payment
            {
                BillId = bill.BillId,
                PaymentDate = DateTime.Now,
                PaymentMethod = PaymentMethod,
                PaidAmount = Cash,
                ChangeAmount = change,
                PaymentStatus = "SUCCESS"
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            // 🔥 UPDATE CURRENT SHIFT SALES
            var currentShift = await _context.Shifts
                .FirstOrDefaultAsync(s => !s.IsClosed);

            if (currentShift != null)
            {
                currentShift.TotalSales += total;

                await _context.SaveChangesAsync();
            }

            // 🔥 CLEAR SESSION
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("CurrentBill");
            HttpContext.Session.Remove("CurrentBillId");

            // 🔥 RETURN JSON
            return Json(new
            {
                success = true,
                billId = bill.BillId,
                paymentMethod = PaymentMethod,
                total = total,
                paid = Cash,
                change = change
            });
        }

        // =========================
        // RECEIPT
        // =========================
        public async Task<IActionResult> ReceiptBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                    .ThenInclude(d => d.Menu)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
            {
                return RedirectToAction("Create");
            }

            return View(bill);
        }
        public async Task<IActionResult> History()
        {
            var bills = await _context.Bills
                .Include(b => b.Payments)
                .Where(b => b.Status == "PAID")
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            return View(bills);
        }

        [HttpPost]
        public async Task<IActionResult> RenameBill(int billId, string billName)
        {
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BillId == billId);

            if (bill == null)
            {
                return RedirectToAction("Create");
            }

            bill.BillName = billName;

            await _context.SaveChangesAsync();

            return RedirectToAction("Create");
        }
    }
}