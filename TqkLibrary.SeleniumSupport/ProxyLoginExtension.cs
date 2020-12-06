using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Text;

namespace TqkLibrary.SeleniumSupport
{
  public static class ProxyLoginExtension
  {
    const string manifest_json = "{\"version\":\"1.0.0\",\"manifest_version\":2,\"name\":\"ChromeProxy\",\"permissions\":[\"proxy\",\"tabs\",\"unlimitedStorage\",\"storage\",\"<all_urls>\",\"webRequest\",\"webRequestBlocking\"],\"background\":{\"scripts\":[\"background.js\"]},\"minimum_chrome_version\":\"22.0.0\"}";
    const string background_js = "var config={mode:\"fixed_servers\",rules:{singleProxy:{scheme:\"http\",host:\"{host}\",port:{port}},bypassList:[\"localhost\"]}};chrome.proxy.settings.set({value:config,scope:\"regular\"},function(){});function callbackFn(details){return{authCredentials:{username:\"{username}\",password:\"{password}\"}};}chrome.webRequest.onAuthRequired.addListener(callbackFn,{urls:[\"<all_urls>\"]},['blocking']);";

    public static void GenerateExtension(string filepath, string host, string port, string username, string password)
    {
      string background_ = background_js.Replace("{host}", host).Replace("{port}", port.ToString()).Replace("{username}", username).Replace("{password}", password);
      ZipFile zipFile = ZipFile.Create(filepath);
      zipFile.BeginUpdate();
      using CustomStaticDataSource manifest = new CustomStaticDataSource(manifest_json);
      zipFile.Add(manifest, "manifest.json");
      using CustomStaticDataSource background = new CustomStaticDataSource(background_); 
      zipFile.Add(background, "background.js");
      zipFile.CommitUpdate();
      zipFile.Close();
    }
    public static void GenerateExtension(string filepath, string host,int port,string username, string password) => GenerateExtension(filepath, host, port, username, password);

  }

  internal class CustomStaticDataSource : IStaticDataSource , IDisposable
  {
    readonly MemoryStream memoryStream;
    public CustomStaticDataSource(string content)
    {
      this.memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
    public void Dispose() => memoryStream.Dispose();
    public Stream GetSource() => memoryStream;
  }
}
