using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> entity)
        {
            entity.HasKey(e => e.UserRoleId).HasName("user_roles_pkey");

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "user_roles_role_id_idx");
            entity.HasIndex(e => new { e.UserId, e.RoleId }, "user_roles_user_id_role_id_idx").IsUnique();

            entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_roles_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_roles_user_id_fkey");
        }


    }
}