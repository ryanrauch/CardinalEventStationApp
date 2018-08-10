using CardinalEventStationApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CardinalEventStationApp.Services
{
    public class SinglePageNavigationService : INavigationService
    {
        protected Application CurrentApplication
        {
            get { return Application.Current; }
        }

        public Task NavigatePopAsync()
        {
            throw new NotImplementedException();
        }

        public Task NavigatePushAsync<T>(T page) where T : Page
        {
            CurrentApplication.MainPage = page;
            return Task.CompletedTask;
        }

        public Task NavigatePushAsync<T>(T page, object param) where T : Page
        {
            (page.BindingContext as ViewModelBase).Initialize(param);
            return NavigatePushAsync(page);
        }

        public void NavigateToLogin()
        {
            //CurrentApplication.MainPage = new LoginView();
        }

        public void NavigateToMain()
        {
            //CurrentApplication.MainPage = new InitialView();
        }
    }
}
