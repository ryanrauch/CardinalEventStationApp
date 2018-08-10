using System.Threading.Tasks;
using Xamarin.Forms;

namespace CardinalEventStationApp.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateToMain();
        void NavigateToLogin();
        Task NavigatePopAsync();
        Task NavigatePushAsync<T>(T page) where T : Page;
        Task NavigatePushAsync<T>(T page, object param) where T : Page;
    }
}
