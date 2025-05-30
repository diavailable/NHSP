﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace NHSP.Payroll.Formula
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
            string customFileName = $"{FileName}";
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
        public async Task<FileContentResult> DownloadFileAsync(string fileName)
        {
            var filePath = Path.Combine(_uploadPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file was not found.", fileName);
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var contentType = "application/octet-stream"; // Default content type for files

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file: {ex.Message}");
                    return false;
                }
            }

            return false;
        }
        public async Task<List<string>> GetFileNameAsync(string searchQuery = null)
        {
            if (!Directory.Exists(_uploadPath))
            {
                return new List<string>();
            }
            var files = Directory.GetFiles(_uploadPath).ToList();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                files = files
                    .Where(file => Path.GetFileNameWithoutExtension(file)
                        .Equals(searchQuery, StringComparison.OrdinalIgnoreCase)) 
                    .ToList();
            }

            var fullfilename = files.Select(Path.GetFileName).ToList();
            return await Task.FromResult(fullfilename);
        }
    }
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
