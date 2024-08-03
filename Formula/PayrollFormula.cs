using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace NHSP.Formula
{
    public class PayrollFormula
    {
    }
    public class FileUploadService
    {
        private readonly string _uploadPath;

        public FileUploadService(string uploadPath)
        {
            _uploadPath = uploadPath;
        }

        public async Task<(string FilePath, string DirectoryPath)> UploadFileAsync(IFormFile file, string FileName)
        {
            if (file == null || file.Length == 0)
            {
                return (null, null);
            }

            // Ensure the file extension is preserved
            var extension = Path.GetExtension(file.FileName);
            string datePart = DateTime.Now.ToString("MMddyy");
            string customFileName = $"{datePart}{FileName}";
            var filePath = Path.Combine(_uploadPath, customFileName);

            // Create directory if it does not exist
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return (filePath, customFileName);
        }
    }
    // string extract
    public static class StringEdit
    {
        public static string RightStr(string input)
        {
            if (input == null) return null;

            string[] parts = input.Split('-');

            if (parts.Length > 1)
            {
                string[] words = parts[1].Trim().Split(' ');

                if (words.Length > 0)
                {
                    return words[0].Trim();
                }
            }
            return input;
        }
        public static string LeftStr(string input)
        {
            if (input == null) return null;

            string[] parts = input.Split('-');
            if (parts.Length > 0)
            {
                return parts[0].Trim();
            }
            return input;
        }
        public static string NoSpaceUpper(string input)
        {
            if (input == null) return null;
            return input.ToUpper().Replace(" ", "");
        }
    }
}
