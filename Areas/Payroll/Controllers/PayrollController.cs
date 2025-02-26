using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NHSP.Areas.Payroll.Models;
using NHSP.Models;
using NHSP.Payroll.Formula;
using System.Data;
using System.Globalization;

namespace NHSP.Areas.Payroll.Controllers
{
    [Area("Payroll")]
    public class PayrollController : Controller
    {
        private readonly NHSPContext _context1;
        private readonly DatabaseContext _context2;
        private readonly HttpClient _httpClient;
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
        public PayrollController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment, NHSPContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll, HttpClient httpClient)
        {
            _configuration = configuration;
            con1 = new SqlConnection(_configuration.GetConnectionString("nhspConnection"));
            con2 = new SqlConnection(_configuration.GetConnectionString("pettycashConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = new FileUploadService(Path.Combine(_hostingEnvironment.WebRootPath, "PayrollFiles"));
            _httpClient = httpClient;
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
        public IActionResult SiteDTR()
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionId) == null)
            {
                return RedirectToAction("Login", "Home", new { area = (string)null });
            }
            int sesid = int.Parse(HttpContext.Session.GetString(SessionId));

            var sitequery = _context1.Sites
                            .Where(s => s.SiteSCTK == sesid || s.SiteOM == sesid || s.SiteSOM == sesid)
                            .GroupJoin(
                                _context1.payroll.Where(p => p.Status == 1),
                                site => site.SiteId,
                                payroll => payroll.SiteId,
                                (site, payrollGroup) => new
                                {
                                    Site = site,
                                    LatestDTR = payrollGroup
                                        .OrderByDescending(p => p.AddedDate)
                                        .FirstOrDefault()
                                }
                            )
                            .Select(e => new
                            {
                                e.Site.SiteId,
                                e.Site.Sitename,
                                e.Site.Status,
                                e.Site.SiteSOM,
                                e.Site.SiteOM,
                                e.Site.SiteSCTK,
                                e.Site.Payroll,
                                AddedDate = e.LatestDTR != null ? e.LatestDTR.AddedDate : (DateTime?)null,
                                FileId = e.LatestDTR != null ? e.LatestDTR.FileId : (int?)null,
                                ApproveOMDate = e.LatestDTR != null ? e.LatestDTR.ApproveOMDate : (DateTime?)null,
                                ApproveSOMDate = e.LatestDTR != null ? e.LatestDTR.ApproveSOMDate : (DateTime?)null,
                                ApprovePODate = e.LatestDTR != null ? e.LatestDTR.ApprovePODate : (DateTime?)null,
                                ApproveACCDate = e.LatestDTR != null ? e.LatestDTR.ApproveACCDate : (DateTime?)null,
                                Release = e.LatestDTR != null ? e.LatestDTR.Release : (DateTime?)null,
                                DTRstatus = e.LatestDTR != null ? e.LatestDTR.Status : (int?)null,
                            })
                            .ToList();

            ViewBag.Site = sitequery;

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
                if (siteassign != null)
                {
                    siteassign.SiteSCTK = int.Parse(HttpContext.Session.GetString(SessionId));
                    _context1.SaveChanges();
                };
            }

            return RedirectToAction("SiteDTR", "Payroll");
        }
        [HttpPost]
        public async Task<IActionResult> Uploaddtr(FileModel pm, int siteId)
        {
            try
            {
                GetSession();

                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                var model = _context1.Sites
                            .Where(a => a.SiteId == siteId)
                            .Select(a => new
                            {
                                SiteId = a.SiteId,
                                a.Sitename,
                                a.Status,
                                a.SiteSOM,
                                a.SiteOM,
                                a.SiteSCTK
                            });

                var modelresult = model.FirstOrDefault();

                var fm = new FileModel
                {
                    SiteId = modelresult.SiteId,
                    SiteName = modelresult?.Sitename.ToString()
                };

                if (pm.UploadFile == null || !pm.UploadFile.Any())
                {
                    ModelState.AddModelError("UploadFile", "Please select file to upload.");
                    return PartialView("_UploaddtrPartial", fm);
                }

                var allowedExtensions = new[] { ".xls", ".xlsx", ".png", ".jpeg", ".jpg" };

                var siteresult = _context1.Sites
                                .Where(a => a.SiteId == siteId)
                                .Select(a => new { a.SiteId, a.Sitename })
                                .FirstOrDefault();

                if (siteresult == null)
                {
                    ModelState.AddModelError("SiteId", "Invalid site ID.");
                    return PartialView("_UploaddtrPartial", fm);
                }

                List<string> uploadedFiles = new List<string>();
                int inc = 1;
                foreach (var file in pm.UploadFile)
                {
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("UploadFile", $"Invalid file type: {file.FileName}. Only .xls, .xlsx, .png, .jpeg, .jpg allowed.");
                        return PartialView("_UploaddtrPartial", fm);
                    }

                    string fileformat = (extension == ".xlsx") ? Path.ChangeExtension(file.FileName, ".xls") : file.FileName;
                    string fileName = $"{siteresult.SiteId}_{ViewBag.SessionId}_{DateTime.Now:MMddyy}_{inc}{Path.GetExtension(fileformat)}";

                    var (filePath, rawfilename) = await _fileUploadPayroll.UploadFileAsync(file, fileName);

                    if (string.IsNullOrEmpty(filePath))
                    {
                        ModelState.AddModelError("UploadFile", $"File upload failed: {file.FileName}");
                        return PartialView("_UploaddtrPartial", fm);
                    }

                    _context1.payroll.Add(new payroll
                    {
                        FileName = rawfilename,
                        FileString = filePath,
                        FileType = "DTR",
                        SiteId = siteresult.SiteId,
                        SiteName = StringEdit.RightStr(siteresult.Sitename),
                        AddedBy = int.Parse(HttpContext.Session.GetString(SessionId)),
                        AddedDate = DateTime.Now,
                        Status = 1
                    });

                    uploadedFiles.Add(fileName);
                    inc++;
                }

                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                var bypass = _context1.Users
                    .Where(u => u.UserId == sesid)
                    .Select(u => new
                    {
                        u.Bypass
                    })
                    .FirstOrDefault();
                if (bypass != null && bypass.Bypass == 1)
                {
                    _context1.Sites.Where(s => s.SiteId == siteId).ToList().ForEach(site => site.Payroll = 2);
                }
                else
                {
                    _context1.Sites.Where(s => s.SiteId == siteId).ToList().ForEach(site => site.Payroll = 1);
                }   

                _context1.SaveChanges();

                if (uploadedFiles.Count > 0)
                {
                    ModelState.Clear();
                    ViewBag.SuccessMessage = $"{uploadedFiles.Count} file(s) uploaded successfully.";
                }

                return PartialView("_UploaddtrPartial", fm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Upload action: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public IActionResult UploaddtrParital(int SiteId)
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
                SiteId = siteresult.SiteId,
                SiteName = siteresult?.Sitename.ToString()
            };
            return PartialView("_UploaddtrPartial", fm);
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
                        var site = _context1.Sites
                                    .Where(a => a.SiteOM == sesid && a.Status == "Active" && a.Payroll == 1)
                                    .Select(a => new
                                    {
                                        a.SiteId,
                                        a.Sitename,
                                        a.SiteSOM,
                                        a.SiteOM,
                                        a.SiteSCTK,
                                        a.Payroll
                                    }).ToList();

                        ViewBag.Approvals = site;
                    }
                    if (sestype.Contains("OPE") && sestype.Contains("HEAD"))
                    {
                        var site = _context1.Sites
                                    .Where(a => a.SiteSOM == sesid && a.Status == "Active" && a.Payroll == 2)
                                    .Select(a => new
                                    {
                                        a.SiteId,
                                        a.Sitename,
                                        a.SiteSOM,
                                        a.SiteOM,
                                        a.SiteSCTK,
                                        a.Payroll
                                    }).ToList();

                        ViewBag.Approvals = site;
                    }
                    if (sestype.Contains("PAYROLL"))
                    {
                        var site = _context1.Sites
                                    .Where(a => a.Status == "Active" && a.Payroll == 3)
                                    .Select(a => new
                                    {
                                        a.SiteId,
                                        a.Sitename,
                                        a.SiteSOM,
                                        a.SiteOM,
                                        a.SiteSCTK,
                                        a.Payroll
                                    }).ToList();

                        ViewBag.Approvals = site;
                    }
                    if (sestype.Contains("ACCTG") && sestype.Contains("STAFF"))
                    {
                        var site = _context1.Sites
                                    .Where(a => a.Status == "Active" && a.Payroll == 4)
                                    .Select(a => new
                                    {
                                        a.SiteId,
                                        a.Sitename,
                                        a.SiteSOM,
                                        a.SiteOM,
                                        a.SiteSCTK,
                                        a.Payroll
                                    }).ToList();

                        ViewBag.Approvals = site;
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
        public async Task<IActionResult> Approve(int siteId)
        {
            try
            {
                GetSession();
                if (HttpContext.Session.GetString(SessionId) == null)
                {
                    return RedirectToAction("Login", "Home", new { area = (string)null });
                }
                int sesid = int.Parse(HttpContext.Session.GetString(SessionId));
                var bypass = _context1.Users
                    .Where(u => u.UserId == sesid)
                    .Select(u => new
                    {
                        u.Bypass
                    })
                    .FirstOrDefault();                
                DateTime? fdt = DateTime.ParseExact(DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss"), "MM-dd-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                var payrollstatus = _context1.Sites
                              .Where(s => s.SiteId == siteId && s.Status == "Active")
                              .Select(s => new
                              {
                                  s.Payroll,
                                  s.Status
                              }).FirstOrDefault();
                if (payrollstatus != null)
                {
                    if (payrollstatus.Payroll == 1)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 1)
                                       .ToList();
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveOM = sesid;
                            payrollFile.ApproveOMDate = fdt;
                            payrollFile.Status = 1;
                        }

                        var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                        if (site != null)
                        {
                            site.Payroll = 2;
                        }
                        _context1.SaveChanges();

                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (payrollstatus.Payroll == 2)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 1)
                                       .ToList();
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApproveSOM = sesid;
                            payrollFile.ApproveSOMDate = fdt;
                            payrollFile.Status = 1;
                        }

                        var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                        if (site != null)
                        {
                            site.Payroll = 3;
                        }
                        _context1.SaveChanges();

                        return RedirectToAction("ViewPayroll", "Payroll");
                    }
                    if (payrollstatus.Payroll == 3)
                    {
                        var payrollFiles = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 1)
                                       .ToList();
                        foreach (var payrollFile in payrollFiles)
                        {
                            payrollFile.ApprovePO = sesid;
                            payrollFile.ApprovePODate = fdt;
                            payrollFile.Status = 1;
                        }

                        //var payroll = _context1.payroll
                        //               .Where(pf => pf.SiteId == siteId && pf.Status == 4 && pf.FileType == "Payroll")
                        //               .ToList();
                        //foreach (var payrollFile in payroll)
                        //{
                        //    payrollFile.Status = 3;
                        //}

                        var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                        if (site != null)
                        {
                            site.Payroll = 4;
                        }
                        _context1.SaveChanges();

                        //var files = await _context1.payroll
                        //    .Where(p => p.SiteId == siteId && p.Status == 1 && p.FileType == "DTR")
                        //    .OrderByDescending(p => p.AddedDate)
                        //    .Select(a => new
                        //    {
                        //        a.FileId,
                        //        a.FileName,
                        //        a.SiteId
                        //    })
                        //    .ToListAsync();

                        //if (files.Any())
                        //{
                        //    foreach (var item in files)
                        //    {
                        //        bool fileDeleted = await _fileUploadService.DeleteFileAsync(item.FileName);

                        //        if (fileDeleted)
                        //        {
                        //            var payrollFiles = await _context1.payroll
                        //                .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId)
                        //                .ToListAsync();

                        //            if (payrollFiles.Any())
                        //            {
                        //                foreach (var payrollFile in payrollFiles)
                        //                {
                        //                    payrollFile.Status = 0;
                        //                    _context1.payroll.Update(payrollFile);
                        //                }
                        //            }
                        //            var sitefilestatus = await _context1.Sites
                        //                .FirstOrDefaultAsync(a => a.SiteId == item.SiteId);

                        //            if (sitefilestatus != null)
                        //            {
                        //                sitefilestatus.Payroll = 4;
                        //                _context1.Sites.Update(sitefilestatus);
                        //            }
                        //            await _context1.SaveChangesAsync();

                        //        }
                        //        else
                        //        {
                        //            Console.WriteLine($"Failed to delete file '{item.FileName}'.");
                        //            TempData["ErrorMessage"] = "Failed to delete the file.";
                        //            return RedirectToAction("DashPayroll", "Payroll");
                        //        }
                        //    }
                        //}

                        //var prfiles = await _context1.payroll
                        //           .Where(p => p.SiteId == siteId && p.Status == 4 && p.FileType == "Payroll")
                        //           .OrderByDescending(p => p.AddedDate)
                        //           .Select(a => new
                        //           {
                        //               a.FileId,
                        //               a.FileName,
                        //               a.SiteId
                        //           })
                        //           .ToListAsync();

                        //if (prfiles.Any())
                        //{
                        //    foreach (var item in prfiles)
                        //    {
                        //        var payrollFiles = await _context1.payroll
                        //            .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId && p.Status == 4)
                        //            .ToListAsync();

                        //        if (payrollFiles.Any())
                        //        {
                        //            foreach (var payrollFile in payrollFiles)
                        //            {
                        //                payrollFile.Status = 3;
                        //                _context1.payroll.Update(payrollFile);
                        //            }
                        //        }
                        //        await _context1.SaveChangesAsync();
                        //    }
                        //}
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
        public async Task<IActionResult> Decline(int siteId, string remarks)
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
                                   .Where(pf => pf.SiteId == siteId && pf.Status == 1 && pf.FileType == "DTR");
                    foreach (var payrollFile in payrollFiles)
                    {                        
                        payrollFile.Remarks = remarks;
                    }

                    var prstatus = _context1.Sites
                                   .Where(pf => pf.SiteId == siteId);
                    foreach (var pr in prstatus)
                    {
                        pr.Payroll = 4;
                    }
                    _context1.SaveChanges();

                }
                var files = await _context1.payroll
                            .Where(p => p.SiteId == siteId && p.Status == 1 && p.FileType == "DTR")
                            .OrderByDescending(p => p.AddedDate)
                            .Select(a => new
                            {
                                a.FileId,
                                a.FileName,
                                a.SiteId
                            })
                            .ToListAsync();

                if (files.Any())
                {
                    foreach (var item in files)
                    {
                        bool fileDeleted = await _fileUploadService.DeleteFileAsync(item.FileName);

                        if (fileDeleted)
                        {
                            var payrollFiles = await _context1.payroll
                                .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId)
                                .ToListAsync();

                            if (payrollFiles.Any())
                            {
                                foreach (var payrollFile in payrollFiles)
                                {
                                    payrollFile.Status = 0;
                                    _context1.payroll.Update(payrollFile);
                                }
                            }
                            var sitefilestatus = await _context1.Sites
                                .FirstOrDefaultAsync(a => a.SiteId == item.SiteId);

                            if (sitefilestatus != null)
                            {
                                sitefilestatus.Payroll = 0;
                                _context1.Sites.Update(sitefilestatus);
                            }
                            await _context1.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Failed to delete file '{item.FileName}'.");
                            TempData["ErrorMessage"] = "Failed to delete the file.";
                            return RedirectToAction("DashPayroll", "Payroll");
                        }
                    }
                }

                return RedirectToAction("ViewPayroll", "Payroll");
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
                    Bypass = um.SiteId
                };

                _context1.Users.Add(trans);
            }

            _context1.SaveChanges();

            return RedirectToAction("Totable", "Payroll");
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

                var payroll = _context1.Sites
                            .GroupJoin(
                                _context1.payroll.Where(p => p.FileType == "Payroll" && (p.Status == 2 || p.Status == 3 || p.Status == 4)),
                                s => s.SiteId,
                                p => p.SiteId,
                                (s, payrollGroup1) => new { s, payrollGroup1 }
                            )
                            .SelectMany(
                                x => x.payrollGroup1.DefaultIfEmpty(),
                                (x, p1) => new { x.s, p1 }
                            )
                            .GroupJoin(
                                _context1.payroll.Where(p => p.FileType == "DTR" && p.Status == 1),
                                sp => sp.s.SiteId,
                                p => p.SiteId,
                                (sp, payrollGroup2) => new
                                {
                                    sp.s,
                                    sp.p1,
                                    p2 = payrollGroup2.OrderBy(p => p.AddedDate).FirstOrDefault()
                                }
                            )
                            .Select(x => new
                            {
                                x.s.SiteId,
                                x.s.Sitename,
                                x.s.Status,
                                x.s.SiteSOM,
                                x.s.SiteOM,
                                x.s.SiteSCTK,
                                x.s.Payroll,

                                FileName = x.p1 != null ? x.p1.FileName : null,
                                FileString = x.p1 != null ? x.p1.FileString : null,
                                FileType = x.p1 != null ? x.p1.FileType : null,
                                FileStatus = x.p1 != null ? x.p1.Status : null,

                                FileAddedDate = x.p2 != null ? x.p2.AddedDate : null,
                                FileApproveOMDate = x.p2 != null ? x.p2.ApproveOMDate : null,
                                FileApproveSOMDate = x.p2 != null ? x.p2.ApproveSOMDate : null,
                                FileApprovePODate = x.p2 != null ? x.p2.ApprovePODate : null
                            })
                            .ToList();
                ViewBag.payroll = payroll;
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
                    return RedirectToAction("Login", "Home");
                }
                ModelState.Clear();

                var files = Request.Form.Files;
                if (files == null || files.Count == 0)
                {
                    ModelState.AddModelError("UploadFile", "Please select file to upload.");
                    return PartialView("_AddPayrollPartial", pm);
                }

                var allowedExtensions = new[] { ".png", ".jpeg", ".jpg" };

                var siteresult = _context1.Sites
                                .Where(a => a.SiteId == siteId)
                                .Select(a => new { a.SiteId, a.Sitename })
                                .FirstOrDefault();

                if (siteresult == null)
                {
                    ModelState.AddModelError("SiteId", "Invalid site ID.");
                    return PartialView("_AddPayrollPartial", pm);
                }

                List<string> uploadedFiles = new List<string>();
                int inc = 1;
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("UploadFile", $"Invalid file type: {file.FileName}. Only .png, .jpeg, .jpg allowed.");
                        return PartialView("_AddPayrollPartial", pm);
                    }

                    string fileformat = (extension == ".xlsx") ? Path.ChangeExtension(file.FileName, ".xls") : file.FileName;
                    string fileName = $"{siteresult.SiteId}{Path.GetExtension(fileformat)}";

                    var (filePath, rawfilename) = await _fileUploadPayroll.UploadFileAsync(file, fileName);

                    if (string.IsNullOrEmpty(filePath))
                    {
                        ModelState.AddModelError("UploadFile", $"File upload failed: {file.FileName}");
                        return PartialView("_AddPayrollPartial", pm);
                    }

                    _context1.payroll.Add(new payroll
                    {
                        FileName = rawfilename,
                        FileString = filePath,
                        FileType = "Payroll",
                        SiteId = siteresult.SiteId,
                        SiteName = StringEdit.RightStr(siteresult.Sitename),
                        AddedBy = int.Parse(HttpContext.Session.GetString(SessionId)),
                        AddedDate = DateTime.Now,
                        Status = 2
                    });

                    uploadedFiles.Add(fileName);
                    inc++;
                }
                var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if (site != null)
                {
                    site.Payroll = 4;
                }
                _context1.SaveChanges();

                if (uploadedFiles.Count > 0)
                {
                    ModelState.Clear();
                    ViewBag.SuccessMessage = $"{uploadedFiles.Count} file(s) uploaded successfully.";
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
                                a.SiteId,
                                a.Sitename,
                                a.Status,
                                a.SiteSOM,
                                a.SiteOM,
                                a.SiteSCTK
                            });

            var siteresult = sitequery.FirstOrDefault();

            var fm = new FileModel
            {
                SiteId = siteresult.SiteId,
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

                var payroll = _context1.Sites
                            .Where(s => s.SiteSCTK == sesid || s.SiteOM == sesid || s.SiteSOM == sesid)
                            .GroupJoin(
                                _context1.payroll.Where(p => p.FileType == "Payroll" && p.Status == 2 || p.Status == 3 || p.Status == 4),
                                s => s.SiteId,
                                p => p.SiteId,
                                (s, payrollGroup) => new { s, payrollGroup }
                            )
                            .SelectMany(
                                x => x.payrollGroup.DefaultIfEmpty(),
                                (x, p) => new
                                {
                                    x.s.SiteId,
                                    x.s.Sitename,
                                    x.s.Status,
                                    x.s.SiteSOM,
                                    x.s.SiteOM,
                                    x.s.SiteSCTK,
                                    x.s.Payroll,
                                    FileName = p != null ? p.FileName : null,
                                    FileString = p != null ? p.FileString : null,
                                    FileType = p != null ? p.FileType : null,
                                    FileStatus = p != null ? p.Status : null
                                }
                            )
                            .ToList();

                ViewBag.payroll = payroll;
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

                var files = await _context1.payroll
                            .Where(p => p.SiteId == SiteId && p.Status == 1 && p.FileType == "DTR")
                            .OrderByDescending(p => p.AddedDate)
                            .Select(a => new
                            {
                                a.FileId,
                                a.FileName,
                                a.SiteId
                            })
                            .ToListAsync();

                if (files.Any())
                {
                    foreach (var item in files)
                    {
                        bool fileDeleted = await _fileUploadService.DeleteFileAsync(item.FileName);

                        if (fileDeleted)
                        {
                            var payrollFiles = await _context1.payroll
                                .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId)
                                .ToListAsync();

                            if (payrollFiles.Any())
                            {
                                foreach (var payrollFile in payrollFiles)
                                {
                                    payrollFile.Status = 0;
                                    _context1.payroll.Update(payrollFile);
                                }
                            }
                            var sitefilestatus = await _context1.Sites
                                .FirstOrDefaultAsync(a => a.SiteId == item.SiteId);

                            if (sitefilestatus != null)
                            {
                                sitefilestatus.Payroll = 0;
                                _context1.Sites.Update(sitefilestatus);
                            }
                            await _context1.SaveChangesAsync();

                        }
                        else
                        {
                            Console.WriteLine($"Failed to delete file '{item.FileName}'.");
                            TempData["ErrorMessage"] = "Failed to delete the file.";
                            return RedirectToAction("DashPayroll", "Payroll");
                        }
                    }
                }

                var prfiles = await _context1.payroll
                           .Where(p => p.SiteId == SiteId && p.Status == 4 && p.FileType == "Payroll")
                           .OrderByDescending(p => p.AddedDate)
                           .Select(a => new
                           {
                               a.FileId,
                               a.FileName,
                               a.SiteId
                           })
                           .ToListAsync();

                if (prfiles.Any())
                {
                    foreach (var item in prfiles)
                    {
                        var payrollFiles = await _context1.payroll
                            .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId && p.Status == 4)
                            .ToListAsync();

                        if (payrollFiles.Any())
                        {
                            foreach (var payrollFile in payrollFiles)
                            {
                                payrollFile.Status = 3;
                                _context1.payroll.Update(payrollFile);
                            }
                        }
                        await _context1.SaveChangesAsync();
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
        public async Task<IActionResult> PRpreview(int siteId, string fileType)
        {
            GetSession();
            if (HttpContext.Session.GetString(SessionType) == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "Payroll";
            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

            var dtrquery = _context1.payroll
                        .Where(a => a.SiteId == siteId && a.Status == 1 && a.FileType == "DTR")
                        .Select(a => new PreviewModel
                        {
                            FileName = a.FileName,
                            SiteId = a.SiteId
                        }).ToList();

            var prquery = _context1.payroll
                        .Where(a => a.SiteId == siteId && (a.Status == 2 || a.Status == 3 || a.Status == 4) && a.FileType == "Payroll")
                        .Select(a => new PreviewModel
                        {
                            FileName = a.FileName,
                            SiteId = a.SiteId
                        }).ToList();

            if (fileType == "DTR")
            {
                ViewBag.prev = dtrquery;
            }
            if (fileType == "Payroll")
            {
                ViewBag.prev = prquery;
            }

            return PartialView("_PRpreview");
        }
        public IActionResult Approvepr(int siteId)
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

                var payroll = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 2 && pf.FileType == "Payroll")
                                       .ToList();
                foreach (var payrollFile in payroll)
                {
                    payrollFile.Status = 3;
                }

                var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if (site != null)
                {
                    site.Payroll = 5;
                }

                _context1.SaveChanges();
                return RedirectToAction("CheckPayroll", "Payroll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public async Task<IActionResult> Deletepr(int SiteId)
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

                var files = await _context1.payroll
                            .Where(p => p.SiteId == SiteId && (p.Status == 3 || p.Status == 4) && p.FileType == "Payroll")
                            .OrderByDescending(p => p.AddedDate)
                            .Select(a => new
                            {
                                a.FileId,
                                a.FileName,
                                a.SiteId
                            })
                            .ToListAsync();

                if (files.Any())
                {
                    foreach (var item in files)
                    {
                        bool fileDeleted = await _fileUploadService.DeleteFileAsync(item.FileName);

                        if (fileDeleted)
                        {
                            var payrollFiles = await _context1.payroll
                                .Where(p => p.FileName == item.FileName && p.SiteId == item.SiteId && (p.Status == 3 || p.Status == 4))
                                .ToListAsync();

                            if (payrollFiles.Any())
                            {
                                foreach (var payrollFile in payrollFiles)
                                {
                                    payrollFile.Status = 0;
                                    _context1.payroll.Update(payrollFile);
                                }

                            }
                            await _context1.SaveChangesAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Failed to delete file '{item.FileName}'.");
                            TempData["ErrorMessage"] = "Failed to delete the file.";
                            return RedirectToAction("DashPayroll", "Payroll");
                        }
                    }
                    var site = _context1.Sites.FirstOrDefault(s => s.SiteId == SiteId);
                    if (site != null)
                    {
                        site.Payroll = 4;
                    }
                }
                return RedirectToAction("Addpayroll", "Payroll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult Declinepr(int siteId)
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


                var payroll = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 2 && pf.FileType == "Payroll")
                                       .ToList();
                foreach (var payrollFile in payroll)
                {
                    payrollFile.Status = 4;
                }
                var dtr = _context1.payroll
                                       .Where(pf => pf.SiteId == siteId && pf.Status == 1 && pf.FileType == "DTR")
                                       .ToList();
                foreach (var dtrfile in dtr)
                {
                    dtrfile.ApprovePODate = fdt;
                }

                var site = _context1.Sites.FirstOrDefault(s => s.SiteId == siteId);
                if (site != null)
                {
                    site.Payroll = 4;
                }
                _context1.SaveChanges();
                return RedirectToAction("CheckPayroll", "Payroll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult UserManage()
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

                var user = _context1.Users
                    .Where(a => a.UserStatus == "Active" && a.Position.Contains("site coor") || a.Position.Contains("time keep"))
                    .Select(a => new
                    {
                        a.UserId,
                        a.FirstName,
                        a.LastName,
                        a.Username,
                        a.Password,
                        a.Position,
                        a.UserStatus,
                        a.Bypass
                    });

                ViewBag.Userlist = user.ToList();

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        public IActionResult BypassOMapprove(int UserId)
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

                var user = _context1.Users.FirstOrDefault(s => s.UserId == UserId);
                if (user != null && user.Bypass == null || user.Bypass == 0)
                {
                    user.Bypass = 1;
                }
                else
                {
                   user.Bypass = 0;
                }
                _context1.SaveChanges();

                return RedirectToAction("UserManage", "Payroll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Forward action: " + ex.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public IActionResult ReviseDtr()
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