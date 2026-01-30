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
        public async Task<IActionResult> Login(int? banned = null)
        {
            if (banned == 1)
            {
                TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                // Nếu user đã đăng nhập nhưng bị ban (cookie còn sống) -> đá ra ngay và báo lỗi
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (long.TryParse(userIdStr, out var userId))
                {
                    var dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
                    if (dbUser == null || dbUser.IsBanned)
                    {
                        await HttpContext.SignOutAsync("TripCompassCookie");
                        TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                        return RedirectToAction(nameof(Login));
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            // Mặc định không hiển thị lỗi
            ViewBag.Error = null;

            // Hiển thị thông báo lỗi (nếu có) từ lần đăng nhập trước
            if (TempData["Error"] is string errorMessage)
            {
                ViewBag.Error = errorMessage;
            }

            // Hiển thị thông báo thành công và thông tin admin (nếu có)
            if (TempData["Success"] is string successMessage)
            {
                ViewBag.Success = successMessage;
                ViewBag.AdminEmail = TempData["AdminEmail"] as string;
                ViewBag.AdminPassword = TempData["AdminPassword"] as string;
            }

            return View();
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> Banned(int? b = null)
        {
            if (b == 1)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
            }

            // Nếu vẫn còn cookie đăng nhập, sign-out để tránh tiếp tục sử dụng
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync("TripCompassCookie");
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

            // Check if admin from config (PasswordHash = "CONFIG_ADMIN", không lưu trong database)
            if (user.PasswordHash == "CONFIG_ADMIN")
            {
                // Admin từ config - sign in trực tiếp, không cần reload từ database
                await SignInUser(user, isConfigAdmin: true);
                return RedirectToAction("Index", "Portal", new { area = "Admin" });
            }

            // Reload user with UserRoles to ensure they're loaded (chỉ cho users từ database)
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
           DEBUG: Test Password Verification
           Access: /Account/TestPassword?email=admin@tripcompass.com&password=Admin@123
        ========================= */
        
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> TestPassword(string email = "admin@tripcompass.com", string password = "Admin@123")
        {
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                return Json(new { 
                    success = false, 
                    message = $"User not found: {email}",
                    email = email
                });
            }

            var result = new
            {
                success = false,
                email = user.Email,
                username = user.UserName,
                userId = user.UserId,
                passwordHash = user.PasswordHash != null ? user.PasswordHash.Substring(0, Math.Min(50, user.PasswordHash.Length)) + "..." : "NULL",
                hashLength = user.PasswordHash?.Length ?? 0,
                hashFormat = user.PasswordHash?.StartsWith("AQAAAA") == true ? "VALID" : "INVALID",
                isPlaceholder = user.PasswordHash == "HASH_ADMIN" || 
                               user.PasswordHash == "HASH_ADMIN_PLACEHOLDER" ||
                               user.PasswordHash == "HASH_MOD" ||
                               user.PasswordHash == "HASH_USER1" ||
                               user.PasswordHash == "HASH_USER2" ||
                               user.PasswordHash == "GOOGLE",
                verificationResult = "NOT_TESTED"
            };

            // Test password verification
            if (!string.IsNullOrEmpty(user.PasswordHash) && 
                user.PasswordHash != "HASH_ADMIN" && 
                user.PasswordHash != "HASH_ADMIN_PLACEHOLDER" &&
                user.PasswordHash != "HASH_MOD" &&
                user.PasswordHash != "HASH_USER1" &&
                user.PasswordHash != "HASH_USER2" &&
                user.PasswordHash != "GOOGLE")
            {
                try
                {
                    var isValid = _passwordHasher.Verify(user.PasswordHash, password);
                    return Json(new
                    {
                        success = isValid,
                        message = isValid 
                            ? $"✓ Password verification SUCCESS! You can login with email: {email}, password: {password}"
                            : $"✗ Password verification FAILED! Hash does not match password '{password}'. You need to update the password hash.",
                        email = user.Email,
                        username = user.UserName,
                        userId = user.UserId,
                        passwordHash = user.PasswordHash != null ? user.PasswordHash.Substring(0, Math.Min(50, user.PasswordHash.Length)) + "..." : "NULL",
                        hashLength = user.PasswordHash?.Length ?? 0,
                        hashFormat = user.PasswordHash?.StartsWith("AQAAAA") == true ? "VALID" : "INVALID",
                        verificationResult = isValid ? "SUCCESS" : "FAILED",
                        testPassword = password
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"✗ Password verification ERROR: {ex.Message}. Hash format may be corrupted.",
                        email = user.Email,
                        username = user.UserName,
                        userId = user.UserId,
                        passwordHash = user.PasswordHash != null ? user.PasswordHash.Substring(0, Math.Min(50, user.PasswordHash.Length)) + "..." : "NULL",
                        hashLength = user.PasswordHash?.Length ?? 0,
                        hashFormat = "ERROR",
                        verificationResult = "ERROR",
                        error = ex.Message,
                        testPassword = password
                    });
                }
            }

            return Json(new
            {
                success = false,
                message = $"✗ Password hash is placeholder or invalid. You need to update it using /Setup/GenerateHash?password={password}",
                email = user.Email,
                username = user.UserName,
                userId = user.UserId,
                passwordHash = user.PasswordHash ?? "NULL",
                hashLength = 0,
                hashFormat = "INVALID",
                isPlaceholder = true,
                verificationResult = "NOT_TESTED",
                testPassword = password,
                fixUrl = $"/Setup/GenerateHash?password={password}"
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

        private async Task SignInUser(User user, bool isConfigAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Admin từ config - thêm role Admin trực tiếp
            if (isConfigAdmin || user.PasswordHash == "CONFIG_ADMIN")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else
            {
                // Users từ database - lấy roles từ UserRoles
                if (user.UserRoles != null)
                {
                    foreach (var r in user.UserRoles)
                        claims.Add(new Claim(ClaimTypes.Role, r.Role.RoleName));
                }
            }

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

            // ✅ KIỂM TRA 1: Nếu user đã tồn tại và là Admin - KHÔNG cho phép đăng nhập bằng Google
            // Admin phải đăng nhập bằng email/password để đảm bảo bảo mật
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                // Reload với roles để kiểm tra
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
            user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
            {
                TempData["Error"] = "Không xác thực được với Google. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }

            // ✅ DOUBLE CHECK: Admin cannot login with Google (safety check after user creation/retrieval)
            if (user.UserRoles.Any(r => r.Role.RoleName == "Admin"))
            {
                TempData["Error"] = "Tài khoản Admin không thể đăng nhập bằng Google. Vui lòng sử dụng email và mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            // Check if user is banned
            if (user.IsBanned)
            {
                // Cho đăng nhập nhưng sẽ hiển thị banner "bị khóa" trong layout
                await SignInUser(user);
                return LocalRedirect(returnUrl);
            }

            await SignInUser(user);
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

        [Authorize, HttpGet]
        public async Task<IActionResult> Wallet()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var wallet = await _db.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == user.UserId);

            var balance = wallet?.Balance ?? 0;

            var transactions = await _db.CoinTransactions
                .AsNoTracking()
                .Where(t => t.UserId == user.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();

            var vm = new WalletViewModel
            {
                Balance = balance,
                Transactions = transactions.Select(t => new WalletTransactionItem
                {
                    Type = t.Type,
                    Amount = t.Amount,
                    CreatedAt = t.CreatedAt,
                    ReferenceId = t.ReferenceId
                }).ToList()
            };

            return View(vm);
        }

        [Authorize, HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == user.UserId);
            if (wallet == null || wallet.Balance <= 0)
            {
                TempData["Message"] = "Không có số dư khả dụng để rút.";
                return RedirectToAction(nameof(Wallet));
            }

            var now = DateTime.UtcNow;
            var amount = wallet.Balance;

            wallet.Balance = 0;
            wallet.UpdatedAt = now;

            _db.CoinTransactions.Add(new CoinTransaction
            {
                UserId = user.UserId,
                Amount = -amount,
                Type = "Withdraw request",
                ReferenceId = wallet.WalletId,
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            TempData["Message"] = $"Đã gửi yêu cầu rút {amount:N0}₫. Admin sẽ chuyển tiền thủ công.";
            return RedirectToAction(nameof(Wallet));
        }

        /* =========================
           NOTIFICATIONS
        ========================= */

        [HttpGet]
        public async Task<IActionResult> Notifications(int page = 1)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (email == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            const int pageSize = 20;
            var notificationRepo = HttpContext.RequestServices.GetRequiredService<TripCompass.Application.Interfaces.Repositories.INotificationRepository>();
            var notifications = await notificationRepo.GetByUserIdAsync(user.UserId, page, pageSize);
            var unreadCount = await notificationRepo.GetUnreadCountAsync(user.UserId);

            ViewBag.UnreadCount = unreadCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(notifications);
        }

        /* =========================
           FORGOT PASSWORD
        ========================= */

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost, AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not for security
                TempData["Message"] = "Nếu email tồn tại, mã OTP đã được gửi.";
                return RedirectToAction(nameof(VerifyForgotOtp));
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Save OTP to database
            _db.EmailOtps.Add(new EmailOtp
            {
                Email = model.Email,
                OtpCode = otp,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });

            await _db.SaveChangesAsync();
            await _emailService.SendOtpAsync(model.Email, otp);

            TempData["ForgotPasswordEmail"] = model.Email;
            TempData["Message"] = "Mã OTP đã được gửi đến email của bạn.";

            return RedirectToAction(nameof(VerifyForgotOtp));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult VerifyForgotOtp()
        {
            var email = TempData["ForgotPasswordEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new VerifyForgotOtpViewModel
            {
                Email = email
            };

            TempData.Keep("ForgotPasswordEmail");

            return View(model);
        }

        [HttpPost, AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyForgotOtp(VerifyForgotOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email;
            var otp = model.OtpCode;

            // Verify OTP
            var emailOtp = await _db.EmailOtps.FirstOrDefaultAsync(x =>
                x.Email == email &&
                x.OtpCode == otp &&
                !x.IsUsed &&
                x.ExpiredAt > DateTime.UtcNow);

            if (emailOtp == null)
            {
                ModelState.AddModelError("OtpCode", "Mã OTP không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            // Mark OTP as used
            emailOtp.IsUsed = true;
            await _db.SaveChangesAsync();

            TempData["ResetPasswordEmail"] = email;
            return RedirectToAction(nameof(ResetPassword));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetPasswordEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Email = email
            };

            TempData.Keep("ResetPasswordEmail");

            return View(model);
        }

        [HttpPost, AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userRepository.GetByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Update password
            var newHash = _passwordHasher.Hash(model.NewPassword);
            user.ChangePassword(newHash);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }

        /* =========================
           UPGRADE PLAN
        ========================= */

        [Authorize, HttpPost]
        public async Task<IActionResult> UpgradePlan(string planCode, string planType = "monthly")
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Validate plan code
            if (planCode != "Pro" && planCode != "Enterprise")
            {
                TempData["Error"] = "Gói không hợp lệ";
                return RedirectToAction("Premium", "Home");
            }

            // Check if user already has this plan or higher
            var currentPlan = await _db.UserPlans
                .Where(x => x.UserId == user.UserId && (x.ExpiredAt == null || x.ExpiredAt > DateTime.UtcNow))
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync();

            var currentPlanCode = currentPlan?.PlanCode ?? "Free";
            
            // Check upgrade path
            if (currentPlanCode == "Enterprise" || (currentPlanCode == "Pro" && planCode == "Pro"))
            {
                TempData["Info"] = "Bạn đã có gói này hoặc gói cao hơn";
                return RedirectToAction("Premium", "Home");
            }

            // Calculate amount and expiration date
            decimal amount = 0;
            DateTime? expiredAt = null;
            if (planType == "monthly")
            {
                amount = 299000; // 299,000 VND
                expiredAt = DateTime.UtcNow.AddMonths(1);
            }
            else if (planType == "yearly")
            {
                amount = 2390000; // 2,390,000 VND
                expiredAt = DateTime.UtcNow.AddYears(1);
            }

            // Create premium order
            var order = new PremiumOrder
            {
                UserId = user.UserId,
                PlanCode = planCode,
                PlanType = planType,
                Amount = amount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiredAt
            };

            _db.PremiumOrders.Add(order);
            await _db.SaveChangesAsync();

            // Redirect to payment page
            return RedirectToAction("PremiumPayment", "Account", new { orderId = order.OrderId });
        }

        [Authorize]
        public async Task<IActionResult> PremiumPayment(long orderId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var order = await _db.PremiumOrders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại";
                return RedirectToAction("Premium", "Home");
            }

            if (order.Status == "Paid")
            {
                TempData["Info"] = "Đơn hàng đã được thanh toán";
                return RedirectToAction("Premium", "Home");
            }

            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> CheckPremiumPayment(long orderId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { paid = false });
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Json(new { paid = false });
            }

            var order = await _db.PremiumOrders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == user.UserId);

            if (order == null)
            {
                return Json(new { paid = false });
            }

            return Json(new { paid = order.Status == "Paid" });
        }

    }
}
