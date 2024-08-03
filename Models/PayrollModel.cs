using System.ComponentModel.DataAnnotations;

namespace NHSP.Models
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
        [DataType(DataType.Upload)]
        public IFormFile UploadFile { get; set; }
        public string FileName { get; set; }
        public string SiteId { get; set; }
    }
}
