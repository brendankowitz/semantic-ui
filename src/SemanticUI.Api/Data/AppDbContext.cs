using Microsoft.EntityFrameworkCore;
using SemanticUI.Api.Models;

namespace SemanticUI.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<UIComponent> UIComponents { get; set; }
    public DbSet<UIInteraction> UIInteractions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.ChatId, e.CreatedAt });

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UIComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ComponentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.ChatId, e.CreatedAt });
            entity.HasIndex(e => new { e.ChatId, e.IsActive });

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.UIComponents)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UIInteraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.ComponentId, e.CreatedAt });
        });
    }
}
