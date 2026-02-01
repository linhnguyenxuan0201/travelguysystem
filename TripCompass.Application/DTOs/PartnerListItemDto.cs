namespace TripCompass.Application.DTOs
{
    public class PartnerListItemDto
    {
        public long PartnerId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        
        // Thông tin đăng ký
        public string StoreName { get; set; } = null!;
        public string BusinessType { get; set; } = null!;
        public string RepresentativeName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;
        
        // Thông tin tài khoản ngân hàng
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        
        // Giấy tờ pháp lý
        public string IdNumber { get; set; } = null!;
        public string? TaxId { get; set; }
        
        // Mô tả dịch vụ
        public string? ServiceDescription { get; set; }
        
        // Trạng thái
        public bool IsApproved { get; set; }
        public string StatusDisplay { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
