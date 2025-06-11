using Firebase.Database;
using Firebase.Database.Query;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LGU_login : ContentPage
    {
        private readonly FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private readonly UserRepository userRepository; // ✅ Declare userRepository

        public LGU_login()
        {
            InitializeComponent();
            userRepository = new UserRepository();  // ✅ Initialize userRepository
        }

        private bool isPasswordHidden = true;

        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordHidden = !isPasswordHidden;
            passwordEntry.IsPassword = isPasswordHidden;

            // Change the eye icon based on visibility
            togglePasswordButton.Source = isPasswordHidden ? "eye.png" : "hidden.png";
        }

        private async void ToLGUPage(object sender, EventArgs e)
        {
            try
            {
                string email = emailEntry.Text;
                string password = passwordEntry.Text;

                // ✅ Try to sign in using Firebase Authentication first
                string token = await userRepository.SignIn(email, password);

                if (string.IsNullOrEmpty(token))
                {
                    await DisplayAlert("Sign In Failed", "Incorrect email or password.", "OK");
                    return;
                }

                // ✅ Get user details from database AFTER successful auth
                UsersModel user = await userRepository.GetUserByEmail(email);

                if (user == null)
                {
                    await DisplayAlert("Unauthorized", "User data not found in the database.", "OK");
                    return;
                }

                // ✅ Ensure only LGU accounts can log in
                if (user.UserType != "Police" &&
                    user.UserType != "Medical Team" &&
                    user.UserType != "Fire Department" &&
                    user.UserType != "Banelco")
                {
                    await DisplayAlert("Unauthorized", "This account is not an LGU account.", "OK");
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


                // ✅ Everything OK — Navigate to LGU dashboard
                await Navigation.PushModalAsync(new LGU2Page(email));
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


        private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new ForgotPasswordPage());
        }
    }
}
