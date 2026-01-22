namespace TripCompass.Domain.Entities
{
    public class Partner
    {
        public long PartnerId { get; set; }
        public long UserId { get; set; }

        // Thông tin đăng ký
        public string StoreName { get; set; } = null!; // Tên cửa hàng/Doanh nghiệp
        public string BusinessType { get; set; } = null!; // Loại hình kinh doanh
        public string RepresentativeName { get; set; } = null!; // Họ và tên người đại diện
        public string PhoneNumber { get; set; } = null!; // Số điện thoại
        public string BusinessAddress { get; set; } = null!; // Địa chỉ kinh doanh

        // Thông tin tài khoản ngân hàng
        public string BankName { get; set; } = null!; // Tên ngân hàng
        public string AccountNumber { get; set; } = null!; // Số tài khoản
        public string AccountHolderName { get; set; } = null!; // Tên chủ tài khoản

        // Giấy tờ pháp lý
        public string IdNumber { get; set; } = null!; // Số CCCD/CMND
        public string? TaxId { get; set; } // Mã số thuế (nếu có)

        // Mô tả dịch vụ
        public string? ServiceDescription { get; set; } // Mô tả chi tiết về dịch vụ

        // Trạng thái
        public bool IsApproved { get; set; } = false; // Đã được phê duyệt chưa
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
