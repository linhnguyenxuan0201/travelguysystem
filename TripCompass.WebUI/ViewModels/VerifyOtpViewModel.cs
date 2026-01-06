using System.ComponentModel.DataAnnotations;

namespace TripCompass.WebUI.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string OtpCode { get; set; } = null!;
    }
}
