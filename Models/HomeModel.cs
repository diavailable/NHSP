using Microsoft.EntityFrameworkCore;

namespace NHSP.Models
{
    public class HomeModel : DbContext
    {
        public HomeModel(DbContextOptions<HomeModel> options) : base(options)
        {

        }
        public DbSet<HomeModel> Dataset { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}