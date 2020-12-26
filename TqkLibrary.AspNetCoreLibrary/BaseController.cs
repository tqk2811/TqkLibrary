using Microsoft.AspNetCore.Mvc;

namespace TqkLibrary.AspNetCoreLibrary
{
  public abstract class BaseController : Controller
  {
    protected RedirectToActionResult RedirectToAction<T>(string ActionName, object routeValues) where T : Controller
    {
      string controllerName = typeof(T).Name;
      controllerName = controllerName.Substring(0, controllerName.LastIndexOf("Controller"));
      return RedirectToAction(ActionName, controllerName, routeValues);
    }

    protected RedirectToActionResult RedirectToAction<T>(string ActionName) where T : Controller => RedirectToAction<T>(ActionName, null);
  }
}