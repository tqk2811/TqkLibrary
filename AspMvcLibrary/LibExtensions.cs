using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AspMvcLibrary
{
  public enum HashType
  {
    SHA256,
    MD5
  }
  public static class LibExtensions
  {
    public static string LocalServerPath { get { return HttpContext.Current.Server.MapPath("~"); } }
    public static string Domain { get { return "https://" + HttpContext.Current.Request.Url.Authority; } }


    public static Guid GenerateGuid()
    {
      var buffer = Guid.NewGuid().ToByteArray();

      var time = new DateTime(0x76c, 1, 1);
      var now = DateTime.Now;
      var span = new TimeSpan(now.Ticks - time.Ticks);
      var timeOfDay = now.TimeOfDay;

      var bytes = BitConverter.GetBytes(span.Days);
      var array = BitConverter.GetBytes(
          (long)(timeOfDay.TotalMilliseconds / 3.333333));

      Array.Reverse(bytes);
      Array.Reverse(array);
      Array.Copy(bytes, bytes.Length - 2, buffer, buffer.Length - 6, 2);
      Array.Copy(array, array.Length - 4, buffer, buffer.Length - 4, 4);

      return new Guid(buffer);
    }
    public static string CalcHashPassword(this string Password, HashType hashType = HashType.SHA256)
    {
      byte[] hash = null;
      switch(hashType)
      {
        case HashType.SHA256:
          using (SHA256 mySHA256 = SHA256.Create()) hash = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(Password));
          break;

        case HashType.MD5:
          using (MD5 md5 = MD5.Create()) hash = md5.ComputeHash(Encoding.UTF8.GetBytes(Password));
          break;

        default:return null;
      }
      return BitConverter.ToString(hash).Replace("-", "");
    }
    public static string ControllerName(this Type TypeController)
    {
      string controllerName = TypeController.Name;
      return controllerName.Substring(0, controllerName.LastIndexOf("Controller"));
    }
    public static string ControllerName(this string controllerName)
    {
      return controllerName.Substring(0, controllerName.LastIndexOf("Controller"));
    }


    private static readonly Random random = new Random();
    private const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static string RandomString(int length)
    {
      return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }
  }
}