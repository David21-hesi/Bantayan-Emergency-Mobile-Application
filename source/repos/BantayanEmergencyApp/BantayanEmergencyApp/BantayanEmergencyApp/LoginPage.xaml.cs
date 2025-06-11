using Firebase.Database;
using Firebase.Database.Query;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;// ✅ Ensure this namespace is included

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private readonly UserRepository userRepository; // ✅ Declare userRepository

        public LoginPage()
        {
            InitializeComponent();
            userRepository = new UserRepository();  // ✅ Initialize userRepository
        }

        private void ToLGUloginform(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new LGU_login());
        }

        private void ToAdminLoginForm(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new AdminLogin());
        }

        private async void ToExitApp(object sender, EventArgs e)
        {
            bool confirmExit = await DisplayAlert("Exit", "Are you sure you want to exit?", "Yes", "No");
            if (confirmExit)
            {
                System.Environment.Exit(0);
            }
        }

        private void ToRegistrationPage(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new RegistrationPage());
        }

        private async void ToResidentPage(object sender, EventArgs e)
        {
            try
            {
                string email = emailEntry.Text;
                string password = passwordEntry.Text;

                // ✅ Attempt to sign in with Firebase Authentication
                string token = await userRepository.SignIn(email, password);

                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Sign In Failed", "Incorrect email or password.", "OK");
                    return;
                }

                // ✅ After successful auth, fetch user data
                UsersModel user = await userRepository.GetUserByEmail(email);

                if (user == null)
                {
                    await DisplayAlert("Unauthorized", "User data not found in the database.", "OK");
                    return;
                }

                if (user.UserType != "Resident")
                {
                    await DisplayAlert("Unauthorized", "This account is not a Resident account.", "OK");
                    return;
                }
                if (user.Status == "Banned")
                {
                    if (user.BanEndDate == null || user.BanEndDate > DateTime.UtcNow)
                    {
                        string banMessage = user.BanEndDate == null
                            ? $"Your account has been permanently banned. Reason: {user.BanReason}"
                            : $"Your account is banned until {user.BanEndDate.Value:MMMM dd, yyyy}. Reason: {user.BanReason}";

                        await DisplayAlert("Account Banned", banMessage, "OK");
                        return;
                    }
                    else
                    {
                        await firebaseClient
                            .Child("Users")
                            .Child(user.UserId)
                            .PatchAsync(new { Status = "Approved", BanEndDate = (DateTime?)null, BanReason = (string)null });

                        await DisplayAlert("Ban Lifted", "Your account ban has been lifted. You can now log in.", "OK");
                    }
                }


                if (user.Status == "Pending")
                {
                    bool update = await DisplayAlert("Account Pending",
                        "Your account is still under review. Would you like to update your details?",
                        "Update Profile", "Cancel");

                    if (update)
                    {
                        await Navigation.PushModalAsync(new UpdateProfilePage(user.UserId));
                    }
                    return;
                }
                else if (user.Status == "Rejected")
                {
                    string rejectionMessage = string.IsNullOrWhiteSpace(user.RejectionReason)
                        ? "Your account has been rejected. Please contact support for more information."
                        : $"Your account has been rejected.\nReason: {user.RejectionReason}";

                    await DisplayAlert("Account Rejected", rejectionMessage, "OK");
                    return;
                }


                Preferences.Set("UserEmail", email);

                // No need for push notifications anymore
                // The token and FCM functionality have been removed

                await Navigation.PushModalAsync(new Resident2Page(email));
                Clear();
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                await DisplayAlert("Login Error", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private bool isPasswordHidden = true;

        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordHidden = !isPasswordHidden;
            passwordEntry.IsPassword = isPasswordHidden;

            // Change the eye icon based on visibility
            togglePasswordButton.Source = isPasswordHidden ? "eye.png" : "hidden.png";
        }

        public void Clear()
        {
            emailEntry.Text = string.Empty;
            passwordEntry.Text = string.Empty;
        }

        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new ForgotPasswordPage());
        }
    }
}
