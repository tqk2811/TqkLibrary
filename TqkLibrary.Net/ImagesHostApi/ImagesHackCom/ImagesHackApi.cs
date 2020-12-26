using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Net.ImagesHostApi.ImagesHackCom
{
  //https://docs.google.com/document/d/16M3qaw27vgwuwXqExo0aIC0nni42OOuWu_OGvpYl7dE
  public class ImagesHackApi : BaseApi
  {
    public ImagesHackApi(string ApiKey) : base(ApiKey)
    {
    }

    public void UploadImage(Bitmap bitmap)
    {
      using HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
      httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Client-ID", ApiKey);
    }
  }
}