using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using NHSP.Payroll.Formula;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NHSP.Areas.Payroll.Models;
using NHSP.Models;

var builder = WebApplication.CreateBuilder(args);
//Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Set session timeout
    options.Cookie.HttpOnly = true; // Set cookie options
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("pettycashConnection")));
builder.Services.AddDbContext<NHSPContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("nhspConnection")));

var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PayrollFiles");
builder.Services.AddSingleton(new FileUploadService(uploadPath));
// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthorization();

app.UseSession();

app.UseRouting();

app.MapControllerRoute(
    name: "area",
    pattern: "{area:exists}/{controller=Payroll}/{action= DashPayroll}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
