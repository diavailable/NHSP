using Microsoft.AspNetCore.Mvc;
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
using NHSP.Payroll.Formula;
using NHSP.Areas.Payroll.Models;
using NHSP.Models;
using static Azure.Core.HttpHeader;
using System.Runtime.InteropServices.JavaScript;

namespace NHSP.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    public class PayrollController : Controller
    {
        private readonly NHSPContext _context1;
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
        public PayrollController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment, NHSPContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll)
        {
            _configuration = configuration;
            con1 = new SqlConnection(_configuration.GetConnectionString("nhspConnection"));
            con2 = new SqlConnection(_configuration.GetConnectionString("pettycashConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = new FileUploadService(Path.Combine(_hostingEnvironment.WebRootPath, "PayrollFiles"));
        }

        public void GetSession()
        {
            ViewData["SessionType"] = HttpContext.Session.GetString(SessionType);
            ViewData["SessionId"] = HttpContext.Session.GetString(SessionId);
            ViewData["SessionName"] = HttpContext.Session.GetString(SessionName);
            ViewData["Layout"] = "Payroll";
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DashPayroll()
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null});
            }
            return View();
        }
        public IActionResult RequestPayroll()
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }
            int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
            var sitequery = _context1.Sites
                            .Where(a => a.Status == "Active" && (a.SiteSOM == sesid || a.SiteSCTK == sesid || a.SiteOM == sesid))
                            .Select(site => new
                            {
                                site.SiteId,
                                site.Sitename,
                                site.Status,
                                site.SiteSOM,
                                site.SiteOM,
                                site.SiteSCTK,
                                site.Payroll
                            });

            ViewBag.Site = sitequery.ToList();
 
            return View();
        }
        public IActionResult SelectSite()
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }
            int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
            var sitequery = _context1.Sites
                                .Where(a => a.Status == "Active")
                                .OrderBy(a => a.Sitename)
                                .Select(a => new SiteList
                                {
                                    SiteId = a.SiteId,
                                    Sitename = a.Sitename,
                                    Status = a.Status,
                                    SiteSOM = a.SiteSOM,
                                    SiteOM = a.SiteOM,
                                    SiteSCTK = a.SiteSCTK
                                }).ToList();

            ViewBag.AssignSite = sitequery;

            return PartialView("_SelectSite", sitequery);
        }
        [HttpPost]
        public IActionResult AssignSite(int siteId)
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }
            string position = HttpContext.Session.GetString(SessionType);
            if (position.Contains("OPE") && position.Contains("HEAD"))
            {
                var siteassign = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if (siteassign != null)
                {
                    siteassign.SiteSOM = int.Parse(HttpContext.Session.GetString(SessionId));
                    _context1.SaveChanges();
                };
                return RedirectToAction("ViewPayroll", "Payroll");
            }
            if (position.Contains("OPE") && position.Contains("MAN"))
            {
                var siteassign = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if (siteassign != null)
                {
                    siteassign.SiteOM = int.Parse(HttpContext.Session.GetString(SessionId));
                    _context1.SaveChanges();
                };
                return RedirectToAction("ViewPayroll", "Payroll");
            }
            if ((position.Contains("SITE") && position.Contains("COOR")) || (position.Contains("TIME") && position.Contains("KEEP")))
            {
                var siteassign = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if(siteassign != null)
                {
                    siteassign.SiteSCTK = int.Parse(HttpContext.Session.GetString(SessionId));
                    _context1.SaveChanges();
                };
            }

            return RedirectToAction("RequestPayroll", "Payroll");
        }
        [HttpPost]
        public async Task<IActionResult> Upload(FileModel pm, int siteId)
        {
            try
            {
                GetSession();

                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
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

                var sitequery = _context1.Sites
                                .Where(a => a.SiteId == siteId)
                                .Select(a => new 
                                { 
                                    a.SiteId,
                                    a.Sitename
                                });

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
                string sitename = StringEdit.RightStr(siteresult.Sitename);
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
                    AddedDate = fdt,
                    Status = 1
                };
                _context1.payroll.Add(payrollFile);
                _context1.SaveChanges();

                var sitesToUpdate = _context1.Sites.Where(s => s.SiteId == siteId);
                foreach (var site in sitesToUpdate)
                {
                    site.Payroll = 1;
                }
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
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }

            var sitequery = _context1.Sites
                            .Where(a => a.SiteId == SiteId)
                            .Select(a => new
                            {
                                SiteId = a.SiteId,
                                a.Sitename,
                                a.Status,
                                a.SiteSOM,
                                a.SiteOM,
                                a.SiteSCTK
                            });

            var siteresult = sitequery.FirstOrDefault();

            var fm = new FileModel
            {
                SiteId = siteresult?.SiteId.ToString(),
                SiteName = siteresult?.Sitename.ToString()
            };
            return PartialView("_UploadPartial", fm);
        }
        public IActionResult ViewPayroll()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }
                string sestype = HttpContext.Session.GetString(SessionType);
                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                var user = _context1.Users
                    .Where(u => u.UserStatus == "Active" && u.UserId == sesid)
                    .Select(u => new
                    {
                        u.Position,
                    })
                    .FirstOrDefault();

                if (user != null)
                {
                    if (sestype.Contains("OPE") && sestype.Contains("MAN"))
                    {
                        var payroll = _context1.payroll
                                    .GroupJoin(
                                    _context1.Sites,
                                    p => p.SiteId,
                                    s => s.SiteId,
                                    (p, siteGroup) => new { Payroll = p, Site = siteGroup.FirstOrDefault() }
                                    )
                                    .Where(j => j.Site != null && j.Site.SiteOM == sesid && j.Payroll.Status == 1)
                                    .Select(j => new
                                    {
                                    FileId = j.Payroll.FileId,
                                    FileName = j.Payroll.FileName,
                                    FileString = j.Payroll.FileString,
                                    SiteId = j.Payroll.SiteId,
                                    Sitename = j.Site != null ? j.Site.Sitename : null,
                                    SiteSOM = j.Site != null ? j.Site.SiteSOM : null,
                                    SiteOM = j.Site != null ? j.Site.SiteOM : null,
                                    SiteSCTK = j.Site != null ? j.Site.SiteSCTK : null,
                                    SiteName = j.Payroll.SiteName,
                                    AddedBy = j.Payroll.AddedBy,
                                    AddedDate = j.Payroll.AddedDate,
                                    ApproveOM = j.Payroll.ApproveOM,
                                    ApproveOMDate = j.Payroll.ApproveOMDate,
                                    ApproveSOM = j.Payroll.ApproveSOM,
                                    ApproveSOMDate = j.Payroll.ApproveSOMDate,
                                    ApprovePO = j.Payroll.ApprovePO,
                                    ApprovePODate = j.Payroll.ApprovePODate,
                                    FinalizeBy = j.Payroll.FinalizedBy,
                                    Release = j.Payroll.Release,
                                    ApproveACC = j.Payroll.ApproveACC,
                                    ApproveACCDate = j.Payroll.ApproveACCDate,                                    
                                    Status = j.Payroll.Status
                                    });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("OPE") && sestype.Contains("HEAD"))
                    {
                        var payroll = _context1.payroll
                                    .GroupJoin(
                                    _context1.Sites,
                                    p => p.SiteId,
                                    s => s.SiteId,
                                    (p, siteGroup) => new { Payroll = p, Site = siteGroup.FirstOrDefault() }
                                    )
                                    .Where(j => j.Site != null && j.Site.SiteSOM == sesid && j.Payroll.Status == 2)
                                    .Select(j => new
                                    {
                                        FileId = j.Payroll.FileId,
                                        FileName = j.Payroll.FileName,
                                        FileString = j.Payroll.FileString,
                                        SiteId = j.Payroll.SiteId,
                                        Sitename = j.Site != null ? j.Site.Sitename : null,
                                        SiteSOM = j.Site != null ? j.Site.SiteSOM : null,
                                        SiteOM = j.Site != null ? j.Site.SiteOM : null,
                                        SiteSCTK = j.Site != null ? j.Site.SiteSCTK : null,
                                        SiteName = j.Payroll.SiteName,
                                        AddedBy = j.Payroll.AddedBy,
                                        AddedDate = j.Payroll.AddedDate,
                                        ApproveOM = j.Payroll.ApproveOM,
                                        ApproveOMDate = j.Payroll.ApproveOMDate,
                                        ApproveSOM = j.Payroll.ApproveSOM,
                                        ApproveSOMDate = j.Payroll.ApproveSOMDate,
                                        ApprovePO = j.Payroll.ApprovePO,
                                        ApprovePODate = j.Payroll.ApprovePODate,
                                        FinalizeBy = j.Payroll.FinalizedBy,
                                        Release = j.Payroll.Release,
                                        ApproveACC = j.Payroll.ApproveACC,
                                        ApproveACCDate = j.Payroll.ApproveACCDate,
                                        Status = j.Payroll.Status
                                    });

                        ViewBag.Approvals = payroll.ToList();
                    }
                    if (sestype.Contains("PAYROLL"))
                    {
                        var payroll = _context1.payroll
                                   .Where(c => c.Status == 3 || c.Status == 5)
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
                    if (sestype.Contains("ACCTG") && sestype.Contains("STAFF"))
                    {
                        var payroll = _context1.payroll
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
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }
                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                DateTime? fdt = DateTime.ParseExact(DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss"), "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                var status = _context1.payroll
                              .Where(s => s.SiteId == siteId && s.Status != 0)
                              .Select(s => new
                              {
                                  s.FileId,
                                  s.SiteId,
                                  s.Status
                              }).FirstOrDefault();
                if (status != null)
                {
                    if (status.Status == 1)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 1);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveOM = sesid;
                            payrollFile.ApproveOMDate = fdt;
                            payrollFile.Status = 2;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 2)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 2);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveSOM = sesid;
                            payrollFile.ApproveSOMDate = fdt;
                            payrollFile.Status = 3;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 3)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 3);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApprovePO = sesid;
                            payrollFile.ApprovePODate = fdt;
                            payrollFile.Status = 4;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 4)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 4);
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveACC = sesid;
                            payrollFile.ApproveACCDate = fdt;
                            payrollFile.Status = 5;
                        }
                        _context1.SaveChanges();
                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (status.Status == 5)
                    {
                        var payrollFiles = _context1.payroll
                            .Where(pf => pf.SiteId == siteId && pf.Status == 5)
                            .ToList(); 

                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.FinalizedBy = sesid;
                            payrollFile.Release = fdt;
                            payrollFile.Status = 6;
                        }

                        var payrollSites = _context1.Sites
                            .Where(site => site.SiteId == siteId)
                            .ToList(); 

                        foreach (var site in payrollSites)
                        {
                            site.Payroll = 0;
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
        public IActionResult Decline(int siteId)
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
                var status = _context1.payroll
                              .Where(s => s.SiteId == siteId)
                              .Select(s => new
                              {
                                  s.FileId,
                                  s.SiteId,
                                  s.Status
                              }).FirstOrDefault();
                if (status != null)
                {
                    var payrollFiles = _context1.payroll
                                   .Where(pf => pf.SiteId == siteId);
                    foreach (var payrollFile in payrollFiles)
                    {
                        payrollFile.Status = 0;
                    }
                    _context1.SaveChanges();
                    var prstatus = _context1.Sites
                                   .Where(pf => pf.SiteId == siteId);
                    foreach (var pr in prstatus)
                    {
                        pr.Payroll = 0;
                    }
                    _context1.SaveChanges();
                    return RedirectToAction("ViewPayroll", "Payroll");
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
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }
                var filelist = _context1.payroll
                                .Where(a => a.Status == 0 || a.Status == 6)
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
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);
                string date = yyyy + "-" + MM + "-" + dd + " 23:59:59";
                DateTime? pdate = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var fileName = await _context1.payroll
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
                            var payrollFile = await _context1.payroll
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

    //    public IActionResult Totable()
    //    {
    //        try
    //        {
    //            GetSession();
    //            if (HttpContext.Session.GetString(SessionId) == null)
    //            {
    //                return RedirectToAction("Login", "Home", new { area = (string)null });
    //            }

    //            var newuser = _context1.Users
    //                .Select(b => new
    //                {
    //                    b.FirstName,
    //                    b.LastName
    //                })
    //                .ToList();  

    //            var olduser = (from u in _context2.tbl_users
    //                           join c in _context2.tbl_contents on u.User_Type equals c.id into userContent
    //                           from uc in userContent.DefaultIfEmpty()
    //                           where u.User_Status == 1
    //                           select new
    //                           {
    //                               u.id,
    //                               u.Last_Name,
    //                               u.First_Name,
    //                               u.Middle_Name,
    //                               u.Address,
    //                               u.Position,
    //                               u.User_Type,
    //                               u.Username,
    //                               u.Password,
    //                               u.User_Status,
    //                               u.Contact_Number,
    //                               u.SiteId,
    //                               Usertype = uc.Item_Details
    //                           })
    //          .ToList();

    //            if (!newuser.Any())
    //            {
    //                // Return all users from a table since b is empty
    //               var userlist = olduser.ToList();
    //                ViewBag.Users = userlist;
    //            }
    //            else
    //            {
    //                var userlist = olduser
    //                            .Where(a => !newuser.Any(b => b.FirstName == a.First_Name && b.LastName == a.Last_Name))
    //                            .Select(a => new
    //                            {
    //                                a.id,
    //                                First_Name = a.First_Name ?? "",
    //                                Last_Name = a.Last_Name ?? "",
    //                                Middle_Name = a.Middle_Name ?? "",
    //                                Position = a.Position ?? "",
    //                                Username = a.Username ?? "",
    //                                Password = a.Password ?? "",
    //                                Contact_Number = a.Contact_Number ?? "",
    //                                SiteId = a.SiteId
    //                            })
    //                            .ToList();
    //                ViewBag.Users = userlist;
    //            }                

    //            return View();
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("Error in Forward action: " + ex.Message);
    //            return StatusCode(500);
    //        }
    //    }
    //    public IActionResult MigrateUser()
    //    {
    //        //try
    //        //{
    //        GetSession();
    //        if (HttpContext.Session.GetString(SessionId) == null)
    //        {
    //            return RedirectToAction("Login", "Home", new { area = (string)null });
    //        }
    //        var newuser = _context1.Users
    //                .Select(b => new
    //                {
    //                    b.FirstName,
    //                    b.LastName
    //                })
    //                .ToList();

    //        var olduser = (from u in _context2.tbl_users
    //                       join c in _context2.tbl_contents on u.User_Type equals c.id into userContent
    //                       from uc in userContent.DefaultIfEmpty()
    //                       where u.User_Status == 1
    //                       select new
    //                       {
    //                           u.id,
    //                           u.Last_Name,
    //                           u.First_Name,
    //                           u.Middle_Name,
    //                           u.Address,
    //                           u.Position,
    //                           u.User_Type,
    //                           u.Username,
    //                           u.Password,
    //                           u.User_Status,
    //                           u.Contact_Number,
    //                           u.SiteId,
    //                           Usertype = uc.Item_Details
    //                       })
    //          .ToList();

    //        var userlist = olduser
    //.Where(a => !newuser.Any(b => b.FirstName == a.First_Name && b.LastName == a.Last_Name))
    //.Select(a => new
    //{
    //    a.id,
    //    First_Name = a.First_Name ?? "",
    //    Last_Name = a.Last_Name ?? "",
    //    Middle_Name = a.Middle_Name ?? "",
    //    Position = a.Position ?? "",
    //    Username = a.Username ?? "",
    //    Password = a.Password ?? "",
    //    Contact_Number = a.Contact_Number ?? "",
    //    SiteId = a.SiteId,
    //    Usertype = a.Usertype
    //})
    //.ToList();

    //        foreach (var um in userlist)
    //        {
    //            var trans = new Users
    //            {
    //                LastName = um.Last_Name,
    //                FirstName = um.First_Name,
    //                MiddleName = um.Middle_Name,
    //                Position = um.Position,
    //                Username = um.Username,
    //                Password = um.Password,
    //                UserStatus = "Active",
    //                UserType = um.Usertype,
    //                ContactNo = um.Contact_Number,
    //                SiteId = um.SiteId
    //            };

    //            _context1.Users.Add(trans);
    //        }

    //        _context1.SaveChanges();

    //        return RedirectToAction("Totable", "Payroll");
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    Console.WriteLine("Error in Forward action: " + ex.Message);
    //        //    return StatusCode(500);
    //        //}
    //    }
    //    public IActionResult Sites()
    //    {
    //        //try
    //        //{
    //            GetSession();
    //            if (HttpContext.Session.GetString(SessionType) == null)
    //            {
    //                return RedirectToAction("Login", "Home");
    //            }
    //            ViewBag.Layout = "Payroll";
    //            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

    //        var site = _context2.tbl_contents
    //                    .Where(a => a.Status == 1 && a.Item_Type == "Site")
    //                    .Select(a => new
    //                    {
    //                        a.Item_Details,
    //                        a.Status,
    //                        a.WithOM,
    //                        a.SC_TK,
    //                        a.SOM
    //                    });
    //        var sitelist = site.ToList();

    //        foreach (var ns in sitelist)
    //        {
    //            var trans = new Sites
    //            {
    //                Sitename = ns.Item_Details,
    //                Status = "Active",
    //                SiteSOM = ns.SOM,
    //                SiteOM = ns.WithOM,
    //                SiteSCTK = ns.SC_TK,

    //            };

    //            _context1.Sites.Add(trans);
    //        }

    //        _context1.SaveChanges();

    //        return View();
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    Console.WriteLine("Error in Forward action: " + ex.Message);
    //        //    return StatusCode(500);
    //        //}
    //    }
        public IActionResult Completed()
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

                var prcomplete = _context1.payroll
                                .Where(pf => pf.Status == 6 || pf.Status == 0)
                                .Select(pf => new
                                {
                                    pf.FileId,
                                    pf.FileName,
                                    pf.FileString,
                                    pf.SiteId,
                                    pf.SiteName,
                                    pf.AddedBy,
                                    pf.AddedDate,
                                    pf.ApproveOM,
                                    pf.ApproveOMDate,
                                    pf.ApproveSOM,
                                    pf.ApproveSOMDate,
                                    pf.ApprovePO,
                                    pf.ApprovePODate,
                                    pf.FinalizedBy,                                    
                                    pf.ApproveACC,
                                    pf.ApproveACCDate,
                                    pf.Release,
                                    pf.Status
                                });

                ViewBag.Completed = prcomplete.ToList();
                return View();
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