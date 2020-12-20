using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using TqkLibrary.Net.CloudStorage.GoogleDrive;

namespace TqkLibrary.Test
{
  [TestClass]
  public class DriveApiNonLoginTest
  {
    [TestMethod]
    public void TestDownload()
    {
      using FileStream fileStream = new FileStream("D:\\file.rar", FileMode.Create, FileAccess.Write, FileShare.Read);
      DriveApiNonLogin.Download("1d_EhjSXQqFWFTtDi-vxbGI16XcNhXPI5", fileStream).Wait();
    }

    [TestMethod]
    public void ListPublicFolder()
    {
      string result = DriveApiNonLogin.ListPublicFolder("0Bx154iMNwuyWfnZJVkxQTHJJY2J5X19pUTNabkxlWVNrUE9OUTJOVFdYWE11bkpSbDlFc0k").Result;
      Console.WriteLine(result);
    }

    [TestMethod]
    public void ExportExcel()
    {
      using FileStream fileStream = new FileStream("D:\\file.xlsx", FileMode.Create, FileAccess.Write, FileShare.Read);
      DriveApiNonLogin.ExportExcel("1BvMPOldsOOqgWbxxXX7jwHRM5342R8iKspit_DlPWmE", fileStream).Wait();
    }
  }
}