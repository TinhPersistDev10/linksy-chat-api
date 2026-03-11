using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class MemberPermissionConfiguration : IEntityTypeConfiguration<MemberPermission>
    {
        public void Configure(EntityTypeBuilder<MemberPermission> entity)
        {

            entity.HasKey(e => e.PermissionId).HasName("permission_members_pkey");

            entity.ToTable("member_permissions");

            entity.HasIndex(e => e.MemberId, "permission_members_member_id_idx").IsUnique();

            entity.Property(e => e.PermissionId)
                .ValueGeneratedNever()
                .HasColumnName("permission_id");

            entity.Property(e => e.MemberId)
                .HasColumnName("member_id");

            entity.Property(e => e.CanSendMessages)
                .HasDefaultValue(true)
                .HasColumnName("can_send_messages");
            //Can send media
            entity.Property(e => e.CanSendMedia)
                .HasDefaultValue(true)
                .HasColumnName("can_send_media");
            //Can send voice
            entity.Property(e => e.CanSendVoice)
                .HasDefaultValue(true)
                .HasColumnName("can_send_voice");
            //Can send files
            entity.Property(e => e.CanSendFiles)
                            .HasDefaultValue(true)
                            .HasColumnName("can_send_files");
            //Can pin message
            entity.Property(e => e.CanPinMessages)
                .HasDefaultValue(true)
                .HasColumnName("can_pin_messages");
            //Can manage calls
            entity.Property(e => e.CanManageCalls)
                            .HasDefaultValue(true)
                            .HasColumnName("can_manage_calls");
            //Can invite members
            entity.Property(e => e.CanInviteMembers)
                .HasDefaultValue(true)
                .HasColumnName("can_invite_members");

            //Can edit group info
            entity.Property(e => e.CanEditGroupInfo)
                .HasDefaultValue(false)
                .HasColumnName("can_edit_group_info");

            //Can delete message
            entity.Property(e => e.CanDeleteMessages)
                .HasDefaultValue(false)
                .HasColumnName("can_delete_messages");

            //Can remove members
            entity.Property(e => e.CanRemoveMembers)
                .HasDefaultValue(false)
                .HasColumnName("can_remove_members");

            //Create at
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            //Update at
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");



            entity.HasOne(d => d.Member)
                .WithOne(p => p.MemberPermission)
                .HasForeignKey<MemberPermission>(d => d.MemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("permission_members_member_id_fkey");
        }
    }
}
