using Microsoft.AspNetCore.Mvc;
using NHSP.Models;
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
using NHSP.Payroll.Formula;
using NHSP.Areas.Payroll.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace NHSP.Controllers
{
    public class HomeController : Controller
    {
        private readonly NHSPContext _context1;
        private readonly DatabaseContext _context2;
        const string SessionName = "_Name";
        const string SessionLayout = "_Layout";
        const string SessionType = "_Type";
        const string SessionId = "_Id";

        public SqlConnection con;
        public SqlCommand cmd;
        private readonly IConfiguration _configuration;
        private readonly FileUploadService _fileUploadPayroll;
        private readonly FileUploadService _fileUploadService;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment, NHSPContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll)
        {
            _configuration = configuration;
            con = new SqlConnection(_configuration.GetConnectionString("pettycashConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = new FileUploadService(Path.Combine(_hostingEnvironment.WebRootPath, "Files"));
        }
        public void GetSession() 
        {
            ViewBag.Id = HttpContext.Session.GetString(SessionId);
            ViewBag.Name = HttpContext.Session.GetString(SessionName);
            ViewBag.Layout = HttpContext.Session.GetString(SessionLayout);
            ViewBag.Type = HttpContext.Session.GetString(SessionType);
        }
            
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Selection(LoginModel m)
        {
            if (ModelState.IsValid)
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                var query2 = _context1.Users
                            .Where(a => a.Username == m.Username && a.Password == m.Password)
                            .Select(a => new
                            {
                                a.UserId,
                                a.FirstName,
                                a.Username,
                                a.Password,
                                a.Position,
                                a.UserType
                            });

                var result2 = query2.FirstOrDefault();

                if (result2 == null)
                {
                    var query1 = from u in _context2.tbl_users
                                 join c in _context2.tbl_contents on u.User_Type equals c.id into uc
                                 from c in uc.DefaultIfEmpty()
                                 where u.Username == m.Username && u.Password == m.Password
                                 select new
                                 {
                                     u.id,
                                     u.First_Name,
                                     u.Username,
                                     u.Password,
                                     u.Position,
                                     c.Code
                                 };
                    var result1 = query1.FirstOrDefault();

                    string position = StringEdit.NoSpaceUpper(result1.Position);
                    HttpContext.Session.SetString(SessionType, position);
                    HttpContext.Session.SetString(SessionId, result1.id.ToString());
                    HttpContext.Session.SetString(SessionName, result1.First_Name.ToString());
                    var usermodel = new UsersModel
                    {
                        UserName = result1.Username,
                        Password = result1.Password,
                        Position = position,
                        Code = result1.Code,
                        Id = result1.id.ToString()
                    };
                    return PartialView("_Selection", usermodel);
                }
                if (result2 != null)
                {
                    string position = StringEdit.NoSpaceUpper(result2.Position);
                    HttpContext.Session.SetString(SessionType, position);
                    HttpContext.Session.SetString(SessionId, result2.UserId.ToString());
                    HttpContext.Session.SetString(SessionName, result2.FirstName.ToString());
                    var usermodel = new UsersModel
                    {
                        UserName = result2.Username,
                        Password = result2.Password,
                        Position = position,
                        Code = result2.UserType,
                        Id = result2.UserId.ToString()
                    };
                    return PartialView("_Selection", usermodel);
                }
                else
                {
                    ModelState.AddModelError("Username", "Username / Password is incorrect.");
                    return View(m);
                }
            }
            return View(m);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store";
            HttpContext.Response.Headers["Pragma"] = "no-cache";
            HttpContext.Response.Headers["Expires"] = "-1";
            return RedirectToAction("Login");
        }
    }
}
