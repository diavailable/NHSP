using Microsoft.EntityFrameworkCore;

namespace NHSP.Models
{
    public class NewHireModel : DbContext
    {
        public NewHireModel(DbContextOptions<NewHireModel> options) : base(options)
        {

        }
        public DbSet<NewHireModel> Dataset { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}