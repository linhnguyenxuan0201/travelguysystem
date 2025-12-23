using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class PostCategory
    {
        public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }

}
