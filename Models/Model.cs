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
        public string? AddedBy { get; set; }
        public DateTime? AddedDate { get; set; }
        public string? ApproveOM { get; set; }
        public DateTime? ApproveOMDate { get; set; }
        public string? ApproveSOM { get; set; }
        public DateTime? ApproveSOMDate { get; set; }
        public string? ApproveIM { get; set; }
        public DateTime? ApproveIMDate { get; set; }
        public string? ApproveACC { get; set; }
        public DateTime? ApproveACCDate { get; set; }
        public DateTime? Release { get; set; }
    }
}