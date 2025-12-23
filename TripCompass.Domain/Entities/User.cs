namespace TripCompass.Domain.Entities
{
    public class User
    {
        public long UserId { get; private set; }   // 👈 TRÙNG DB
        public string UserName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public bool IsBanned { get; private set; }
        public int ReputationScore { get; set; }
        public int ReputationLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<UserRole> UserRoles { get; private set; }
            = new List<UserRole>();
        

        private User() { }

        public User(string userName, string email, string passwordHash)
        {
            UserName = userName;
            Email = email;
            PasswordHash = passwordHash;
            IsBanned = false;
        }
        // ✅ ADD METHOD NÀY
        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
        }
    }
}
