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
    //test table model for live petty
    public class tbl_usersModel
    {
        [Key]
        public int id { get; set; }
        public string Last_Name { get; set; }
        public string First_Name { get; set; }
        public string Middle_Name { get; set; }
        public string Address { get; set; }
        public string Position { get; set; }
        public int User_Type { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Date_Created { get; set; }
        public string User_Status { get; set; }
        public string Last_Modified_By { get; set; }
        public string Contact_Number { get; set; }
        public string Email { get; set; }
        public string Id_Number { get; set; }
        public tbl_contentsModel ContentsModel { get; set; }
    }
    public class tbl_contentsModel
    {
        [Key]
        public int id { get; set; }
        public string Code { get; set; }
        public string Item_Type { get; set; }
        public string Item_Details { get; set; }
        public string Status { get; set; }
        public string With_OM {get; set; }
    }
    public class site
    {
        [Key]
        public int site_id { get; set; }
        public string site_name{ get; set; }
        public string client_name { get; set; }
        public string om { get; set; }
        public string status { get; set; }

    }
}