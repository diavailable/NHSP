using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace NHSP.Models
{
    public class Model
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }
    }
    public class LoginModel
    {
        [Required(ErrorMessage = "Invalid username")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Invalid password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
    //pcg models
    public class UsersModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string ContactNo { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string Code { get; set; }
    }
    //portal models
    public class AdminTBLModel
    {
        [Key]
        public string AdminId { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string SiteId { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string UserName { get; set; }
    }
    public class ChangeRateHistoryTBLModel
    {
        [Key]
        public string AdminId { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string SiteId { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string UserName { get; set; }
    }
    // _context2 pettycash db _context2
    public class PayrollViewModel
    {
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
        public int? ApproveIM { get; set; }
        public DateTime? ApprovePODate { get; set; }
        public int? ApproveACC { get; set; }
        public DateTime? ApproveACCDate { get; set; }
        public DateTime? Release { get; set; }
    }
    public class RequestSites
    {
        [Key]
        public int SiteId { get; set; }
        public string Sitename { get; set; }
        public string Status { get; set; }
        public DateTime? AddedDate { get; set; }
        public DateTime? ApproveOMDate { get; set; }
        public DateTime? ApproveSOMDate { get; set; }
        public DateTime? ApprovePODate { get; set; }
        public DateTime? ApproveACCDate { get; set; }
        public DateTime? Release { get; set; }
        public int FileId { get; set; }
        public int? SiteSOM { get; set; }
        public int? SiteOM { get; set; }
        public int? SiteSCTK { get; set; }
        public int Payroll { get; set; }
        public int PayrollStatus { get; set; }
    }
}