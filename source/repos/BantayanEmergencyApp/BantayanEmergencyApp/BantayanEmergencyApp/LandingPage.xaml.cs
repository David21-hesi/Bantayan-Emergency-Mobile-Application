using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LandingPage : ContentPage
    {
        public LandingPage()
        {
            InitializeComponent();

            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                Application.Current.MainPage = new AboutPage();
                return false;
            });
        }
    }
}
