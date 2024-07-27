using Microsoft.EntityFrameworkCore;

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
}
