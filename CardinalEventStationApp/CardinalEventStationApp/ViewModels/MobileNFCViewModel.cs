using System;
using System.Threading.Tasks;

namespace CardinalEventStationApp.ViewModels
{
    public class MobileNFCViewModel : ViewModelBase
    {
        public MobileNFCViewModel()
        {
        }

        public override Task OnAppearingAsync()
        {
            return Task.CompletedTask;
        }
    }
}
