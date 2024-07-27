using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NHSP.Models
{
    public class HomeModel : DbContext
    {
        public HomeModel(DbContextOptions<HomeModel> options) : base(options)
        {

        }
        public DbSet<> Dataset { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}