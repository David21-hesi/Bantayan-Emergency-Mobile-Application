using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class LGUQuickAccessPage : ContentPage
    {
        private string userEmail;

        public LGUQuickAccessPage(string email)
        {
            InitializeComponent();
            userEmail = email;
        }

        private async void OnEmergencyButtonClicked(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new LGUSendEmergencyPage(userEmail));
        }
    }
}
