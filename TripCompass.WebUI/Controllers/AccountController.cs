using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TripCompass.Application.Auth;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.ViewModels;


namespace TripCompass.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly LoginService _loginService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        public AccountController(
            LoginService loginService,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            AppDbContext db,
            IEmailService emailService)
        {
            _loginService = loginService;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _db = db;
            _emailService = emailService;
        }

        /* =========================
           LOGIN
        ========================= */

        [HttpGet, AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            // Mặc định không hiển thị lỗi
            ViewBag.Error = null;

            // Hiển thị thông báo lỗi (nếu có) từ lần đăng nhập trước
            if (TempData["Error"] is string errorMessage)
            {
                ViewBag.Error = errorMessage;
            }

            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _loginService.LoginAsync(email, password);
            if (user == null)
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng, hoặc tài khoản cần đặt lại mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            // Reload user with UserRoles to ensure they're loaded
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (user == null)
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng.";
                return RedirectToAction(nameof(Login));
            }

            await SignInUser(user);

            // Admin redirect to Portal - check UserRoles
            // Query directly from database to ensure we get the most up-to-date role information
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                var hasAdminRole = await _db.UserRoles
                    .AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == adminRole.RoleId);
                
                if (hasAdminRole)
                {
                    return RedirectToAction("Index", "Portal", new { area = "Admin" });
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /* =========================
           FIX ADMIN ROLE (Helper endpoint for debugging)
           Access: /Account/FixAdminRole?email=admin@tripcompass.com
           Or: /Account/FixAdminRole?username=admin
        ========================= */
        
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> FixAdminRole(string? email = null, string? username = "admin")
        {
            User? user = null;

            // Try to find user by email first, then by username
            if (!string.IsNullOrEmpty(email))
            {
                user = await _userRepository.GetByEmailAsync(email);
            }

            if (user == null && !string.IsNullOrEmpty(username))
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
            }

            if (user == null)
            {
                // Try to find any user with "admin" in username or email
                var adminUsers = await _db.Users
                    .Where(u => u.UserName.Contains("admin") || u.Email.Contains("admin"))
                    .Select(u => new { u.UserId, u.UserName, u.Email })
                    .ToListAsync();

                if (adminUsers.Any())
                {
                    return Json(new { 
                        success = false, 
                        message = $"User not found. Found admin-like users:", 
                        users = adminUsers 
                    });
                }

                // Create admin user if not found
                var adminPasswordHash = _passwordHasher.Hash("Admin123!");
                
                user = new User("admin", "admin@tripcompass.com", adminPasswordHash)
                {
                    CreatedAt = DateTime.UtcNow
                };
                
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Create wallet for admin
                _db.Wallets.Add(new Wallet
                {
                    UserId = user.UserId,
                    Balance = 1000,
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            // Get or create Admin role
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                // Create Admin role if not exists
                adminRole = new Role { RoleName = "Admin" };
                _db.Roles.Add(adminRole);
                await _db.SaveChangesAsync();
            }

            // Check if password hash needs to be reset (invalid format / placeholders from SQL seed)
            var needsPasswordReset = string.IsNullOrEmpty(user.PasswordHash) ||
                                    user.PasswordHash == "HASH_ADMIN" ||
                                    user.PasswordHash == "HASH_MOD" ||
                                    user.PasswordHash == "HASH_USER1" ||
                                    user.PasswordHash == "HASH_USER2" ||
                                    user.PasswordHash == "GOOGLE";

            if (needsPasswordReset)
            {
                // Reset password hash to valid format
                user.ChangePassword(_passwordHasher.Hash("Admin123!"));
                await _db.SaveChangesAsync();
            }

            var hasAdminRole = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == adminRole.RoleId);

            if (!hasAdminRole)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = adminRole.RoleId
                });
                await _db.SaveChangesAsync();
                return Json(new { 
                    success = true, 
                    message = $"Admin role assigned to user: {user.UserName} ({user.Email}). Password reset to: Admin123!. Please login with email: {user.Email}, password: Admin123!" 
                });
            }

            var passwordMessage = needsPasswordReset 
                ? " Password has been reset to: Admin123!" 
                : " If password doesn't work, use: Admin123!";

            return Json(new { 
                success = true, 
                message = $"User {user.UserName} ({user.Email}) already has Admin role.{passwordMessage}" 
            });
        }

        /* =========================
           LOGOUT
        ========================= */

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Sign out from TripCompassCookie (this will clear all authentication cookies)
            await HttpContext.SignOutAsync("TripCompassCookie");
            
            return RedirectToAction("Index", "Home");
        }

        /* =========================
           PROFILE
        ========================= */

        [Authorize, HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var wallet = await _db.Wallets
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            var avatar = await _db.UserAvatars
                .FirstOrDefaultAsync(x => x.UserId == user.UserId && x.IsActive);

            // 👉 GET CURRENT PLAN
            var currentPlan = await _db.UserPlans
                .Where(x => x.UserId == user.UserId && x.ExpiredAt == null)
                .Select(x => x.PlanCode)
                .FirstOrDefaultAsync() ?? "Free";

            // 👉 PLAN FEATURES
            List<string> features = currentPlan switch
            {
                "Pro" => new()
                {
                    "Unlimited access",
                    "Priority support",
                    "Advanced features"
                },
                "Enterprise" => new()
                {
                    "Unlimited access",
                    "Dedicated support",
                    "Enterprise features"
                },
                _ => new()
                {
                    "Standard access",
                    "Community support",
                    "Basic features"
                }
            };

            // 👉 NEXT PLAN
            string? nextPlan = currentPlan switch
            {
                "Free" => "Pro",
                "Pro" => "Enterprise",
                _ => null
            };

            var vm = new ProfileViewModel
            {
                // LEFT
                UserName = user.UserName,
                Email = user.Email,
                AvatarUrl = avatar?.AvatarUrl ?? "/images/avatar-default.png",
                JoinedAt = user.CreatedAt,
                ReputationLevel = user.ReputationLevel,
                ReputationScore = user.ReputationScore,
                WalletBalance = wallet?.Balance ?? 0,

                // RIGHT
                CurrentPlan = currentPlan,
                CurrentPlanFeatures = features,
                NextPlan = nextPlan,
                UpgradeBonus = nextPlan == null ? null : "+50%"
            };

            return View(vm);
        }

        /* =========================
           UPLOAD AVATAR
        ========================= */

        [Authorize, HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return RedirectToAction(nameof(Profile));

            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userRepository.GetByEmailAsync(email!);
            if (user == null) return RedirectToAction("Login");

            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/avatars");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"user_{user.UserId}_{DateTime.UtcNow.Ticks}{Path.GetExtension(avatar.FileName)}";
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            // deactivate old avatars
            var oldAvatars = await _db.UserAvatars
                .Where(x => x.UserId == user.UserId && x.IsActive)
                .ToListAsync();

            foreach (var a in oldAvatars)
                a.IsActive = false;

            _db.UserAvatars.Add(new UserAvatar
            {
                UserId = user.UserId,
                AvatarUrl = "/uploads/avatars/" + fileName,
                IsActive = true
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Profile));
        }

        /* =========================
           HELPER
        ========================= */

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var r in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, r.Role.RoleName));

            var identity = new ClaimsIdentity(claims, "TripCompassCookie");

            await HttpContext.SignInAsync(
                "TripCompassCookie",
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }
        /* =========================
   CHANGE PASSWORD
========================= */

        [Authorize, HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            // ❗ Check current password
            if (!_passwordHasher.Verify(user.PasswordHash, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // ✅ Update password
            var newHash = _passwordHasher.Hash(model.NewPassword);
            user.ChangePassword(newHash);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }
        [HttpGet, AllowAnonymous]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action(
                nameof(GoogleResponse),
                "Account",
                new { returnUrl });

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, "Google");
        }
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync("Google");

            if (!result.Succeeded)
            {
                TempData["Error"] = "Đăng nhập Google thất bại. Vui lòng thử lại hoặc dùng email/mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal?.FindFirstValue(ClaimTypes.Name);

            if (email == null)
            {
                TempData["Error"] = "Không lấy được email từ Google. Vui lòng thử lại hoặc dùng email/mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user already exists and is Admin - Admin cannot login with Google
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                // Reload with roles
                existingUser = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == existingUser.UserId);

                if (existingUser != null && existingUser.UserRoles.Any(r => r.Role.RoleName == "Admin"))
                {
                    TempData["Error"] = "Tài khoản Admin không thể đăng nhập bằng Google. Vui lòng sử dụng email và mật khẩu.";
                    return RedirectToAction(nameof(Login));
                }
            }

            // Use LoginService to handle Google login/signup
            var user = await _loginService.LoginWithGoogleAsync(email, name ?? email.Split('@')[0]);

            // Reload user with UserRoles to ensure they're loaded
            user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                TempData["Error"] = "Không xác thực được với Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user is banned
            if (user.IsBanned)
            {
                TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return RedirectToAction(nameof(Login));
            }

            await SignInUser(user);

            // Redirect based on role (Admin should not reach here via Google, but just in case)
            if (user.UserRoles.Any(r => r.Role.RoleName == "Admin"))
                return RedirectToAction("Index", "Portal", new { area = "Admin" });

            return LocalRedirect(returnUrl);
        }
        /* =========================
   REGISTER
========================= */

        [HttpGet, AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _userRepository.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            var otp = new Random().Next(100000, 999999).ToString();

            _db.EmailOtps.Add(new EmailOtp
            {
                Email = model.Email,
                OtpCode = otp,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });

            await _db.SaveChangesAsync();
            await _emailService.SendOtpAsync(model.Email, otp);

            TempData["RegisterEmail"] = model.Email;
            TempData["RegisterName"] = model.FullName;
            TempData["RegisterPassword"] = model.Password;

            return RedirectToAction(nameof(VerifyOtp));
        }
        [HttpGet, AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            var model = new VerifyOtpViewModel
            {
                Email = TempData["RegisterEmail"] as string ?? ""
            };

            TempData.Keep("RegisterEmail");
            TempData.Keep("RegisterName");
            TempData.Keep("RegisterPassword");

            return View(model);
        }
        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email;
            var otp = model.OtpCode;

            var name = TempData["RegisterName"] as string;
            var password = TempData["RegisterPassword"] as string;

            if (email == null || name == null || password == null)
                return RedirectToAction("Register");

            var emailOtp = await _db.EmailOtps.FirstOrDefaultAsync(x =>
                x.Email == email &&
                x.OtpCode == otp &&
                !x.IsUsed &&
                x.ExpiredAt > DateTime.UtcNow);

            if (emailOtp == null)
            {
                ModelState.AddModelError("OtpCode", "OTP invalid or expired.");
                return View(model);
            }

            // ✅ mark OTP used
            emailOtp.IsUsed = true;
            await _db.SaveChangesAsync();

            // ✅ create user
            var passwordHash = _passwordHasher.Hash(password);
            var user = new User(name, email, passwordHash);

            await _userRepository.AddAsync(user);
            await _userRepository.AssignRoleAsync(user, "User");

            // ✅ Reload user with UserRoles to ensure they're loaded
            user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Failed to create user account.");
                return View(model);
            }

            // ✅ login
            await SignInUser(user);

            // ✅ BẮT BUỘC PHẢI CÓ
            return RedirectToAction("Index", "Home");
        }


    }
}
