using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TripCompass.Infrastructure.Persistence;
using TripCompass.Domain.Entities;

namespace TripCompass.WebUI.Controllers
{
    // Webhook giả lập để nhận giao dịch ngân hàng, match BOOKING-{id} trong nội dung và tự đánh dấu đã thanh toán
    [AllowAnonymous]
    [ApiController]
    [Route("payment/webhook")]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Shared secret cho HMAC (tối giản; bạn có thể đổi và cấu hình ở appsettings)
        private const string WebhookSecret = "CHANGE_ME_WEBHOOK_SECRET";

        public PaymentWebhookController(AppDbContext context)
        {
            _context = context;
        }

        public class PaymentEvent
        {
            public string? TransactionId { get; set; }
            public string? Description { get; set; } // Nội dung chuyển khoản (ví dụ: "TripCompass BOOKING-123")
            public decimal Amount { get; set; }
            public DateTime TransactionTime { get; set; }
            public string? Signature { get; set; } // HMAC-SHA256 của (TransactionId + Amount + Description)
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] PaymentEvent payload)
        {
            if (payload == null) return BadRequest("No payload");

            // Validate HMAC (tối giản)
            var raw = $"{payload.TransactionId}|{payload.Amount}|{payload.Description}";
            var expected = ComputeHmac(raw, WebhookSecret);
            if (!string.Equals(expected, payload.Signature, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("Invalid signature");
            }

            // Kiểm tra xem có phải thanh toán hoa hồng không
            if (payload.Description != null && payload.Description.Contains("COMMISSION", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleCommissionPayment(payload);
            }

            // Tìm BOOKING-{id} trong Description
            var bookingId = ExtractBookingId(payload.Description);
            if (bookingId == null)
            {
                return Ok(new { matched = false, reason = "No booking id" });
            }

            var booking = await _context.PostBookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId.Value);

            if (booking == null)
            {
                return Ok(new { matched = false, reason = "Booking not found" });
            }

            // Nếu đã ghi nhận thanh toán trước đó, không cộng tiền ví lần nữa
            if (string.Equals(booking.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    matched = true,
                    bookingId = booking.BookingId,
                    paymentStatus = booking.PaymentStatus,
                    note = "Payment already recorded"
                });
            }

            // Cập nhật thanh toán nếu chưa paid
            booking.PaymentStatus = "Paid";
            booking.PaidAt = payload.TransactionTime;
            booking.AmountPaid = payload.Amount;
            booking.PaymentRef = payload.TransactionId ?? payload.Description;

            // Ghi nhận doanh thu cho đối tác (trừ 3% phí nếu chưa trừ)
            var commissionRate = 0.03m;
            var commissionAmount = Math.Round(payload.Amount * commissionRate, 2, MidpointRounding.AwayFromZero);
            var commissionAmountInt = (int)Math.Round(commissionAmount, MidpointRounding.AwayFromZero);
            var netForPartner = (int)Math.Round(payload.Amount * (1m - commissionRate), MidpointRounding.AwayFromZero);
            netForPartner = Math.Max(netForPartner, 0);
            var now = DateTime.UtcNow;

            // Nếu chưa trừ phí (đơn online đã thanh toán), trừ phí ngay
            if (!booking.CommissionDeducted)
            {
                booking.CommissionDeducted = true;
                booking.CommissionAmount = commissionAmount;
            }

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

            // Cộng doanh thu (đã trừ phí) vào ví
            wallet.Balance += netForPartner;
            wallet.UpdatedAt = now;

            _context.CoinTransactions.Add(new CoinTransaction
            {
                UserId = booking.PartnerUserId,
                Amount = netForPartner,
                Type = "Booking income",
                ReferenceId = booking.BookingId,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                matched = true,
                bookingId = booking.BookingId,
                paymentStatus = booking.PaymentStatus
            });
        }

        private static long? ExtractBookingId(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return null;
            // Tìm chuỗi "BOOKING-{number}"
            var marker = "BOOKING-";
            var idx = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var start = idx + marker.Length;
            var sb = new StringBuilder();
            for (int i = start; i < description.Length; i++)
            {
                var ch = description[i];
                if (char.IsDigit(ch)) sb.Append(ch);
                else break;
            }
            if (sb.Length == 0) return null;
            if (long.TryParse(sb.ToString(), out var id)) return id;
            return null;
        }

        private async Task<IActionResult> HandleCommissionPayment(PaymentEvent payload)
        {
            var description = payload.Description ?? "";
            var now = DateTime.UtcNow;

            // COMMISSION-{bookingId} hoặc COMMISSION-ALL-{userId}
            if (description.Contains("COMMISSION-ALL-", StringComparison.OrdinalIgnoreCase))
            {
                // Thanh toán tất cả
                var userIdMarker = "COMMISSION-ALL-";
                var idx = description.IndexOf(userIdMarker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + userIdMarker.Length;
                    var sb = new StringBuilder();
                    for (int i = start; i < description.Length; i++)
                    {
                        var ch = description[i];
                        if (char.IsDigit(ch)) sb.Append(ch);
                        else break;
                    }
                    if (long.TryParse(sb.ToString(), out var userId))
                    {
                        var unpaidBookings = await _context.PostBookings
                            .Where(b => b.PartnerUserId == userId && b.CommissionDeducted == true && !b.CommissionPaid && b.CommissionAmount.HasValue)
                            .ToListAsync();

                        var totalCommission = unpaidBookings.Sum(b => b.CommissionAmount.Value);
                        if (Math.Abs((decimal)payload.Amount - totalCommission) < 1m) // Cho phép sai số 1đ
                        {
                            foreach (var b in unpaidBookings)
                            {
                                b.CommissionPaid = true;
                                b.CommissionPaidAt = now;
                                b.CommissionPaymentRef = payload.TransactionId ?? payload.Description;
                            }
                            await _context.SaveChangesAsync();
                            return Ok(new { matched = true, type = "commission_all", userId, count = unpaidBookings.Count });
                        }
                    }
                }
            }
            else if (description.Contains("COMMISSION-", StringComparison.OrdinalIgnoreCase))
            {
                // Thanh toán từng đơn
                var marker = "COMMISSION-";
                var idx = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + marker.Length;
                    var sb = new StringBuilder();
                    for (int i = start; i < description.Length; i++)
                    {
                        var ch = description[i];
                        if (char.IsDigit(ch)) sb.Append(ch);
                        else break;
                    }
                    if (long.TryParse(sb.ToString(), out var bid))
                    {
                        var booking = await _context.PostBookings
                            .FirstOrDefaultAsync(b => b.BookingId == bid && !b.CommissionPaid && b.CommissionAmount.HasValue);

                        if (booking != null && Math.Abs((decimal)payload.Amount - booking.CommissionAmount.Value) < 1m)
                        {
                            booking.CommissionPaid = true;
                            booking.CommissionPaidAt = now;
                            booking.CommissionPaymentRef = payload.TransactionId ?? payload.Description;
                            await _context.SaveChangesAsync();
                            return Ok(new { matched = true, type = "commission", bookingId = bid });
                        }
                    }
                }
            }

            return Ok(new { matched = false, reason = "Commission payment not matched" });
        }

        private static string ComputeHmac(string raw, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}

