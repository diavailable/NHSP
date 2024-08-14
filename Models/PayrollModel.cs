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
    public class ViewPayrollModel
    {
        public int FileId { get; set; }
        public string FileName { get; set; }  
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public DateTime? AddedDate { get; set; }
        public string SiteStatus { get; set; }
        public int WithOM { get; set; }
    }
}
