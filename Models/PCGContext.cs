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
        public DbSet<payroll> PayrollFile { get; set; }
    }
    public class site
    {
        [Key]
        public int site_id { get; set; }
        public string site_name { get; set; }
        public string client_name { get; set; }
        public string om { get; set; }
        public string status { get; set; }

    }
    public class payroll
    {
        [Key]
        public int FileId { get; set; }
        public string? FileName { get; set; }
        public string? FileString { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }
        public int? AddedBy { get; set; }
        public DateTime? AddedDate { get; set; }
        public int? ApproveOM { get; set; }
        public DateTime? ApproveOMDate { get; set; }
        public int? ApproveSOM { get; set; }
        public DateTime? ApproveSOMDate { get; set; }
        public int? ApprovePO { get; set; }
        public DateTime? ApprovePODate { get; set; }
        public int? ApproveIM { get; set; }
        public DateTime? ApproveIMDate { get; set; }
        public int? ApproveACC { get; set; }
        public DateTime? ApproveACCDate { get; set; }
        public DateTime? Release { get; set; }
        public int? Status { get; set; }
    }
}