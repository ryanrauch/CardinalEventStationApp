using CardinalEventStationApp.ViewModels;
using Xamarin.Forms.Xaml;

namespace CardinalEventStationApp.Views.ContentPages
{
    public class InitialViewBase : ViewPageBase<InitialViewModel> { }

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class InitialView : InitialViewBase
	{
		public InitialView ()
		{
			InitializeComponent ();
		}
	}
}