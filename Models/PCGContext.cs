using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NHSP.Models
{
    public class PCGContext : DbContext
    {
        public PCGContext(DbContextOptions<PCGContext> options) : base(options)
        {
        }
        public DbSet<site> site { get; set; }
        public DbSet<PayrollFile> PayrollFile { get; set; }
    }

}