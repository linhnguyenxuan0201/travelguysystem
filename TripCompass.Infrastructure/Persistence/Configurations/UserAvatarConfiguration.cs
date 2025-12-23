using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Domain.Entities;

namespace TripCompass.Infrastructure.Persistence.Configurations
{
    public class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
    {
        public void Configure(EntityTypeBuilder<UserAvatar> builder)
        {
            builder.ToTable("UserAvatars");

            builder.HasKey(x => x.UserAvatarId);

            builder.Property(x => x.UserId)
                   .IsRequired();

            builder.HasOne(x => x.User)
       .WithMany()              // ❗ KHÔNG WithMany(u => u.UserAvatars)
       .HasForeignKey(x => x.UserId)
       .OnDelete(DeleteBehavior.Cascade);


            builder.Property(x => x.AvatarUrl)
                   .IsRequired()
                   .HasMaxLength(255);
        }
    }
}
