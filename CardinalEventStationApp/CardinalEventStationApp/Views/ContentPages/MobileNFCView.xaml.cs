using System;
using System.Collections.Generic;
using CardinalEventStationApp.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CardinalEventStationApp.Views.ContentPages
{
    public class MobileNFCViewBase : ViewPageBase<MobileNFCViewModel> { }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MobileNFCView : MobileNFCViewBase
    {
        public MobileNFCView()
        {
            InitializeComponent();
        }
    }
}
