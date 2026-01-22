namespace TripCompass.WebUI.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Password must contain upper, lower, number and special character")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public class CustomPasswordAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var password = value?.ToString() ?? "";

            if (password.Length < 6)
                return new ValidationResult("Password must be at least 6 characters.");

            if (!Regex.IsMatch(password, "[A-Z]"))
                return new ValidationResult("Password must contain an uppercase letter.");

            if (!Regex.IsMatch(password, "[a-z]"))
                return new ValidationResult("Password must contain a lowercase letter.");

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return new ValidationResult("Password must contain a special character.");

            return ValidationResult.Success;
        }
    }
}
