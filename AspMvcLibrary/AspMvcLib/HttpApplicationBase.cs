using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace AspMvcLibrary.AspMvcLib
{
  public interface IUserSession
  {
    Guid Id { get; set; }
    long? AccountId { get; set; }
    bool RememberMe { get; set; }
    DateTime ExpiresTime { get; set; }
    DateTime? AccountExpiresTime { get; set; }
  }
  public interface IUserSessionRepository : IRepository<IUserSession>
  {
    IUserSession WhereByGuid(Guid guid);
  }

  [Serializable]
  public class AuthCookieSerialize
  {
    public Guid SessionId { get; set; }
  }
  //public class HttpApplicationBase<T> : System.Web.HttpApplication where T : class, IUserSessionRepository, new()
  //{
  //  protected int AccountExpiresTime { get; set; } = 30 * 60;//30 min
  //  protected int AccountExpiresTimeRememberMe { get; set; } = 60 * 60 * 24 * 365;//1 year
  //  protected string[] Languages { get; }
  //  protected int DefaultLanguageIndex { get; }
  //  protected string CookieName { get; set; } = FormsAuthentication.FormsCookieName;

  //  protected HttpApplicationBase(string[] Languages,int DefaultLanguageIndex)
  //  {
  //    if (Languages == null || Languages.Count() == 0) throw new ArgumentNullException(nameof(Languages));
  //    if (DefaultLanguageIndex < 0 || DefaultLanguageIndex >= Languages.Count()) throw new ArgumentOutOfRangeException(nameof(DefaultLanguageIndex));
  //    this.Languages = Languages;
  //    this.DefaultLanguageIndex = DefaultLanguageIndex;
  //  }


  //  protected void Application_BeginRequest(object sender, EventArgs e)
  //  {
  //    HttpCookie httpCookie = HttpContext.Current.Request.Cookies["lang"];
  //    string lang = Languages[DefaultLanguageIndex];
  //    if (httpCookie != null && !string.IsNullOrEmpty(httpCookie.Value))//found
  //    {
  //      if (Languages.Any((l) => l.Equals(httpCookie.Value))) lang = httpCookie.Value;//check valid
  //      else//re create
  //      {
  //        httpCookie = new HttpCookie("lang");
  //        httpCookie.Value = lang;
  //        httpCookie.Expires = DateTime.Now.AddYears(10);
  //        Response.Cookies.Add(httpCookie);
  //      }
  //    }
  //    else//create
  //    {
  //      httpCookie = new HttpCookie("lang");
  //      string cookie_acceptlang = Request.Headers.GetValues("Accept-Language").FirstOrDefault();
  //      if (!string.IsNullOrEmpty(cookie_acceptlang))
  //      {
  //        string _lang = cookie_acceptlang.Split(';').FirstOrDefault();
  //        if (!string.IsNullOrEmpty(_lang))
  //        {
  //          string[] langs = _lang.Split(',');
  //          foreach (string l in langs)
  //          {
  //            if (Languages.Any(ls => ls.Equals(l)))
  //            {
  //              lang = l;
  //              break;
  //            }
  //          }
  //        }
  //      }
  //      httpCookie.Value = lang;
  //      httpCookie.Expires = DateTime.Now.AddYears(10);
  //      Response.Cookies.Add(httpCookie);
  //    }
  //    Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
  //    Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
  //  }

  //  protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
  //  {
  //    IUserSession userSession;
  //    HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
  //    using (T userSessionRepository = new T())
  //    {
  //      if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
  //      {
  //        //decrpyt & Deserialize cookie
  //        FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
  //        JavaScriptSerializer serializer = new JavaScriptSerializer();
  //        try
  //        {
  //          AuthCookieSerialize authCookieSerialize = serializer.Deserialize<AuthCookieSerialize>(authTicket.UserData);
  //          //find usersession in db
  //          userSession = userSessionRepository.WhereByGuid(authCookieSerialize.SessionId);
  //          if (userSession != null)
  //          {
  //            if (userSession.AccountExpiresTime != null && userSession.AccountExpiresTime.Value > DateTime.Now)//account not expires -> login
  //            {
  //              //increase time Expires
  //              if (userSession.RememberMe) userSession.AccountExpiresTime = DateTime.Now.AddSeconds(AccountExpiresTimeRememberMe);
  //              else userSession.AccountExpiresTime = DateTime.Now.AddSeconds(AccountExpiresTime);
  //            }
  //            else//expires -> logout, remove account in session
  //            {
  //              userSession.AccountExpiresTime = null;
  //              userSession.AccountId = null;
  //            }
  //            if (userSession.RememberMe) userSession.ExpiresTime = DateTime.Now.AddSeconds(AccountExpiresTimeRememberMe);
  //            else userSession.ExpiresTime = DateTime.Now.AddYears(10);
  //            //update session to db
  //            userSessionRepository.Update(userSession);
  //            userSessionRepository.Save();

  //            //load user Principal
  //            HttpContext.Current.User = new CustomPrincipal(userSession);

  //            //update change to cookie client
  //            HttpCookie httpCookie = new HttpCookie(FormsAuthentication.FormsCookieName);
  //            httpCookie.Value = authCookie.Value;
  //            if (userSession.RememberMe) httpCookie.Expires = DateTime.Now.AddSeconds(AccountExpiresTimeRememberMe);
  //            else httpCookie.Expires = DateTime.Now.AddYears(10);
  //            Response.Cookies.Add(httpCookie);
  //            return;
  //          }
  //        }
  //        catch (Exception) { }
  //      }

  //      //create new sesion & cookie
  //      userSession = new UserSession();
  //      userSession.Id = LibExtensions.GenerateGuid();
  //      userSession.ExpiresTime = DateTime.Now.AddDays(90);
  //      userSessionRepository.Insert(userSession);
  //      userSessionRepository.Save();

  //      //load user Principal
  //      HttpContext.Current.User = new CustomPrincipal(userSession);

  //      //add cookie to client
  //      Response.Cookies.Add(
  //      new HttpCookie(
  //        FormsAuthentication.FormsCookieName,
  //        FormsAuthentication.Encrypt(
  //          new FormsAuthenticationTicket(
  //               1,
  //               userSession.Id.ToString(),
  //               DateTime.Now,
  //               DateTime.Now.AddYears(20),
  //               false,
  //               new JavaScriptSerializer().Serialize(
  //                 new AuthCookieSerialize() { SessionId = userSession.Id }))))
  //      {
  //        Expires = DateTime.Now.AddDays(Extensions.sessionDayMax)//cookie Expires
  //      });
  //    }
  //  }
  //}
}
