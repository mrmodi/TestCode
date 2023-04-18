using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestCode;
using TestCode.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.AddDbContext<LogDbContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddFile("C:/Users/manan/OneDrive/Desktop/Sem 4/Capstone Project/TestCode/TestCode/LogFiles/myapp-{Date}.txt",
        isJson: false,
        minimumLevel: LogLevel.Information,
        fileSizeLimitBytes: null,
        retainedFileCountLimit: null);

});

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
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
