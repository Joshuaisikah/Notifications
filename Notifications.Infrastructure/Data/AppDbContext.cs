using Microsoft.EntityFrameworkCore;

namespace Notifications.Infrastructure.Data;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Outboxmessage> Outboxmessages => Set<Outboxmessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Recipient).IsRequired();
            entity.Property(n => n.Message).IsRequired();
            entity.Property(n => n.Channel).IsRequired();
            entity
                .HasOne(n => n.outboxmessage)
                .WithOne()
                .HasForeignKey<Outboxmessage>(o => o.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Outboxmessage>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Status).HasConversion<string>();
            entity.HasIndex(o => new { o.Status, o.NextRetryAt });
        });
        base.OnModelCreating(modelBuilder);
    }
}
