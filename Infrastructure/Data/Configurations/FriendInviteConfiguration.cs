using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations;

public class FriendInviteConfiguration : IEntityTypeConfiguration<FriendInvite>
{
    public void Configure(EntityTypeBuilder<FriendInvite> entity)
    {
        entity.HasKey(e => e.Id).HasName("friend_invites_pkey");
        entity.ToTable("friend_invites");

        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Token)
            .HasColumnName("token")
            .HasMaxLength(128)
            .IsRequired();
        entity.Property(e => e.InviterId).HasColumnName("inviter_id");
        entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        entity.Property(e => e.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false);
        entity.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        entity.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("friend_invites_token_uidx");
        entity.HasIndex(e => new { e.InviterId, e.IsUsed, e.ExpiresAt })
            .HasDatabaseName("friend_invites_inviter_active_idx");

        entity.HasOne(d => d.Inviter)
            .WithMany()
            .HasForeignKey(d => d.InviterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("friend_invites_inviter_id_fkey");
    }
}
