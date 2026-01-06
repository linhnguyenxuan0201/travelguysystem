using System.ComponentModel.DataAnnotations;

namespace TripCompass.WebUI.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[\W_]).+$",
            ErrorMessage = "Password must contain uppercase, lowercase and special character"
        )]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
