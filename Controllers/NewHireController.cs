<<<<<<<< HEAD:Controllers/HomeController.cs
﻿using Microsoft.AspNetCore.Mvc;
========
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
>>>>>>>> 83893d127d03eb24f0183a300e6f16ee043e609a:Controllers/NewHireController.cs

namespace NHSP.Controllers
{
    public class NewHireController : Controller
    {
<<<<<<<< HEAD:Controllers/HomeController.cs
        public IActionResult Index()
========
        private readonly ILogger<NewHireController> _logger;

        public NewHireController(ILogger<NewHireController> logger)
        {
            _logger = logger;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
