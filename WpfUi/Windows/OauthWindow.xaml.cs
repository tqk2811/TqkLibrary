using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfUi.Windows
{
  /// <summary>
  /// Interaction logic for OauthWindow.xaml
  /// </summary>
  public partial class OauthWindow : Window
  {
    readonly Uri uri;
    readonly Uri redirect_uri;
    public OauthWindow(Uri uri, Uri redirect_uri)
    {
      this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
      this.redirect_uri = redirect_uri ?? throw new ArgumentNullException(nameof(redirect_uri));
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      WB.Navigate(uri);
    }

    private void WB_Navigating(object sender, NavigatingCancelEventArgs e)
    {
      if(e.Uri.AbsoluteUri.StartsWith(redirect_uri.AbsoluteUri))
      {
        UriResult = e.Uri;
        this.Close();
      }
    }

    public Uri UriResult { get; private set; }
  }
}
