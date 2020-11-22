using System.Web.Mvc;
namespace AspMvcLibrary.AspMvcLib
{
  public class BaseController : Controller
  {
    protected virtual new IBasePrincipal User
    {
      get { return HttpContext.User as IBasePrincipal; }
    }
    public RedirectToRouteResult RedirectToAction<T>(string ActionName, object routeValues) where T : BaseController
    {
      string controllerName = typeof(T).Name;
      controllerName = controllerName.Substring(0, controllerName.LastIndexOf("Controller"));
      return RedirectToAction(ActionName, controllerName, routeValues);
    }
    public RedirectToRouteResult RedirectToAction<T>(string ActionName) where T : BaseController
    {
      return RedirectToAction<T>(ActionName, null);
    }
  }
}
