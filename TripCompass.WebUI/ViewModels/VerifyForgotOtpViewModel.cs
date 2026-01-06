using System.ComponentModel.DataAnnotations;

namespace TripCompass.WebUI.ViewModels
{
    public class VerifyForgotOtpViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
