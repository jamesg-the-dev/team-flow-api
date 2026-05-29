using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Identity;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> b)
    {
        b.ToTable("profiles");
        b.HasKey(x => x.Id);

        // Supabase auth user id — unique, app-side FK (auth schema is owned by Supabase,
        // so we don't declare a CLR FK relationship here).
        b.Property(x => x.UserId).IsRequired();
        b.HasIndex(x => x.UserId).IsUnique();

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(100);
        b.Property(x => x.AvatarPath).HasMaxLength(2000);
        b.Property(x => x.Bio).HasMaxLength(500);
        b.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
        b.Property(x => x.Locale).HasMaxLength(16).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");

        b.Ignore(x => x.DomainEvents);
    }
}
