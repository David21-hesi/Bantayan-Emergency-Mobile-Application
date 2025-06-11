using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class ResidentsQuickAccessPage : ContentPage
    {
        private string userEmail;

        public ResidentsQuickAccessPage(string email)
        {
            InitializeComponent();
            userEmail = email;
        }

        private async void OnEmergencyButtonClicked(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new EmergencyPage(userEmail));
        }
    }
}
