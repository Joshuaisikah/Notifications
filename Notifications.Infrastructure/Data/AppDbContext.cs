using Microsoft.EntityFrameworkCore;

namespace Notifications.Infrastructure.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Recipient).IsRequired();
            entity.Property(n => n.Message).IsRequired();
            entity.Property(n => n.Channel).IsRequired();
            entity
                .HasOne(n => n.OutboxMessage)
                .WithOne()
                .HasForeignKey<OutboxMessage>(o => o.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Status).HasConversion<string>();
            entity.HasIndex(o => new { o.Status, o.NextRetryAt });
        });
        base.OnModelCreating(modelBuilder);
    }
}
