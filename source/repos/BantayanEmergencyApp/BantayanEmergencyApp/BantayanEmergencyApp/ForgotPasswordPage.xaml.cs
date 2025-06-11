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
    public partial class ForgotPasswordPage : ContentPage
    {
        UserRepository userRepository = new UserRepository();
        public ForgotPasswordPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            string email = emailEntry.Text;
            if (string.IsNullOrEmpty(email)) // Show warning if email is empty
            {
                await DisplayAlert("Warning", "Please enter your email", "Ok");
                return;
            }

            bool isSend = await userRepository.ResetPassword(email);
            if (isSend)
            {
                await DisplayAlert("Reset Password", "Please check the link we sent in your email to create your new password", "Ok");
            }
            else
            {
                await DisplayAlert("Reset Password", "Failed to send link", "Ok");
            }
        }
    }
}