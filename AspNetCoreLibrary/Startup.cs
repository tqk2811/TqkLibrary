using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AspNetCoreLibrary
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
  }
}