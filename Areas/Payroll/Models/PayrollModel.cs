using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace NHSP.Areas.Payroll.Models
{
    public class PayrollModel
    {
        [Required]
        public IFormFile UploadFile { get; set; }
        public string FileName { get; set; }
        public string SiteId { get; set; }
    }
    public class FileModel
    {
        [BindRequired]
        [DataType(DataType.Upload)]
        public IFormFile UploadFile { get; set; }
        public string FileName { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
    }
    public class ViewPayrollModel
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public DateTime? AddedDate { get; set; }
        public string SiteStatus { get; set; }
        public int WithOM { get; set; }
        public string Remarks { get; set; }
    }
    public class UserMigrate
    {
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
        public int SiteId { get; set; }
    }
    public class SiteList
    {
        public int SiteId { get; set; }
        public string Sitename { get; set; }
        public string Status { get; set; }
        public int? SiteSOM { get; set; }
        public int? SiteOM { get; set; }
        public int? SiteSCTK { get; set; }
    }
}
