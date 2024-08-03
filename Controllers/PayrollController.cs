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
        public PayrollController(IConfiguration configuration, PCGContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll)
        {
            _configuration = configuration;
            con1 = new SqlConnection(_configuration.GetConnectionString("pcgConnection"));
            con2 = new SqlConnection(_configuration.GetConnectionString("portestConnection"));
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

            var sitequery = from u in _context2.tbl_users
                            join c in _context2.tbl_contents
                            on u.id equals c.WithOM into userContents
                            from c in userContents.DefaultIfEmpty()
                            where c != null
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

                DateTime dt = DateTime.Now;
                string fdt = dt.ToString("dd-MM-yyyy HH:mm:ss");
                string filedir = filePath;
                string fname = rawfilename;
                string sitename = StringEdit.RightStr(siteresult.SiteName);
                if (con1.State == ConnectionState.Closed)
                {
                    con1.Open();
                }
                using (cmd = new SqlCommand("INSERT INTO PayrollFile (FileName, FileString, Site, Addedby, AddedDate) " +
                                            "VALUES (@FileName, @FileString, @Site, @Addedby, @AddedDate)", con1))
                {
                    cmd.Parameters.AddWithValue("@FileName", fname);
                    cmd.Parameters.AddWithValue("@FileString", filedir);
                    cmd.Parameters.AddWithValue("@Site", sitename);
                    cmd.Parameters.AddWithValue("@AddedBy", $"{siteresult.First_Name} {siteresult.Last_Name}");
                    cmd.Parameters.AddWithValue("@AddedDate", fdt);
                }
                cmd.ExecuteNonQuery();
                if (con1.State == ConnectionState.Open)
                {
                    con1.Close();
                }

                if (!string.IsNullOrEmpty(filePath))
                {                    
                    /*
                        var newContent = new PayrollFile
                        {
                            FileName = pm.FileName,
                            FileString = filedir,
                            Site = siteresult.SiteName,
                            AddedBy = $"{siteresult.First_Name} {siteresult.Last_Name}",
                            AddedDate = fdt,

                            ApproveOM = null,
                            ApproveOMDate = null,
                            ApproveSOM = null,
                            ApproveSOMDate = null,
                            ApproveIM = null,
                            ApproveIMDate = null,
                            ApproveACC = null,
                            ApproveACCDate = null,
                            Release = null
                        };
                    
                    _context1.PayrollFile.Add(newContent);
                    await _context1.SaveChangesAsync();
                    */
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
