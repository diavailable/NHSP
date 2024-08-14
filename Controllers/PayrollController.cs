using Microsoft.AspNetCore.Mvc;
using NHSP.Models;
using NHSP.Formula;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Drawing;
using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.IO;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting.Internal;
using System.Globalization;
using static System.Net.WebRequestMethods;

namespace NHSP.Controllers
{
    public class PayrollController : Controller
    {
        private readonly PCGContext _context1;
        private readonly DatabaseContext _context2;
        const string SessionName = "_Name";
        const string SessionLayout = "_Layout";
        const string SessionType = "_Type";
        const string SessionId = "_Id";

        public SqlConnection con1;
        public SqlConnection con2;
        public SqlCommand cmd;
        private readonly IConfiguration _configuration;
        private readonly FileUploadService _fileUploadPayroll;
        private readonly FileUploadService _fileUploadService;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public PayrollController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment, PCGContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll)
        {
            _configuration = configuration;
            con1 = new SqlConnection(_configuration.GetConnectionString("pcgConnection"));
            con2 = new SqlConnection(_configuration.GetConnectionString("portestConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = new FileUploadService(Path.Combine(_hostingEnvironment.WebRootPath, "PayrollFiles"));
        }

        public void GetSession()
        {
            ViewBag.Type = HttpContext.Session.GetString(SessionType);
            ViewBag.SessionId = HttpContext.Session.GetString(SessionId); ;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DashPayroll()
        {
            GetSession();
            if (ViewBag.Type == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "Payroll";
            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);
            return View();
        }
        public IActionResult RequestPayroll()
        {
            GetSession();
            if (ViewBag.Type == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "Payroll";

              var sitequery = _context2.tbl_contents
                              .Where(c => c.Item_Type == "Site" && c.Status == 1)
                              .Select(c => new
                              {
                              SiteId = c.id,
                              c.Code,
                              c.Item_Type,
                              SiteName = c.Item_Details,
                              c.Status,
                              c.WithOM
                              });

            ViewBag.Site = sitequery.ToList();

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(FileModel pm, int siteId)
        {
            try
            {
                GetSession();

                if (ViewBag.Type == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                if (pm.UploadFile == null || pm.UploadFile.Length == 0)
                {
                    ModelState.AddModelError("UploadFile", "Please select a file to upload.");
                    return PartialView("_UploadPartial", pm);
                }

                var allowedExtensions = new[] { ".xls", ".xlsx", ".png", ".jpeg", ".jpg" };
                var extension = Path.GetExtension(pm.UploadFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("UploadFile", "Only .xls, .xlsx, .png, .jpeg, .jpg file types are allowed.");
                    return PartialView("_UploadPartial", pm);
                }

                var sitequery = from u in _context2.tbl_users
                                join c in _context2.tbl_contents
                                on u.id equals c.WithOM into userContents
                                from c in userContents.DefaultIfEmpty()
                                where c != null && c.id == siteId
                                select new
                                {
                                    UserId = u.id,
                                    u.First_Name,
                                    u.Last_Name,
                                    SiteId = c.id,
                                    SiteName = c != null ? c.Item_Details : null,
                                    Status = c != null ? c.Status : (int?)null,
                                    WithOM = c != null ? c.WithOM : (int?)null
                                };

                var siteresult = sitequery.FirstOrDefault();

                if (siteresult == null)
                {
                    ModelState.AddModelError("SiteId", "Invalid site ID.");
                    return PartialView("_UploadPartial", pm);
                }

                string fileformat;
                if (extension == ".xlsx")
                {
                    fileformat = Path.ChangeExtension(pm.UploadFile.FileName, ".xls");
                }
                else
                {
                    fileformat = pm.UploadFile.FileName;
                }

                pm.FileName = "_" + siteresult.SiteId + "_" + ViewBag.SessionId + Path.GetExtension(fileformat);
                var (filePath, rawfilename) = await _fileUploadPayroll.UploadFileAsync(pm.UploadFile, pm.FileName);

                DateTime? fdt = DateTime.ParseExact(DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss"), "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                string filedir = filePath;
                string fname = rawfilename;
                string sitename = StringEdit.RightStr(siteresult.SiteName);
                int sctk = int.Parse(HttpContext.Session.GetString(SessionId));
                if (con1.State == ConnectionState.Closed)
                {
                    con1.Open();
                }

                var payrollFile = new payroll
                {
                    FileName = fname,
                    FileString = filedir,
                    SiteId = siteresult.SiteId,
                    SiteName = sitename,
                    AddedBy = sctk,
                    AddedDate = fdt
                };
                _context1.PayrollFile.Add(payrollFile);
                _context1.SaveChanges();

                if (con1.State == ConnectionState.Open)
                {
                    con1.Close();
                }

                if (!string.IsNullOrEmpty(filePath))
                {                    
                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Uploaded successfully.";
                }
                else
                {
                    ModelState.AddModelError("UploadFile", "File upload failed.");
                    return PartialView("_UploadPartial", pm);
                }

                return PartialView("_UploadPartial", pm);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Upload action: " + ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public IActionResult UploadPartial(int SiteId)
        {
            GetSession();
            if (ViewBag.Type == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "Payroll";
            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

            var sitequery = from u in _context2.tbl_users
                            join c in _context2.tbl_contents
                            on u.id equals c.WithOM into userContents
                            from c in userContents.DefaultIfEmpty()
                            where c != null && c.id == SiteId
                            select new
                            {
                                UserId = u.id,
                                u.First_Name,
                                u.Last_Name,
                                SiteId = c.id,
                                SiteName = c != null ? c.Item_Details : null,
                                Status = c != null ? c.Status : (int?)null,
                                WithOM = c != null ? c.WithOM : (int?)null
                            };
                            
            var siteresult = sitequery.FirstOrDefault();

            var fm = new FileModel
            {
                SiteId = siteresult?.SiteId.ToString()
            };

            return PartialView("_UploadPartial", fm);
        }
        public IActionResult ViewPayroll()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                string sestype = HttpContext.Session.GetString(SessionType);
                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                var user = _context2.tbl_users
                    .Where(u => u.User_Status == 1 && u.id == sesid)
                    .Select(u => new
                    {
                        u.Position,
                    })
                    .FirstOrDefault();

                if (user != null)
                {
                    if(sestype.Contains("OPE") && sestype.Contains("MAN"))
                    {
                        var payroll = _context1.PayrollFile
                                   .Where(c => c.Status == null)
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                   });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("OPE") && sestype.Contains("HEAD"))
                    {
                        var payroll = _context1.PayrollFile
                                   .Where(c => c.Status == 1)
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                   });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("ACC") && sestype.Contains("MAN"))
                    {
                        var payroll = _context1.PayrollFile
                                   .Where(c => c.Status == 2)
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                   });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("INSP"))
                    {
                        var payroll = _context1.PayrollFile
                                   .Where(c => c.Status == 3)
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                   });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("ACCTG"))
                    {
                        var payroll = _context1.PayrollFile
                                   .Where(c => c.Status == 4)
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                   });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    return View();
                }   
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var fileResult = await _fileUploadPayroll.DownloadFileAsync(fileName);
                return fileResult;
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpPost]
        public IActionResult Approve(int siteId)
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);
                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                DateTime? fdt = DateTime.ParseExact(DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss"), "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                var status = _context1.PayrollFile
                              .Where(s => s.SiteId == siteId)
                              .Select(s => new
                              {
                                  s.FileId,
                                  s.SiteId,
                                  s.Status
                              }).FirstOrDefault();
                if (status != null)
                {
                    if (status.Status == null)
                    {
                        var payrollFiles = _context1.PayrollFile
                                       .Where(pf => pf.SiteId == siteId);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveOM = sesid;
                            payrollFile.ApproveOMDate = fdt;
                            payrollFile.Status = 1;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 1)
                    {
                        var payrollFiles = _context1.PayrollFile
                                       .Where(pf => pf.SiteId == siteId);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveSOM = sesid;
                            payrollFile.ApproveSOMDate = fdt;
                            payrollFile.Status = 2;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 2)
                    {
                        var payrollFiles = _context1.PayrollFile
                                       .Where(pf => pf.SiteId == siteId);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApprovePO = sesid;
                            payrollFile.ApprovePODate = fdt;
                            payrollFile.Status = 3;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 3)
                    {
                        var payrollFiles = _context1.PayrollFile
                                       .Where(pf => pf.SiteId == siteId);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveIM = sesid;
                            payrollFile.ApproveIMDate = fdt;
                            payrollFile.Status = 4;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 4)
                    {
                        var payrollFiles = _context1.PayrollFile
                                       .Where(pf => pf.SiteId == siteId);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveACC = sesid;
                            payrollFile.ApproveACCDate = fdt;
                            payrollFile.Status = 5;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult DeleteFile()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

                var filelist = _context1.PayrollFile
                                .Where(a => a.Status != 0)
                                .Select(a => new
                                {
                                    a.FileId,
                                    a.FileName,
                                    a.SiteName,
                                    a.AddedDate
                                });
                ViewBag.FileList = filelist.ToList();

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteFile(string MM, string dd, string yyyy)
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);
                string date = yyyy + "-" + MM + "-" + dd + " 23:59:59";
                DateTime? pdate = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var fileName = await _context1.PayrollFile
                    .Where(a => a.AddedDate <= pdate)
                    .Select(a => new
                    {
                        a.FileId,
                        a.FileName,
                        a.SiteName,
                        a.AddedDate
                    })
                    .ToListAsync();

                if (fileName != null && fileName.Any())
                {
                    foreach (var file in fileName)
                    {
                        string fileToDelete = file.FileName;
                        var fileDeleted = await _fileUploadService.DeleteFileAsync(fileToDelete);

                        if (fileDeleted)
                        {
                            var payrollFile = await _context1.PayrollFile
                            .FirstOrDefaultAsync(p => p.FileName == file.FileName);

                            if (payrollFile != null)
                            {
                                payrollFile.Status = 0;
                                await _context1.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to delete file '{file.FileName}'.");
                            TempData["ErrorMessage"] = "Failed to delete the file.";
                        }
                    }
                    return RedirectToAction("DeleteFile");
                }
                else
                {
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("DeleteFile");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult New()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
    }
}