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
        private readonly ILogger<NewHireController> _logger;

        public NewHireController(ILogger<NewHireController> logger)
        {
            _logger = logger;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult DashNewHire()
        {
            return View();
        }
    }
}
