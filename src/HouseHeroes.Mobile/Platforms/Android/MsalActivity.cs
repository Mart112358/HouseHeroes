using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace HouseHeroes.Mobile.Platforms.Android;

[Activity(Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
    DataScheme = "msal7cd7528e-e23c-4f29-9267-0e4f2af1ac9d")]
public class MsalActivity : BrowserTabActivity
{
}