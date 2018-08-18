using CardinalEventStationApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardinalEventStationApp.ViewModels
{
    public class InitialViewModel : ViewModelBase
    {
        private readonly INFCReader _nfcReader;

        public InitialViewModel(INFCReader nfcReader)
        {
            _nfcReader = nfcReader;
        }

        private string _message { get; set; }
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged(() => Message);
            }
        }

        public override async Task OnAppearingAsync()
        {
            try
            {
                Message = "vm loaded";
                await _nfcReader.InitIO();
                Message = "InitIO()...";

                for (int i = 0; i < 100; ++i)
                {
                    await Task.Delay(1000);
                    var present = _nfcReader.IsTagPresent();
                    Message = i.ToString() + ":" + present.ToString();
                    if(present)
                    {
                        var u = _nfcReader.ReadUid();
                        string tmp = string.Empty;
                        foreach(var b in u.FullUid)
                        {
                            tmp += b.ToString();
                        }
                        Message = tmp;
                    }
                    await Task.Delay(1000);
                }
            }
            catch(Exception ex)
            {
                Message = ex.Message;
            }
        }
    }
}
