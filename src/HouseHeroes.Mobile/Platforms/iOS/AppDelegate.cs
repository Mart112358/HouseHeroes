using Foundation;
using Microsoft.Identity.Client;
using UIKit;

namespace HouseHeroes.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
	{
		AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url);
		return base.OpenUrl(app, url, options);
	}
}
