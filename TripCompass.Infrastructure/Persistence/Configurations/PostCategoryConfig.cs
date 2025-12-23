using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Domain.Entities;

namespace TripCompass.Infrastructure.Persistence.Configurations
{
    public class PostCategoryConfig : IEntityTypeConfiguration<PostCategory>
    {
        public void Configure(EntityTypeBuilder<PostCategory> builder)
        {
            builder.HasKey(x => new { x.PostId, x.CategoryId });

            builder.HasOne(x => x.Post)
                .WithMany(p => p.PostCategories)
                .HasForeignKey(x => x.PostId);

            builder.HasOne(x => x.Category)
                .WithMany(c => c.PostCategories)
                .HasForeignKey(x => x.CategoryId);
        }
    }

}
