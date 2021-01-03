using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Net.ImagesHostApi.ImgurCom
{
  public class ImgurApi : BaseApi
  {
    private const string EndPoint = "https://api.imgur.com/3";

    public ImgurApi(string ApiKey) : base(ApiKey)
    {
    }
  }
}