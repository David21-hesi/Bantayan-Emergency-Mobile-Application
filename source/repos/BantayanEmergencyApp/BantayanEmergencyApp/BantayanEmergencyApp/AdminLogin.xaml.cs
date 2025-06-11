using BantayanEmergencyApp;
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
    public partial class AdminLogin : ContentPage

    {
        AdminRepository adminRepository = new AdminRepository();
        public AdminLogin()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                string email = emailEntry.Text;
                string password = passwordEntry.Text;
                string token = await adminRepository.SignIn(email, password);
                if (!string.IsNullOrEmpty(token))
                {
                   
                    await Navigation.PushModalAsync(new AdminDashboardPage());
                    Clear();
                }
                else
                {
                    await DisplayAlert("Sign in", "Sign in failed", "Ok");
                }
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("INVALID_EMAIL"))
                {
                    await DisplayAlert("Unauthorized", "Email not found", "Ok");
                }
                else if (exception.Message.Contains("INVALID_LOGIN_CREDENTIALS"))
                {
                    await DisplayAlert("Unauthorized", "Incorrect Password", "Ok");
                }
                else
                {
                    await DisplayAlert("Error", "No Internet Connection", "Ok");
                }
            }
        }
        public void Clear()
        {
            emailEntry.Text = string.Empty;
            passwordEntry.Text = string.Empty;
        }

        private bool isPasswordHidden = true;

        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordHidden = !isPasswordHidden;
            passwordEntry.IsPassword = isPasswordHidden;

            // Change the eye icon based on visibility
            togglePasswordButton.Source = isPasswordHidden ? "eye.png" : "hidden.png";
        }


        private void Button_Clicked_1(object sender, EventArgs e)
        {

        }
    }
}

