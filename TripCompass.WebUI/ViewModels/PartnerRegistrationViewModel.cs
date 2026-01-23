using System.ComponentModel.DataAnnotations;

namespace TripCompass.WebUI.ViewModels
{
    public class PartnerRegistrationViewModel
    {
        [Required(ErrorMessage = "Tên cửa hàng/Doanh nghiệp là bắt buộc")]
        [Display(Name = "Tên cửa hàng/Doanh nghiệp")]
        public string StoreName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại hình kinh doanh là bắt buộc")]
        [Display(Name = "Loại hình kinh doanh")]
        public string BusinessType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên người đại diện là bắt buộc")]
        [Display(Name = "Họ và tên người đại diện")]
        public string RepresentativeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ kinh doanh là bắt buộc")]
        [Display(Name = "Địa chỉ kinh doanh")]
        public string BusinessAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc")]
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tài khoản là bắt buộc")]
        [Display(Name = "Số tài khoản")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên chủ tài khoản là bắt buộc")]
        [Display(Name = "Tên chủ tài khoản")]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD/CMND là bắt buộc")]
        [Display(Name = "Số CCCD/CMND")]
        public string IdNumber { get; set; } = string.Empty;

        [Display(Name = "Mã số thuế")]
        public string? TaxId { get; set; }

        [Display(Name = "Mô tả dịch vụ")]
        public string? ServiceDescription { get; set; }

        [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản")]
        public bool AgreeToTerms { get; set; }

        public bool WantPromotions { get; set; }
    }
}
