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

            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _loginService.LoginAsync(email, password);
            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            await SignInUser(user);

            if (user.UserRoles.Any(r => r.Role.RoleName == "Admin"))
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
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
            if (!_passwordHasher.Verify(model.CurrentPassword, user.PasswordHash))
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
                return RedirectToAction(nameof(Login));

            var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal?.FindFirstValue(ClaimTypes.Name);

            if (email == null)
                return RedirectToAction(nameof(Login));

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                user = new User(
                    userName: name ?? email.Split('@')[0],
                    email: email,
                    passwordHash: "GOOGLE"
                );

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }

            await SignInUser(user);

            return LocalRedirect(returnUrl);
        }

        public static User CreateGoogleUser(string email, string? name)
        {
            return new User(
                userName: name ?? email.Split('@')[0],
                email: email,
                passwordHash: "GOOGLE"
            );
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

            // ✅ login
            await SignInUser(user);

            // ✅ BẮT BUỘC PHẢI CÓ
            return RedirectToAction("Index", "Home");
        }


    }
}
