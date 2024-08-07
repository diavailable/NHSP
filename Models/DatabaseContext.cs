using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NHSP.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {

        }
        public DbSet<tbl_usersModel> tbl_users { get; set; }
        public DbSet<tbl_contentsModel> tbl_contents { get; set; }
    }
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
        public int User_Status { get; set; }
        public string Last_Modified_By { get; set; }
        public string Contact_Number { get; set; }
        public string Id_Number { get; set; }
    }
    public class tbl_contentsModel
    {
        public int id { get; set; }
        public string Code { get; set; }
        public string Item_Type { get; set; }
        public string Item_Details { get; set; }
        public int? Status { get; set; }
        public int? WithOM { get; set; }
    }
}
