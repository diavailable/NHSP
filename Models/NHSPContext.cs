using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NHSP.Models
{
    public class NHSPContext : DbContext
    {
        public NHSPContext(DbContextOptions<NHSPContext> options) : base(options)
        {
        }
        public DbSet<site> site { get; set; }
        public DbSet<payroll> payroll { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Sites> Sites { get; set; }
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
        public int? ApproveACC { get; set; }
        public DateTime? ApproveACCDate { get; set; }
        public int? FinalizedBy { get; set; }
        public DateTime? Release { get; set; }
        public int? Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class Users
    {
        [Key]
        public int UserId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Position { get; set; }
        public string UserType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserStatus { get; set; }
        public string ContactNo { get; set; }
        public int? SiteId { get; set; }
    }
    public class Sites
    {
        [Key]
        public int SiteId { get; set; }
        public string Sitename { get; set; }   
        public string Status { get; set; }
        public int? SiteSOM { get; set; }
        public int? SiteOM { get; set; }
        public int? SiteSCTK{ get; set; }
        public int Payroll { get; set; }
    }
}