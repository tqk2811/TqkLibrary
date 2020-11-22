using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AspMvcLibrary.AspMvcLib
{
  public abstract class BaseViewPage : WebViewPage
  {
    public virtual new IBasePrincipal User
    {
      get { return base.User as IBasePrincipal; }
    }
  }

  public abstract class BaseViewPage<TModel> : WebViewPage<TModel>
  {
    public virtual new IBasePrincipal User
    {
      get { return base.User as IBasePrincipal; }
    }
  }
}
