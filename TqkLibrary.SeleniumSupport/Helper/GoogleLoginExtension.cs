using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.SeleniumSupport.Helper
{
  public static class GoogleLoginExtension
  {
    private const string manifest_json = "{\"name\": \"Auto Login Google\",\"description\": \"Auto Login Google.\",\"version\": \"0.0.1\",\"permissions\":[\"<all_urls>\",\"activeTab\",\"tabs\"],\"background\":{\"scripts\":[\"/background.js\"],\"persistent\": true},\"manifest_version\": 2,\"content_scripts\" : [{\"run_at\":\"document_idle\",\"matches\":[\"https://*.google.com/*\"],\"js\": [\"/inject.js\"]}]}";
    private const string background_js = "chrome.runtime.onMessage.addListener(function(message,sender,callback){if (message && message == \"close_tab_call\") {chrome.tabs.remove(sender.tab.id,function(){});}});";
    private const string inject_js = "var g_acc={email:\"{email}\",pass:\"{pass}\",recovery:\"{recovery}\"};var step=0;window.addEventListener('load',function(){window.setInterval(RunLogin,500);});var timeout=200;var count=0;function RunLogin(){if(count>timeout)closeChrome();count++;switch(step){case 0:{if(document.body.innerHTML.includes(g_acc.email)){step=-1;closeChrome();}var email=document.querySelector(\"input[id='identifierId']\");var btn_emailnext=document.querySelector(\"[id='identifierNext'] button\");if(email&&btn_emailnext&&!isHidden(email)){email.value=g_acc.email;btn_emailnext.click();step++;}break;}case 1:{var pass=document.querySelector(\"input[name='password']\");var btn_passnext=document.querySelector(\"[id='passwordNext'] button\");if(pass&&btn_passnext&&!isHidden(pass)){pass.value=g_acc.pass;btn_passnext.click();step++;}break;}case 2:{var recs=document.querySelector(\"div[jsname='EBHGs']:not([id])\");if(recs.length != 0){recs[recs.length - 1].click();step++;}break;}case 3:{var rec_mail=document.getElementById(\"knowledge-preregistered-email-response\");var rec_mail_next=document.querySelector(\"button[jsname='LgbsSe']\");if(rec_mail&&rec_mail_next&&!isHidden(rec_mail)){rec_mail.value=g_acc.recovery;rec_mail_next.click();step++;}break;}}}function closeChrome(){chrome.runtime.sendMessage(\"close_tab_call\");}function isHidden(el){return(el.offsetParent===null)}";

    public static void GenerateExtension(string dirPath,string email, string pass, string recovery)
    {
      string inject_ = inject_js.Replace("{email}", email).Replace("{pass}", pass).Replace("{recovery}", recovery);
      if (Directory.Exists(dirPath)) try { Directory.Delete(dirPath); } catch (Exception) { }

      var dirInfo = Directory.CreateDirectory(dirPath);

      using StreamWriter manifet = new StreamWriter(dirInfo.FullName + "\\manifest.json");
      manifet.Write(manifest_json);

      using StreamWriter inject = new StreamWriter(dirInfo.FullName + "\\inject.js");
      inject.Write(inject_);

      using StreamWriter background = new StreamWriter(dirInfo.FullName + "\\background.js");
      background.Write(background_js);
    }
  }
}
