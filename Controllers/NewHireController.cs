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

namespace NHSP.Controllers
{
    public class NewHireController : Controller
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

        public NewHireController(IConfiguration configuration, PCGContext context1, DatabaseContext context2)
        {
            _configuration = configuration;
            con = new SqlConnection(_configuration.GetConnectionString("portestConnection"));
            _context1 = context1;
            _context2 = context2;
        }
        public void GetSession()
        {
            ViewBag.Type = HttpContext.Session.GetString(SessionType);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DashNewHire()
        {
            if (HttpContext.Session.GetString(SessionType) == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "NewHire";
            return View();
        }
        public IActionResult New()
        {
            if (HttpContext.Session.GetString(SessionType) == null)
            {
                return RedirectToAction("Login", "Home");
            }
            ViewBag.Layout = "NewHire";
            return View();
        }
    }
}
