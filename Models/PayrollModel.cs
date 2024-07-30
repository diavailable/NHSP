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
        [Required(ErrorMessage = "Please select a file to upload.")]
        [DataType(DataType.Upload)]
        [FileExtensions(Extensions = "xls,png,jpeg,jpg", ErrorMessage = "Only .xls, .png, and .jpeg file types are allowed.")]
        public IFormFile UploadFile { get; set; }
        public string FileName { get; set; }
        public string SiteId { get; set; }
    }
}
