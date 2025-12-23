using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TripCompass.Domain.Entities
{
    public class Category
    {
        [Key] // 🔥 BẮT BUỘC
        public long CategoryId { get; set; }

        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }

        public ICollection<PostCategory> PostCategories { get; set; }
            = new List<PostCategory>();
    }
}
