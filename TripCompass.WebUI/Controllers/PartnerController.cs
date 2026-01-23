using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.ViewModels.Partner;

namespace TripCompass.WebUI.Controllers
{
    [Authorize(Roles = "Partner")]
    public class PartnerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailService _emailService;

        public PartnerController(AppDbContext context, ICurrentUserService currentUser, IEmailService emailService)
        {
            _context = context;
            _currentUser = currentUser;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var walletBalance = await _context.Wallets
                .AsNoTracking()
                .Where(w => w.UserId == userId)
                .Select(w => (int?)w.Balance)
                .FirstOrDefaultAsync() ?? 0;

            // Recent orders = bookings of this partner
            var recentOrders = await _context.PostBookings
                .AsNoTracking()
                .Where(b => b.PartnerUserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .Take(5)
                .ToListAsync();

            var postTitles = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.PostId, p => p.Title);

            var activeCodes = await _context.PartnerDiscountCodes
                .AsNoTracking()
                .Where(c => c.PartnerUserId == userId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var selectedYear = now.Year;
            if (Request.Query.ContainsKey("year") && int.TryParse(Request.Query["year"], out var year) && year >= 2000 && year <= 2100)
            {
                selectedYear = year;
            }
            ViewBag.SelectedYear = selectedYear;
            var startYear = new DateTime(selectedYear, 1, 1);
            var endYear = new DateTime(selectedYear, 12, 31, 23, 59, 59);
            var yearlyBookings = await _context.PostBookings
                .AsNoTracking()
                .Where(b => b.PartnerUserId == userId && b.BookedAt >= startYear && b.BookedAt <= endYear)
                .ToListAsync();

            var totalRevenue = yearlyBookings.Sum(b => b.TotalAmount);
            var newOrders = yearlyBookings.Count(b => b.Status == "Processing");

            var monthly = Enumerable.Range(1, 12)
                .Select(m => new MonthlyRevenuePoint
                {
                    Month = m,
                    Amount = yearlyBookings
                        .Where(b => b.BookedAt.Month == m)
                        .Sum(b => b.TotalAmount)
                })
                .ToList();

            var vm = new PartnerDashboardViewModel
            {
                ShopName = "ShopAdmin",
                TotalRevenue = totalRevenue,
                NewOrders = newOrders,
                WalletBalance = walletBalance,
                ActiveDiscountCodes = activeCodes.Count,
                MonthlyRevenue = monthly,
                RecentOrders = recentOrders.Select(o => new RecentOrderItem
                {
                    BookingId = o.BookingId,
                    PostId = o.PostId,
                    PostTitle = postTitles.TryGetValue(o.PostId, out var t) ? t : $"Post #{o.PostId}",
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    BookedAt = o.BookedAt
                }).ToList(),
                DiscountCodes = activeCodes.Select(c => new DiscountCodeItem
                {
                    Id = c.PartnerDiscountCodeId,
                    Code = c.Code,
                    PercentOff = c.PercentOff,
                    Purpose = c.Purpose,
                    IsActive = c.IsActive,
                    ExpiryDate = c.ExpiryDate,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                WalletActivities = await _context.CoinTransactions
                    .AsNoTracking()
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .Select(t => new WalletActivityItem
                    {
                        Label = t.Type,
                        At = t.CreatedAt,
                        Amount = t.Amount
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var bookings = await _context.PostBookings
                .AsNoTracking()
                .Where(b => b.PartnerUserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            var postTitles = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.PostId, p => p.Title);

            var customerEmails = await _context.Users
                .AsNoTracking()
                .Where(u => bookings.Select(b => b.CustomerUserId).Distinct().Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Email);

            var vm = new PartnerOrdersViewModel
            {
                Orders = bookings.Select(b => new PartnerOrderItem
                {
                    BookingId = b.BookingId,
                    PostTitle = postTitles.TryGetValue(b.PostId, out var t) ? t : $"Post #{b.PostId}",
                    CustomerName = b.CustomerName,
                    CustomerPhone = b.CustomerPhone,
                    CustomerEmail = customerEmails.TryGetValue(b.CustomerUserId, out var em) ? em : "",
                    TotalAmount = b.TotalAmount,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus,
                    BookedAt = b.BookedAt,
                    Note = b.Note
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(long bookingId)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var booking = await _context.PostBookings.FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PartnerUserId == userId);
            if (booking == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Orders));
            }

            booking.Status = "Completed";
            booking.Note = null;
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Đã duyệt đơn #{booking.BookingId}.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(long bookingId, string reason)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            reason = (reason ?? "").Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Message"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(Orders));
            }
            if (reason.Length > 500) reason = reason.Substring(0, 500);

            var booking = await _context.PostBookings.FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PartnerUserId == userId);
            if (booking == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Orders));
            }

            booking.Status = "Cancelled";
            booking.Note = reason;
            
            var now = DateTime.UtcNow;
            var refundMessage = "";

            // Nếu đơn đã thanh toán online → hoàn tiền tự động
            if (booking.PaymentMethod == "Online" && booking.PaymentStatus == "Paid" && booking.AmountPaid.HasValue && !booking.Refunded)
            {
                var refundAmount = booking.AmountPaid.Value;
                booking.Refunded = true;
                booking.RefundAmount = refundAmount;
                booking.RefundedAt = now;
                booking.RefundReason = $"Từ chối bởi shop: {reason}";

                // Hoàn tiền vào ví khách (tự động tạo ví nếu chưa có)
                var customerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.CustomerUserId);
                if (customerWallet == null)
                {
                    customerWallet = new Wallet
                    {
                        UserId = booking.CustomerUserId,
                        Balance = 0,
                        UpdatedAt = now
                    };
                    _context.Wallets.Add(customerWallet);
                }

                // Cộng tiền hoàn lại vào ví khách
                customerWallet.Balance += (int)Math.Round(refundAmount, MidpointRounding.AwayFromZero);
                customerWallet.UpdatedAt = now;

                _context.CoinTransactions.Add(new CoinTransaction
                {
                    UserId = booking.CustomerUserId,
                    Amount = (int)Math.Round(refundAmount, MidpointRounding.AwayFromZero),
                    Type = "Booking refund",
                    ReferenceId = booking.BookingId,
                    CreatedAt = now
                });

                // Nếu đã cộng tiền vào ví shop (từ webhook), trừ lại
                if (booking.CommissionDeducted && booking.CommissionAmount.HasValue)
                {
                    var partnerWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.PartnerUserId);
                    if (partnerWallet != null)
                    {
                        // Trừ lại số tiền đã cộng (netForPartner = TotalAmount - CommissionAmount)
                        var netForPartner = booking.TotalAmount - booking.CommissionAmount.Value;
                        partnerWallet.Balance -= (int)Math.Round(netForPartner, MidpointRounding.AwayFromZero);
                        partnerWallet.UpdatedAt = now;

                        _context.CoinTransactions.Add(new CoinTransaction
                        {
                            UserId = booking.PartnerUserId,
                            Amount = -(int)Math.Round(netForPartner, MidpointRounding.AwayFromZero),
                            Type = "Booking refund (reversed)",
                            ReferenceId = booking.BookingId,
                            CreatedAt = now
                        });
                    }
                }

                refundMessage = $" Đã hoàn tiền {refundAmount:N0}₫ vào ví của khách.";
            }

            await _context.SaveChangesAsync();

            var customerEmail = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == booking.CustomerUserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var refundNote = booking.Refunded ? $"\n\nĐã hoàn tiền {booking.RefundAmount:N0}₫ vào ví của bạn." : "";
                var subject = $"TripCompass - Đơn đặt chỗ #{booking.BookingId} đã bị từ chối";
                var body =
$@"Xin chào {booking.CustomerName},

Đơn đặt chỗ #{booking.BookingId} của bạn đã bị shop từ chối.
Lý do: {reason}{refundNote}

Trân trọng,
TripCompass";
                await _emailService.SendEmailAsync(customerEmail, subject, body);
            }

            TempData["Message"] = $"Đã từ chối đơn #{booking.BookingId} và gửi email cho khách.{refundMessage}";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDiscountCode(string code, int percentOff, string purpose, string? expiryDate)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            code = (code ?? "").Trim().ToUpperInvariant();
            purpose = (purpose ?? "").Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã (code)." });
            }
            if (code.Length > 30)
            {
                return Json(new { success = false, message = "Code tối đa 30 ký tự." });
            }
            if (percentOff < 1 || percentOff > 100)
            {
                return Json(new { success = false, message = "Phần trăm giảm giá phải từ 1 đến 100." });
            }
            if (string.IsNullOrWhiteSpace(purpose))
            {
                return Json(new { success = false, message = "Vui lòng nhập mục đích." });
            }
            if (purpose.Length > 200)
            {
                return Json(new { success = false, message = "Mục đích tối đa 200 ký tự." });
            }

            DateTime? expiry = null;
            if (!string.IsNullOrWhiteSpace(expiryDate))
            {
                // input type=date => yyyy-MM-dd
                if (DateTime.TryParseExact(expiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    // set to end of day local-ish (store as UTC date end)
                    expiry = dt.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    return Json(new { success = false, message = "Hạn sử dụng không hợp lệ." });
                }
            }

            var exists = await _context.PartnerDiscountCodes
                .AnyAsync(x => x.PartnerUserId == userId && x.Code == code);
            if (exists)
            {
                return Json(new { success = false, message = "Code đã tồn tại." });
            }

            var entity = new PartnerDiscountCode
            {
                PartnerUserId = userId,
                Code = code,
                PercentOff = percentOff,
                Purpose = purpose,
                ExpiryDate = expiry,
                CreatedAt = DateTime.UtcNow
            };
            
            // Set IsActive một cách rõ ràng để tránh bị database default override
            entity.IsActive = false; // false = Chờ admin duyệt, true = Đã duyệt và hoạt động

            _context.PartnerDiscountCodes.Add(entity);
            
            // Đảm bảo EF Core gửi giá trị IsActive vào database
            _context.Entry(entity).Property(e => e.IsActive).IsModified = true;
            
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Tạo mã giảm giá thành công! Mã đang chờ admin duyệt." });
        }

        [HttpGet]
        public async Task<IActionResult> DiscountCodes()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId <= 0) return RedirectToAction("Login", "Account");

                var codes = await _context.PartnerDiscountCodes
                    .AsNoTracking()
                    .Where(c => c.PartnerUserId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var vm = new DiscountCodesListViewModel
                {
                    Codes = codes.Select(c => new DiscountCodeItem
                    {
                        Id = c.PartnerDiscountCodeId,
                        Code = c.Code,
                        PercentOff = c.PercentOff,
                        Purpose = c.Purpose,
                        IsActive = c.IsActive,
                        ExpiryDate = c.ExpiryDate,
                        CreatedAt = c.CreatedAt
                    }).ToList()
                };

                return View("DiscountCodes", vm);
            }
            catch (Exception ex)
            {
                // Log error và trả về view với model rỗng
                var emptyVm = new DiscountCodesListViewModel
                {
                    Codes = new List<DiscountCodeItem>()
                };
                return View("DiscountCodes", emptyVm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Withdraw()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == userId);
            if (wallet == null || wallet.Balance <= 0)
            {
                TempData["Message"] = "Không có số dư khả dụng để rút.";
                return RedirectToAction(nameof(Dashboard));
            }

            var now = DateTime.UtcNow;
            var amount = wallet.Balance;

            wallet.Balance = 0;
            wallet.UpdatedAt = now;

            _context.CoinTransactions.Add(new CoinTransaction
            {
                UserId = userId,
                Amount = -amount,
                Type = "Withdraw request",
                ReferenceId = wallet.WalletId,
                CreatedAt = now
            });

            _context.SaveChanges();

            TempData["Message"] = $"Đã gửi yêu cầu rút {amount:N0}. Admin sẽ chuyển tiền thủ công.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Commission()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var bookings = await _context.PostBookings
                .AsNoTracking()
                .Where(b => b.PartnerUserId == userId && b.CommissionDeducted == true)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            var unpaidCommission = bookings
                .Where(b => !b.CommissionPaid && b.CommissionAmount.HasValue)
                .Sum(b => b.CommissionAmount.Value);

            var postTitles = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToDictionaryAsync(p => p.PostId, p => p.Title);

            var totalCommission = (int)Math.Round(bookings
                .Where(b => b.CommissionAmount.HasValue)
                .Sum(b => b.CommissionAmount.Value), MidpointRounding.AwayFromZero);

            var vm = new CommissionViewModel
            {
                Bookings = bookings.Select(b => new CommissionBookingItem
                {
                    BookingId = b.BookingId,
                    PostId = b.PostId,
                    PostTitle = postTitles.TryGetValue(b.PostId, out var t) ? t : $"Post #{b.PostId}",
                    TotalAmount = b.TotalAmount,
                    CommissionAmount = (int)Math.Round(b.CommissionAmount ?? 0, MidpointRounding.AwayFromZero),
                    PaymentMethod = b.PaymentMethod,
                    PaymentStatus = b.PaymentStatus,
                    BookedAt = b.BookedAt,
                    CommissionPaid = b.CommissionPaid,
                    CommissionPaidAt = b.CommissionPaidAt
                }).ToList(),
                TotalCommission = totalCommission,
                UnpaidCommission = (int)Math.Round(unpaidCommission, MidpointRounding.AwayFromZero)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateCommissionPaymentQr(long? bookingId = null)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Json(new { success = false, message = "Bạn cần đăng nhập." });

            var bookings = _context.PostBookings
                .Where(b => b.PartnerUserId == userId && b.CommissionDeducted == true && !b.CommissionPaid && b.CommissionAmount.HasValue)
                .ToList();

            if (bookings.Count == 0)
            {
                return Json(new { success = false, message = "Không có phí hoa hồng nào cần thanh toán." });
            }

            decimal totalAmount = 0;
            string addInfo;

            if (bookingId.HasValue)
            {
                // Thanh toán từng đơn
                var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId.Value);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng hoặc đã thanh toán." });
                }
                totalAmount = booking.CommissionAmount.Value;
                addInfo = Uri.EscapeDataString($"TripCompass COMMISSION-{booking.BookingId}");
            }
            else
            {
                // Thanh toán tất cả
                totalAmount = bookings.Sum(b => b.CommissionAmount.Value);
                addInfo = Uri.EscapeDataString($"TripCompass COMMISSION-ALL-{userId}");
            }

            // MB Bank account for payment QR
            const string QrBankCode = "MB";
            const string QrAccountNumber = "68161397979";
            var amountPart = totalAmount > 0 ? $"?amount={(long)decimal.Round(totalAmount, 0)}&addInfo={addInfo}" : $"?addInfo={addInfo}";
            var qrImageUrl = $"https://img.vietqr.io/image/{QrBankCode}-{QrAccountNumber}-compact2.png{amountPart}";

            return Json(new
            {
                success = true,
                qrImageUrl,
                amount = totalAmount,
                bookingId = bookingId,
                isAll = !bookingId.HasValue
            });
        }
    }
}

