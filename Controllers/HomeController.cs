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
using static Azure.Core.HttpHeader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace NHSP.Controllers
{
    public class HomeController : Controller
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

        public HomeController(IConfiguration configuration, PCGContext context1, DatabaseContext context2)
        {
            _configuration = configuration;
            con = new SqlConnection(_configuration.GetConnectionString("portestConnection"));
            _context1 = context1;
            _context2 = context2;
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
        public IActionResult Selection(LoginModel m)
        {
            if (ModelState.IsValid)
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
                var query = from u in _context2.tbl_users
                            join c in _context2.tbl_contents on u.User_Type equals c.id into uc
                            from c in uc.DefaultIfEmpty()
                            where u.Username == m.Username && u.Password == m.Password
                            select new
                            {
                                u.id,
                                u.Username,
                                u.Password,
                                Code = c.Code
                            };
                var result = query.FirstOrDefault();

                if (result != null)
                {
                    HttpContext.Session.SetString(SessionType, result.Code);
                    HttpContext.Session.SetString(SessionId, result.id.ToString());
                    var usermodel = new UsersModel
                    {
                        UserName = result.Username,
                        Password = result.Password,
                        Code = result.Code
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
    }
}
