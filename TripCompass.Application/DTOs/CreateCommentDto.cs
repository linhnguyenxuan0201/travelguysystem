using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.DTOs
{
    public class CreateCommentDto
    {
        public long PostId { get; set; }
        public string Content { get; set; } = null!;
    }

}
