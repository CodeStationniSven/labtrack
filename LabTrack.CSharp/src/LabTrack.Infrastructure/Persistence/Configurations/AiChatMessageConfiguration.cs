using LabTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabTrack.Infrastructure.Persistence.Configurations;

public class AiChatMessageConfiguration : IEntityTypeConfiguration<AiChatMessage>
{
    public void Configure(EntityTypeBuilder<AiChatMessage> builder)
    {
        builder.ToTable("AiChatMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Question)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(m => m.Answer)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(m => m.ModelUsed)
            .HasMaxLength(50);

        builder.Property(m => m.PromptTokens);

        builder.Property(m => m.CompletionTokens);

        builder.Property(m => m.ResponseTimeMs);

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.CreatedAtUtc);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}