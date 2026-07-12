using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using linksy_backend_api.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace linksy_backend_api.Infrastructure.Data.Configurations
{

    public class CallParticipantConfiguration : IEntityTypeConfiguration<CallParticipant>
    {
        public void Configure(EntityTypeBuilder<CallParticipant> builder)
        {
            builder.ToTable("call_participants"); // ← snake_case

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.CallLogId)
                .HasColumnName("call_log_id")
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("invited");

            builder.Property(x => x.JoinedAt)
                .HasColumnName("joined_at");

            builder.Property(x => x.LeftAt)
                .HasColumnName("left_at");

            builder.HasOne(x => x.CallLog)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.CallLogId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.CallLogId, x.UserId })
                .IsUnique()
                .HasDatabaseName("ix_call_participants_call_log_id_user_id");
        }
    }
}
