using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;

namespace TqkLibrary.AspNetCoreLibrary
{
  public class BaseStartup
  {
    public BaseStartup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
      Configuration = configuration;
      WebHostEnvironment = webHostEnvironment;
    }

    public IWebHostEnvironment WebHostEnvironment { get; }
    public IConfiguration Configuration { get; }

    public virtual void ConfigureServices(IServiceCollection services)
    {
      services.AddDatabaseDeveloperPageExceptionFilter();
      services.AddDataProtection().SetApplicationName(WebHostEnvironment.ApplicationName).PersistKeysToFileSystem(new DirectoryInfo($"{WebHostEnvironment.ContentRootPath}\\PersistKeys"));
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        //app.UseDatabaseErrorPage();
        app.UseMigrationsEndPoint();
      }
    }
  }
}