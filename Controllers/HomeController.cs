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
using static Azure.Core.HttpHeader;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;
using System.Text;

namespace NHSP.Controllers
{
    public class HomeController : Controller
    {
        private readonly NHSPContext _context1;
        private readonly DatabaseContext _context2;
        private readonly HttpClient _httpClient;
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

        public HomeController(IConfiguration configuration, IWebHostEnvironment hostingEnvironment, NHSPContext context1, DatabaseContext context2, FileUploadService fileUploadPayroll, HttpClient httpClient)
        {
            _configuration = configuration;
            con = new SqlConnection(_configuration.GetConnectionString("pettycashConnection"));
            _context1 = context1;
            _context2 = context2;
            _fileUploadPayroll = fileUploadPayroll;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = new FileUploadService(Path.Combine(_hostingEnvironment.WebRootPath, "Files"));
            _httpClient = httpClient;
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
        public async Task<IActionResult> Login(LoginModel m)
        {
            //string apiUrl = "http://t0pnotch-001-site8.jtempurl.com/api/Values/payrollusers";

            //var response = await _httpClient.GetAsync(apiUrl);

            //if (!response.IsSuccessStatusCode)
            //{
            //    return View("Error");
            //}

            //string jsonString = await response.Content.ReadAsStringAsync();
            //var users = JsonSerializer.Deserialize<List<Users>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            //var userfilter = users.Where(a => a.Username == m.Username && a.Password == m.Password && a.UserStatus == "Active")
            //    .Select(a => new
            //        {
            //            a.UserId,
            //            a.FirstName,
            //            a.Username,
            //            a.Password,
            //            a.Position,
            //            a.UserType
            //        }).FirstOrDefault();

            //    if (userfilter != null)
            //    {
            //        string position = StringEdit.NoSpaceUpper(userfilter.Position);
            //        HttpContext.Session.SetString(SessionType, position);
            //        HttpContext.Session.SetString(SessionId, userfilter.UserId.ToString());
            //        HttpContext.Session.SetString(SessionName, userfilter.FirstName);

            //        var usermodel = new UsersModel
            //        {
            //            UserName = userfilter.Username,
            //            Password = userfilter.Password,
            //            Position = position,
            //            Id = userfilter.UserId.ToString()
            //        };

            //        return RedirectToAction("DashPayroll", "Payroll", new { area = "Payroll" });
            //    }            
            //else
            //{
            //    ModelState.AddModelError("Username", "Username / Password is invalid.");
            //}
            if (ModelState.IsValid)
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }

                var query2 = _context1.Users
                            .Where(a => a.Username == m.Username && a.Password == m.Password && a.UserStatus == "Active")
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
                    if (result1 != null)
                    {
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
                        return RedirectToAction("DashPayroll", "Payroll", new { area = "Payroll" });
                    }
                    else
                    {
                        ModelState.AddModelError("Username", "Username / Password is invalid.");
                    }
                }
                else
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
                    return RedirectToAction("DashPayroll", "Payroll", new { area = "Payroll" });
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
        public IActionResult Register()
        {
            HttpContext.Session.Clear();
            HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store";
            HttpContext.Response.Headers["Pragma"] = "no-cache";
            HttpContext.Response.Headers["Expires"] = "-1";
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(Users u)
        {
            var user = _context1.Users
                        .Where(a => a.Username == u.Username)
                        .Select(a => new
                        {
                            a.Username,
                            a.Password
                        }).FirstOrDefault();

            ModelState.Remove("LastName");
            ModelState.Remove("FirstName");
            ModelState.Remove("MiddleName");
            ModelState.Remove("Position");
            ModelState.Remove("ContactNo");
            ModelState.Remove("Username");
            ModelState.Remove("Password");

            if (string.IsNullOrEmpty(u.LastName))
            {
                ModelState.AddModelError("LastName", "Last Name is invalid");
            }
            if (string.IsNullOrEmpty(u.FirstName))
            {
                ModelState.AddModelError("FirstName", "First Name is invalid");
            }
            if (string.IsNullOrEmpty(u.MiddleName))
            {
                ModelState.AddModelError("MiddleName", "Middle Name is invalid");
            }
            if (string.IsNullOrEmpty(u.Position))
            {
                ModelState.AddModelError("Position", "Position is invalid");
            }
            if (string.IsNullOrEmpty(u.ContactNo))
            {
                ModelState.AddModelError("ContactNo", "Contact # is invalid");
            }
            if (string.IsNullOrEmpty(u.Username))
            {
                ModelState.AddModelError("Username", "Username is invalid");
            }
            if (user != null)
            {
                ModelState.AddModelError("Username", "Username is taken");
            }
            if (string.IsNullOrEmpty(u.Password) )
            {
                ModelState.AddModelError("Password", "Password is invalid");
            }
            if (ModelState.IsValid)
            {
                _context1.Users.Add(u); 
                _context1.SaveChanges();
                return RedirectToAction("Login");
            }
            else
            {
                return View(u);
            }
        }
    }
}