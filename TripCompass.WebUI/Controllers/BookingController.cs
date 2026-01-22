using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Threading.Tasks;
using TripCompass.Application.Auth;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.WebUI.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        // MB Bank account for payment QR
        private const string QrBankCode = "MB";
        private const string QrAccountNumber = "68161397979";

        public BookingController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForPost(long postId, string customerName, string customerPhone, int quantity, string arrivalTime, string? promoCode, string paymentMethod = "Cash")
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Json(new { success = false, message = "Bạn cần đăng nhập để đặt chỗ." });

            customerName = (customerName ?? "").Trim();
            customerPhone = (customerPhone ?? "").Trim();
            promoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim().ToUpperInvariant();

            if (postId <= 0) return Json(new { success = false, message = "Bài viết không hợp lệ." });
            if (string.IsNullOrWhiteSpace(customerName)) return Json(new { success = false, message = "Vui lòng nhập tên." });
            if (customerName.Length > 120) return Json(new { success = false, message = "Tên tối đa 120 ký tự." });
            if (string.IsNullOrWhiteSpace(customerPhone)) return Json(new { success = false, message = "Vui lòng nhập số điện thoại." });
            if (customerPhone.Length > 30) return Json(new { success = false, message = "Số điện thoại tối đa 30 ký tự." });
            if (quantity < 1 || quantity > 100) return Json(new { success = false, message = "Số người không hợp lệ (1-100)." });
            if (string.IsNullOrWhiteSpace(arrivalTime)) return Json(new { success = false, message = "Vui lòng chọn thời gian đến." });

            // datetime-local => "yyyy-MM-ddTHH:mm"
            if (!DateTime.TryParseExact(arrivalTime, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var arrival))
            {
                return Json(new { success = false, message = "Thời gian đến không hợp lệ." });
            }

            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null) return Json(new { success = false, message = "Không tìm thấy bài viết." });

            // Đồng bộ logic với ReviewController.Detail:
            // Cho phép đặt chỗ nếu post là đối tác HOẶC author có role Partner HOẶC bài có thông tin liên hệ.
            var authorHasPartnerRole = post.User?.UserRoles?.Any(ur => ur.Role != null && ur.Role.RoleName == "Partner") == true;
            var hasContactInfo = !string.IsNullOrWhiteSpace(post.Phone) || !string.IsNullOrWhiteSpace(post.OpeningHours);
            var supportsBooking = post.IsPartner || authorHasPartnerRole || hasContactInfo;

            if (!supportsBooking) return Json(new { success = false, message = "Bài viết này không hỗ trợ đặt chỗ." });

            var price = post.Price ?? 0m;
            var totalAmount = price * quantity;

            paymentMethod = (paymentMethod ?? "Cash").Trim();
            if (paymentMethod != "Cash" && paymentMethod != "Online")
            {
                paymentMethod = "Cash";
            }

            var booking = new PostBooking
            {
                PostId = post.PostId,
                PartnerUserId = post.UserId,
                CustomerUserId = userId,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Quantity = quantity,
                VisitDate = arrival,
                PromoCode = promoCode,
                TotalAmount = totalAmount,
                Status = "Processing",
                Note = null,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "Cash" ? "Pending" : "Pending"
            };

            _context.PostBookings.Add(booking);
            await _context.SaveChangesAsync();

            // Auto-deduct 3% commission for cash orders immediately
            // For online orders: deduct when unpaid (treat as cash), or when webhook confirms payment
            var commissionRate = 0.03m;
            var commissionAmount = Math.Round(totalAmount * commissionRate, 2, MidpointRounding.AwayFromZero);
            var commissionAmountInt = (int)Math.Round(commissionAmount, MidpointRounding.AwayFromZero);
            var now = DateTime.UtcNow;

            // Trừ phí ngay nếu: tiền mặt HOẶC online nhưng chưa thanh toán (tính như tiền mặt)
            if (paymentMethod == "Cash" || (paymentMethod == "Online" && booking.PaymentStatus != "Paid"))
            {
                booking.CommissionDeducted = true;
                booking.CommissionAmount = commissionAmount;

                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.PartnerUserId);
                if (wallet == null)
                {
                    wallet = new Wallet
                    {
                        UserId = booking.PartnerUserId,
                        Balance = 0,
                        UpdatedAt = now
                    };
                    _context.Wallets.Add(wallet);
                }

                // Trừ phí từ ví (âm)
                wallet.Balance -= commissionAmountInt;
                wallet.UpdatedAt = now;

                _context.CoinTransactions.Add(new CoinTransaction
                {
                    UserId = booking.PartnerUserId,
                    Amount = -commissionAmountInt,
                    Type = "Commission fee",
                    ReferenceId = booking.BookingId,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();

            // Payment QR using VietQR image (only for Online payment)
            string? qrImageUrl = null;
            if (paymentMethod == "Online")
            {
                var addInfo = Uri.EscapeDataString($"TripCompass BOOKING-{booking.BookingId}");
                var amountPart = totalAmount > 0 ? $"?amount={(long)decimal.Round(totalAmount, 0)}&addInfo={addInfo}" : $"?addInfo={addInfo}";
                qrImageUrl = $"https://img.vietqr.io/image/{QrBankCode}-{QrAccountNumber}-compact2.png{amountPart}";
            }

            return Json(new
            {
                success = true,
                bookingId = booking.BookingId,
                amount = totalAmount,
                qrImageUrl
            });
        }
    }
}

