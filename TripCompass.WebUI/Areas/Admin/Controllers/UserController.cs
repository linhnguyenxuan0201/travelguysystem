using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TripCompass.Application.Features.Admin.Users.BanUser;
using TripCompass.Application.Features.Admin.Users.GetUsers;
using TripCompass.Application.Features.Admin.Users.UnbanUser;
using TripCompass.Domain.Entities;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IMediator _mediator;
        private readonly TripCompass.Infrastructure.Persistence.AppDbContext _db;

        public UserController(IMediator mediator, TripCompass.Infrastructure.Persistence.AppDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        private long GetCurrentAdminId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task LogAdminActionAsync(string actionType, string targetTable, long targetId, string note)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0) return;

            var adminLog = new AdminLog
            {
                AdminId = adminId,
                ActionType = actionType,
                TargetTable = targetTable,
                TargetId = targetId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };
            _db.AdminLogs.Add(adminLog);
            await _db.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? role, int page = 1)
        {
            bool? isBanned = null;
            if (string.Equals(status, "Banned", StringComparison.OrdinalIgnoreCase)) isBanned = true;
            else if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)) isBanned = false;

            var query = new GetUsersQuery 
            { 
                SearchTerm = searchTerm, 
                IsBanned = isBanned,
                Role = role,
                Page = page, 
                PageSize = 10 
            };
            var (items, totalCount) = await _mediator.Send(query);

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Role = role;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 10.0);

            return View(items);
        }

        [HttpPost("ChangeRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(long userId, string role)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return RedirectToAction(nameof(Index));

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == role);
            if (targetRole == null) return RedirectToAction(nameof(Index));

            var oldRole = user.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "None";

            // Remove current roles (assume single-role assignment for UI simplicity)
            _db.UserRoles.RemoveRange(user.UserRoles);
            _db.UserRoles.Add(new TripCompass.Domain.Entities.UserRole
            {
                UserId = user.UserId,
                RoleId = targetRole.RoleId
            });

            await _db.SaveChangesAsync();

            // Log action
            await LogAdminActionAsync(
                "CHANGE_USER_ROLE",
                "Users",
                user.UserId,
                $"Changed role of user {user.UserName} from {oldRole} to {role}");

            return RedirectToAction(nameof(Index), new { role, status = Request.Query["status"], searchTerm = Request.Query["searchTerm"], page = Request.Query["page"] });
        }

        [HttpPost("ChangeStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(long userId, string status)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return RedirectToAction(nameof(Index));

            var wasBanned = user.IsBanned;
            var actionType = "";
            var note = "";

            // Hiện chỉ có cờ IsBanned. Map các trạng thái khác về Active/Banned.
            if (string.Equals(status, "Banned", StringComparison.OrdinalIgnoreCase))
            {
                if (!wasBanned)
                {
                    user.Ban();
                    actionType = "BAN_USER";
                    note = $"Banned user {user.UserName}";
                }
            }
            else
            {
                if (wasBanned)
                {
                    user.Unban();
                    actionType = "UNBAN_USER";
                    note = $"Unbanned user {user.UserName}";
                }
            }

            await _db.SaveChangesAsync();

            // Log action if status actually changed
            if (!string.IsNullOrEmpty(actionType))
            {
                await LogAdminActionAsync(actionType, "Users", user.UserId, note);
            }

            return RedirectToAction(nameof(Index), new { status, role = Request.Query["role"], searchTerm = Request.Query["searchTerm"], page = Request.Query["page"] });
        }

        [HttpPost("Ban/{id}")]
        public async Task<IActionResult> Ban(long id, string reason)
        {
            var command = new BanUserCommand { UserId = id, Reason = reason };
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Unban/{id}")]
        public async Task<IActionResult> Unban(long id)
        {
            var command = new UnbanUserCommand { UserId = id };
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }
    }
}
