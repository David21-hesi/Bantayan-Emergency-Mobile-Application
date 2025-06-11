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
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }
        private void OnGoToLoginClicked(object sender, System.EventArgs e)
        {
            Navigation.PushModalAsync(new LoginPage());
        }
        private async void OnGuestUseClicked(object sender, EventArgs e)
        {
            // Navigate to guest-access page
            await Navigation.PushModalAsync(new GuestHomePage());
        }

    }
}