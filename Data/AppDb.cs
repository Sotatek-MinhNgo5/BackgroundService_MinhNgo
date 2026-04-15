using BackgroundServices.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BackgroundServices.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<EmailLog> EmailLogs { get; set; }
}
