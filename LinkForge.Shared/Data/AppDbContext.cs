using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LinkForge.Shared.Models;

namespace LinkForge.Shared.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Link> Links => Set<Link>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ShortCode).HasMaxLength(16).IsRequired();
            entity.Property(e => e.OriginalUrl).HasMaxLength(2048).IsRequired();
        });

        modelBuilder.Entity<ClickEvent>(entity =>
        {
            entity.HasIndex(e => new { e.LinkId, e.ClickedAt });
            entity.HasOne(e => e.Link)
                  .WithMany()
                  .HasForeignKey(e => e.LinkId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
