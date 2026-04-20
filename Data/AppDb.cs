using BackgroundServices.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BackgroundServices.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<Campaign>(e =>
    //    {
    //        e.HasKey(x => x.Id);
    //        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
    //        e.Property(x => x.Subject).HasMaxLength(500);
    //        e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Draft");
    //    });

    //    modelBuilder.Entity<EmailLog>(e =>
    //    {
    //        e.HasKey(x => x.Id);
    //        e.Property(x => x.Email).HasMaxLength(256).IsRequired();
    //        e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");

    //        e.HasOne(x => x.Campaign)
    //         .WithMany(c => c.EmailLogs)
    //         .HasForeignKey(x => x.CampaignId)
    //         .OnDelete(DeleteBehavior.Cascade);

    //        e.HasIndex(x => x.CampaignId);
    //        e.HasIndex(x => new { x.CampaignId, x.Status });
    //    });
    //}
}
