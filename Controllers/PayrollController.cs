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

        public SqlConnection con;
        public SqlCommand cmd;
        private readonly IConfiguration _configuration;
        private readonly FileUploadService _fileUploadPayroll;
        public PayrollController(IConfiguration configuration, PCGContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll)
        {
            _configuration = configuration;
            con = new SqlConnection(_configuration.GetConnectionString("portestConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
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
            ViewBag.SessionType = HttpContext.Session.GetString(SessionType);

            var sitequery = _context1.site
                            .Select(s => new
                             {
                                 s.site_id,
                                 s.site_name,
                                 s.client_name,
                                 s.om,
                                 s.status
                             })
                             .ToList();

            ViewBag.Site = sitequery;

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(FileModel pm, int SiteId)
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

                var site = _context1.site
                    .Where(s => s.site_id == SiteId)
                    .Select(s => new
                    {
                        s.site_id,
                        s.site_name,
                        s.client_name,
                        s.om,
                        s.status
                    })
                    .FirstOrDefault();

                if (site == null)
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

                pm.FileName = "_" + site.site_id + "_" + ViewBag.SessionId + Path.GetExtension(fileformat);
                var filePath = await _fileUploadPayroll.UploadFileAsync(pm.UploadFile, pm.FileName);

                if (!string.IsNullOrEmpty(filePath))
                {
                    ModelState.Clear();
                    ViewBag.SuccessMessage = "Uploaded successfully.";
                }
                else
                {
                    ModelState.AddModelError("UploadFile", "File upload failed.");
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

            var sitequery = _context1.site
                            .Where(s => s.site_id == SiteId)
                            .Select(s => new
                            {
                                s.site_id,
                                s.site_name,
                                s.client_name,
                                s.om,
                                s.status
                            })
                            .ToList();
            var siteresult = sitequery.FirstOrDefault();

            var fm = new FileModel
            {
                SiteId = siteresult?.site_id.ToString()
            };

            return PartialView("_UploadPartial", fm);
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
