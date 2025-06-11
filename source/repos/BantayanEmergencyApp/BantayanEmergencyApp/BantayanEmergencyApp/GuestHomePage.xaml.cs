using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GuestHomePage : ContentPage
    {

        public GuestHomePage()
        {
            InitializeComponent();
            
        }

        private async void CallPolice_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Call Police", "Cancel", null,
                "0917 953 4315", "0955 158 8153");
            if (action != "Cancel" && !string.IsNullOrEmpty(action))
                await Launcher.OpenAsync($"tel:{action.Replace(" ", "")}");
        }

        private async void CallFire_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("tel:09957910021");
        }

        private async void CallMedical_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Call Medical", "Cancel", null,
                "0942 464 7192", "0927 447 5176");
            if (action != "Cancel" && !string.IsNullOrEmpty(action))
                await Launcher.OpenAsync($"tel:{action.Replace(" ", "")}");
        }

        private async void CallBanelco_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Call Banelco", "Cancel", null,
                "0931 117 6126", "0943 701 2696");
            if (action != "Cancel" && !string.IsNullOrEmpty(action))
                await Launcher.OpenAsync($"tel:{action.Replace(" ", "")}");
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new GuestSendReportPage());
        }
    }
}
