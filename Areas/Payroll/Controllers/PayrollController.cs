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
using Microsoft.AspNetCore.Rewrite;

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
                return RedirectToAction("Login", "Home", new { area = (string)null });
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
            //var sitequery = _context1.Sites
            //                .Where(a => a.Status == "Active" && (a.SiteSOM == sesid || a.SiteSCTK == sesid || a.SiteOM == sesid))
            //                .Select(site => new
            //                {
            //                    site.SiteId,
            //                    site.Sitename,
            //                    site.Status,
            //                    site.SiteSOM,
            //                    site.SiteOM,
            //                    site.SiteSCTK,
            //                    site.Payroll
            //                });

            var sitequery = _context1.Sites
                            .Where(a => a.Status == "Active" && (a.SiteSOM == sesid || a.SiteSCTK == sesid || a.SiteOM == sesid))
                            .GroupJoin(_context1.payroll,
                            s => s.SiteId,
                            p => p.SiteId,
                            (s, payrollGroup) => new
                            {
                                Site = s,
                                LatestPayroll = payrollGroup
                                .OrderByDescending(p => p.AddedDate)
                                .FirstOrDefault()
                            })
                            .Select(x => new
                            {
                                x.Site.SiteId,
                                x.Site.Sitename,
                                x.Site.Status,
                                x.Site.SiteSOM,
                                x.Site.SiteOM,
                                x.Site.SiteSCTK,
                                x.Site.Payroll,
                                PayrollStatus = x.LatestPayroll != null ? x.LatestPayroll.Status : null,
                                AddedBy = x.LatestPayroll != null ? x.LatestPayroll.AddedBy : null,
                                AddedDate = x.LatestPayroll != null ? x.LatestPayroll.AddedDate : null,
                                ApproveOMDate = x.LatestPayroll != null ? x.LatestPayroll.ApproveOMDate : null,
                                ApproveSOMDate = x.LatestPayroll != null ? x.LatestPayroll.ApproveSOMDate : null,
                                ApprovePO = x.LatestPayroll != null ? x.LatestPayroll.ApprovePODate : null,
                                ApproveACCDate = x.LatestPayroll != null ? x.LatestPayroll.ApproveACCDate : null,
                                Release = x.LatestPayroll != null ? x.LatestPayroll.Release : null,
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
                                .Where(a => a.Status == "Active" && 
                                (a.SiteOM == null || a.SiteOM != sesid) &&
                                (a.SiteSOM == null || a.SiteSOM != sesid) &&
                                (a.SiteSCTK == null || a.SiteSCTK != sesid))
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

                pm.FileName = siteresult.SiteId + "_" + ViewBag.SessionId + "_" + DateTime.Now.ToString("MMddyy") + Path.GetExtension(fileformat) ;
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
                        var latestPayrolls = _context1.payroll
                                            .Where(p => p.Status == 1)
                                            .GroupBy(p => p.SiteId)
                                            .Select(g => g.OrderByDescending(p => p.AddedDate).FirstOrDefault())
                                            .ToList(); 

                        var payroll = latestPayrolls
                            .Join(
                                _context1.Sites,                                
                                p => p.SiteId,
                                s => s.SiteId,
                                (p, site) => new
                                {
                                    FileId = p.FileId,
                                    FileName = p.FileName,
                                    FileString = p.FileString,
                                    SiteId = p.SiteId,
                                    Sitename = site != null ? site.Sitename : null,
                                    SiteSOM = site != null ? site.SiteSOM : null,
                                    SiteOM = site != null ? site.SiteOM : null,
                                    SiteSCTK = site != null ? site.SiteSCTK : null,
                                    SiteName = p.SiteName,
                                    AddedBy = p.AddedBy,
                                    AddedDate = p.AddedDate,
                                    ApproveOM = p.ApproveOM,
                                    ApproveOMDate = p.ApproveOMDate,
                                    ApproveSOM = p.ApproveSOM,
                                    ApproveSOMDate = p.ApproveSOMDate,
                                    ApprovePO = p.ApprovePO,
                                    ApprovePODate = p.ApprovePODate,
                                    FinalizeBy = p.FinalizedBy,
                                    Release = p.Release,
                                    ApproveACC = p.ApproveACC,
                                    ApproveACCDate = p.ApproveACCDate,
                                    Status = p.Status
                                })
                            .Where(j => j.SiteOM == sesid)
                            .ToList();

                        ViewBag.Approvals = payroll;
                    }
                    if (sestype.Contains("OPE") && sestype.Contains("HEAD"))
                    {
                        var latestPayrolls = _context1.payroll
                                            .Where(p => p.Status == 2)
                                            .GroupBy(p => p.SiteId)
                                            .Select(g => g.OrderByDescending(p => p.AddedDate).FirstOrDefault())
                                            .ToList();

                        var payroll = latestPayrolls
                            .Join(
                                _context1.Sites,                                
                                p => p.SiteId,
                                s => s.SiteId,
                                (p, site) => new
                                {
                                    FileId = p.FileId,
                                    FileName = p.FileName,
                                    FileString = p.FileString,
                                    SiteId = p.SiteId,
                                    Sitename = site != null ? site.Sitename : null,
                                    SiteSOM = site != null ? site.SiteSOM : null,
                                    SiteOM = site != null ? site.SiteOM : null,
                                    SiteSCTK = site != null ? site.SiteSCTK : null,
                                    SiteName = p.SiteName,
                                    AddedBy = p.AddedBy,
                                    AddedDate = p.AddedDate,
                                    ApproveOM = p.ApproveOM,
                                    ApproveOMDate = p.ApproveOMDate,
                                    ApproveSOM = p.ApproveSOM,
                                    ApproveSOMDate = p.ApproveSOMDate,
                                    ApprovePO = p.ApprovePO,
                                    ApprovePODate = p.ApprovePODate,
                                    FinalizeBy = p.FinalizedBy,
                                    Release = p.Release,
                                    ApproveACC = p.ApproveACC,
                                    ApproveACCDate = p.ApproveACCDate,
                                    Status = p.Status
                                })
                            .Where(j => j.SiteSOM == sesid)
                            .ToList();

                        ViewBag.Approvals = payroll;
                    }
                    if (sestype.Contains("PAYROLL"))
                    {
                        var payroll = _context1.payroll
                                   .Where(c => c.Status == 3 || c.Status == 5)
                                   .ToList()
                                   .GroupBy(c => c.SiteId)
                                   .Select(g => g.OrderByDescending(c => c.AddedDate).First())
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                       c.Status
                                   }).ToList();

                        ViewBag.Approvals = payroll;
                    }
                    if (sestype.Contains("ACCTG") && sestype.Contains("STAFF"))
                    {
                        var payroll = _context1.payroll
                                   .Where(c => c.Status == 4)
                                   .ToList()
                                   .GroupBy(c => c.SiteId)
                                   .Select(g => g.OrderByDescending(c => c.AddedDate).First())
                                   .Select(c => new
                                   {
                                       c.FileId,
                                       c.FileName,
                                       c.FileString,
                                       c.SiteId,
                                       c.SiteName,
                                       c.AddedBy,
                                       c.AddedDate,
                                       c.Status
                                   }).ToList();

                        ViewBag.Approvals = payroll;
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
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
                ViewBag.Layout = "Payroll";
                ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

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
                              .Where(s => s.SiteId == siteId && s.Status != 0 )
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
        public IActionResult Decline(int siteId, string remarks)
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
                        payrollFile.Remarks = remarks;
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
                                .Where(a => a.Status == 6)
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
                        a.SiteId,
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

                            var sitefilestatus = _context1.Sites
                                                .FirstOrDefault(a => a.SiteId == file.SiteId);

                            if (payrollFile != null)
                            {
                                sitefilestatus.Payroll = 0;
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
        public IActionResult Totable()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }

                var newuser = _context1.Users
                    .Select(b => new
                    {
                        b.FirstName,
                        b.LastName
                    })
                    .ToList();

                var olduser = (from u in _context2.tbl_users
                               join c in _context2.tbl_contents on u.User_Type equals c.id into userContent
                               from uc in userContent.DefaultIfEmpty()
                               where u.User_Status == 1
                               select new
                               {
                                   u.id,
                                   u.Last_Name,
                                   u.First_Name,
                                   u.Middle_Name,
                                   u.Address,
                                   u.Position,
                                   u.User_Type,
                                   u.Username,
                                   u.Password,
                                   u.User_Status,
                                   u.Contact_Number,
                                   u.SiteId,
                                   Usertype = uc.Item_Details
                               })
              .ToList();

                if (!newuser.Any())
                {
                    // Return all users from a table since b is empty
                    var userlist = olduser.ToList();
                    ViewBag.Users = userlist;
                }
                else
                {
                    var userlist = olduser
                                .Where(a => !newuser.Any(b => b.FirstName == a.First_Name && b.LastName == a.Last_Name))
                                .Select(a => new
                                {
                                    a.id,
                                    First_Name = a.First_Name ?? "",
                                    Last_Name = a.Last_Name ?? "",
                                    Middle_Name = a.Middle_Name ?? "",
                                    Position = a.Position ?? "",
                                    Username = a.Username ?? "",
                                    Password = a.Password ?? "",
                                    Contact_Number = a.Contact_Number ?? "",
                                    SiteId = a.SiteId
                                })
                                .ToList();
                    ViewBag.Users = userlist;
                }

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult MigrateUser()
        {
            //try
            //{
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }
            var newuser = _context1.Users
                    .Select(b => new
                    {
                        b.FirstName,
                        b.LastName
                    })
                    .ToList();

            var olduser = (from u in _context2.tbl_users
                           join c in _context2.tbl_contents on u.User_Type equals c.id into userContent
                           from uc in userContent.DefaultIfEmpty()
                           where u.User_Status == 1
                           select new
                           {
                               u.id,
                               u.Last_Name,
                               u.First_Name,
                               u.Middle_Name,
                               u.Address,
                               u.Position,
                               u.User_Type,
                               u.Username,
                               u.Password,
                               u.User_Status,
                               u.Contact_Number,
                               u.SiteId,
                               Usertype = uc.Item_Details
                           })
              .ToList();

            var userlist = olduser
    .Where(a => !newuser.Any(b => b.FirstName == a.First_Name && b.LastName == a.Last_Name))
    .Select(a => new
    {
        a.id,
        First_Name = a.First_Name ?? "",
        Last_Name = a.Last_Name ?? "",
        Middle_Name = a.Middle_Name ?? "",
        Position = a.Position ?? "",
        Username = a.Username ?? "",
        Password = a.Password ?? "",
        Contact_Number = a.Contact_Number ?? "",
        SiteId = a.SiteId,
        Usertype = a.Usertype
    })
    .ToList();

            foreach (var um in userlist)
            {
                var trans = new Users
                {
                    LastName = um.Last_Name,
                    FirstName = um.First_Name,
                    MiddleName = um.Middle_Name,
                    Position = um.Position,
                    Username = um.Username,
                    Password = um.Password,
                    UserStatus = "Active",
                    UserType = um.Usertype,
                    ContactNo = um.Contact_Number,
                    SiteId = um.SiteId
                };

                _context1.Users.Add(trans);
            }

            _context1.SaveChanges();

            return RedirectToAction("Totable", "Payroll");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error in Forward action: " + ex.Message);
            //    return StatusCode(500);
            //}
        }
        public IActionResult Sites()
        {
            //try
            //{
            GetSession();
            if (HttpContext.Session.GetString(SessionType) == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "Payroll";
            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

            var site = _context2.tbl_contents
                        .Where(a => a.Status == 1 && a.Item_Type == "Site")
                        .Select(a => new
                        {
                            a.Item_Details,
                            a.Status,
                            a.WithOM,
                            a.SC_TK,
                            a.SOM
                        });
            var sitelist = site.ToList();

            foreach (var ns in sitelist)
            {
                var trans = new Sites
                {
                    Sitename = ns.Item_Details,
                    Status = "Active",
                    SiteSOM = ns.SOM,
                    SiteOM = ns.WithOM,
                    SiteSCTK = ns.SC_TK,

                };

                _context1.Sites.Add(trans);
            }

            _context1.SaveChanges();

            return View();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error in Forward action: " + ex.Message);
            //    return StatusCode(500);
            //}
        }
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
                                    pf.Status,
                                    pf.Remarks
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
        public IActionResult UserAccount()
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
                var acc = _context1.Users
                        .Where(a => a.UserStatus == "Pending")
                        .Select(a => new
                        {
                            a.UserId,
                            a.FirstName,
                            a.LastName,
                            a.Username,
                            a.Password,
                            a.Position
                        });

                ViewBag.Account = acc.ToList();

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public IActionResult UserApprove(int userId, string status)
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

                var approve = _context1.Users.FirstOrDefault(a => a.UserId == userId);

                if (approve != null)
                {
                    approve.UserStatus = status;
                };
                _context1.SaveChanges();
                return RedirectToAction("UserAccount", "Payroll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult AddPayroll()
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

                var sites = _context1.Sites
                            .Select(site => new
                            {
                                site.SiteId,
                                site.Sitename
                            });

                ViewBag.Sites = sites;
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UploadPayroll(FileModel pm, int siteId)
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
                    return PartialView("_AddPayrollPartial", pm);
                }

                var allowedExtensions = new[] { ".xls", ".xlsx", ".png", ".jpeg", ".jpg" };
                var extension = Path.GetExtension(pm.UploadFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("UploadFile", "Only .xls, .xlsx, .png, .jpeg, .jpg file types are allowed.");
                    return PartialView("_AddPayrollPartial", pm);
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
                    return PartialView("_AddPayrollPartial", pm);
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

                pm.FileName = siteresult.SiteId + Path.GetExtension(fileformat);
                var (filePath, rawfilename) = await _fileUploadPayroll.UploadFileAsync(pm.UploadFile, pm.FileName);
                                
                if (!string.IsNullOrEmpty(filePath))
                {
                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Uploaded successfully.";
                }
                else
                {
                    ModelState.AddModelError("UploadFile", "File upload failed.");
                    return PartialView("_AddPayrollPartial", pm);
                }

                return PartialView("_AddPayrollPartial", pm);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Upload action: " + ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }
        public async Task<IActionResult> DownloadPayroll(string fileName)
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

                var fileNames = await _fileUploadPayroll.GetFileNameAsync(fileName);

                // Check if any files were found
                if (fileNames == null || !fileNames.Any())
                {
                    return NotFound("No matching files found." + fileName);
                }

                // Handle a single file scenario (if multiple are not expected)
                var firstFileName = fileNames.FirstOrDefault();
                if (firstFileName != null)
                {
                    var fileResult = await _fileUploadPayroll.DownloadFileAsync(firstFileName);
                    return fileResult;
                }
                else
                {
                    return BadRequest("Error 69 please coordinate with IT");
                }
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpGet]
        public IActionResult AddPayrollPartial(int SiteId)
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
            return PartialView("_AddPayrollPartial", fm);
        }
        public IActionResult CheckPayroll()
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

                var sitequery = _context1.Sites
                            .Where(a => a.Status == "Active" && (a.SiteSOM == sesid || a.SiteSCTK == sesid || a.SiteOM == sesid) || a.Sitename == "Repair")
                            .GroupJoin(
                            _context1.payroll,
                            site => site.SiteId,
                            payroll => payroll.SiteId,
                            (site, pg) => new RequestSites
                            {
                                SiteId = site.SiteId,
                                Sitename = site.Sitename,
                                Status = site.Status,
                                SiteSOM = site.SiteSOM,
                                SiteOM = site.SiteOM,
                                SiteSCTK = site.SiteSCTK,
                                PayrollStatus = pg.Select(p => p.Status).FirstOrDefault() ?? 0
                            });

                ViewBag.Site = sitequery.ToList();
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult MigrateSites()
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionType) == null)
                {
                    return RedirectToAction("Login", "Home");
                }
            var prsitesSet = new HashSet<string>(_context1.Sites
                            .Where(a => a.Sitename != null)  
                            .Select(a => a.Sitename));

            var newsite = _context2.tbl_contents
                .Where(ps => ps.Status == 1 && ps.Item_Type == "Site" && !prsitesSet.Contains(ps.Item_Details))
                .Select(ps => new
                {
                    ps.Item_Details
                })
                .ToList();

            foreach (var um in newsite)
                {
                    var trans = new Sites
                    {
                        Sitename = um.Item_Details,
                        Status = "Active"
                    };
                    _context1.Sites.Add(trans);
                }

                _context1.SaveChanges();
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDtr(int SiteId)
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

                var file = await _context1.payroll
                            .Where(p => p.SiteId == SiteId && p.Status == 6)
                            .OrderByDescending(p => p.AddedDate)
                            .Select(a => new
                            {
                                a.FileId,
                                a.FileName,
                                a.SiteId
                            })
                            .FirstOrDefaultAsync(); 

                if (file != null)
                {
                    string fileToDelete = file.FileName;
                    bool fileDeleted = await _fileUploadService.DeleteFileAsync(fileToDelete);

                    if (fileDeleted)
                    {
                        var payrollFile = await _context1.payroll
                            .FirstOrDefaultAsync(p => p.FileName == file.FileName);

                        if (payrollFile != null)
                        {
                            var sitefilestatus = await _context1.Sites
                                .FirstOrDefaultAsync(a => a.SiteId == file.SiteId);

                            if (sitefilestatus != null)
                            {
                                sitefilestatus.Payroll = 0;
                            }

                            payrollFile.Status = 0;

                            await _context1.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete file '{file.FileName}'.");
                        TempData["ErrorMessage"] = "Failed to delete the file.";
                        return RedirectToAction("DashPayroll", "Payroll");
                    }
                }

                var site = await _context1.Sites
                    .Where(a => a.SiteId == SiteId)
                    .Select(a => new SiteList
                    {
                        SiteId = a.SiteId,
                        Sitename = a.Sitename
                    })
                    .FirstOrDefaultAsync();

                if (site == null)
                {
                    return NotFound();
                }

                return PartialView("_DeleteDtrPartial", site);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in DeleteDtr action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpGet]
        public IActionResult DeleteDtrPartial(int siteid)
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

                var sitequery = _context1.Sites
                                .Where(a => a.SiteId == siteid)
                                .Select(a => new 
                                {
                                    a.SiteId,
                                    a.Sitename   
                                }).FirstOrDefault();

                if (sitequery == null)
                {
                    return NotFound();
                }

                var site = new SiteList
                {
                    SiteId = sitequery.SiteId,
                    Sitename = sitequery.Sitename
                };

                return PartialView("_DeletedtrPartial", site);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public async Task<IActionResult> PRpreview(string fileName)
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

                var prquery = _context1.payroll
                            .Where(a => a.FileName == fileName)
                            .Select(a => new
                            {
                                a.FileName
                            });

                var prresult = prquery.FirstOrDefault();

                var fileNames = await _fileUploadPayroll.GetFileNameAsync(fileName);
                var fm = new FileModel();
                if (prresult == null)
                {
                    fm = new FileModel
                    {
                        FileName = fileNames.FirstOrDefault()
                    };
                }
                else
                {
                    fm = new FileModel
                    {
                        FileName = prresult?.FileName.ToString()
                    };
                }
                return PartialView("_PRpreview", fm);
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