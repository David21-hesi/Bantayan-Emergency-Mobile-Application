using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class Resident2Page : FlyoutPage
    {
        private string loggedInUserEmail;
        private List<MenuItemModel> menuItems;
        private const string ProfileImageKey = "ProfileImagePath";

        private FirebaseClient firebase = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public Resident2Page(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
            SetupMenu();
            LoadProfileImage();
            LoadUserInfo(); // 🔽 Load contact number from Firebase
        }

        private async void LoadUserInfo()
        {
            try
            {
                var users = await firebase.Child("Users").OnceAsync<UsersModel>();

                foreach (var user in users)
                {
                    if (string.Equals(user.Object.Email, loggedInUserEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        var current = user.Object.ContactNumber;
                        var pending = user.Object.PendingContactNumber;

                        // Set Contact Number
                        ProfileContactNumber.Text = !string.IsNullOrWhiteSpace(pending)
                            ? $"{current} (Change Pending)"
                            : current;

                        // Set Full Name and Usertype
                        ProfileFullName.Text = user.Object.FullName;
                        ProfileUserType.Text = user.Object.UserType;

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load user information: {ex.Message}", "OK");
            }
        }

        private void SetupMenu()
        {
            menuItems = new List<MenuItemModel>
            {
                new MenuItemModel { Title = "Quick Access", Icon = "📲", PageType = typeof(ResidentsQuickAccessPage) },
                new MenuItemModel { Title = "Directory", Icon = "📞", PageType = typeof(Directorypage) },
                new MenuItemModel { Title = "Updates", Icon = "⚠️", PageType = typeof(UpdatesPage) },
                new MenuItemModel { Title = "Logout", Icon = "🚪", PageType = null }
            };

            MenuListView.ItemsSource = menuItems;
            Detail = new NavigationPage(new ResidentsQuickAccessPage(loggedInUserEmail))
            {
                BarBackgroundColor = Color.FromHex("#B71C1C"),
                BarTextColor = Color.White
            };
        }

        private void MenuItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedItem = e.SelectedItem as MenuItemModel;
            if (selectedItem == null) return;

            if (selectedItem.Title == "Logout")
            {
                Logout_Clicked(sender, null);
            }
            else if (selectedItem.PageType != null)
            {
                var page = (Page)Activator.CreateInstance(selectedItem.PageType, loggedInUserEmail);
                Detail = new NavigationPage(page)
                {
                    BarBackgroundColor = Color.FromHex("#B71C1C"),
                    BarTextColor = Color.White
                };
            }

            IsPresented = false;
            MenuListView.SelectedItem = null;
        }

        private async void OnUploadProfileButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Photos>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Denied", "Cannot access photos. Please grant permission.", "OK");
                    return;
                }

                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Profile Picture"
                });

                if (result == null)
                    return;

                var newFile = Path.Combine(FileSystem.AppDataDirectory, $"{loggedInUserEmail}_profile.jpg");
                using (var stream = await result.OpenReadAsync())
                using (var newStream = File.OpenWrite(newFile))
                {
                    await stream.CopyToAsync(newStream);
                }

                ProfileImage.Source = ImageSource.FromFile(newFile);
                Preferences.Set($"{ProfileImageKey}_{loggedInUserEmail}", newFile);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to upload profile picture: {ex.Message}", "OK");
            }
        }

        private void LoadProfileImage()
        {
            var imageKey = $"{ProfileImageKey}_{loggedInUserEmail}";
            if (Preferences.ContainsKey(imageKey))
            {
                var imagePath = Preferences.Get(imageKey, string.Empty);
                if (File.Exists(imagePath))
                {
                    ProfileImage.Source = ImageSource.FromFile(imagePath);
                    return;
                }
            }

            ProfileImage.Source = "user.png";
        }

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
            if (confirm)
            {
                Preferences.Remove("UserEmail");
                Preferences.Remove("UserRole");
                Application.Current.MainPage = new LoginPage();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                bool answer = await DisplayAlert("Confirmation", "Are you sure you want to exit?", "Yes", "No");
                if (answer)
                {
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                }
            });
            return true;
        }
        private async void OnEditContactClicked(object sender, EventArgs e)
        {
            try
            {
                var newContactNumber = await DisplayPromptAsync("Edit Contact Number", "Enter new contact number:");

                if (string.IsNullOrWhiteSpace(newContactNumber))
                    return;

                var users = await firebase.Child("Users").OnceAsync<UsersModel>();

                foreach (var user in users)
                {
                    if (string.Equals(user.Object.Email, loggedInUserEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        // Save the pending number to Firebase
                        await firebase.Child("Users")
                                      .Child(user.Key)
                                      .PatchAsync(new { PendingContactNumber = newContactNumber });

                        await DisplayAlert("Submitted", "Your contact number change is now pending admin approval.", "OK");
                        ProfileContactNumber.Text = $"{user.Object.ContactNumber} (Pending)";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to submit contact number change: {ex.Message}", "OK");
            }
        }
    }

    public class MenuItemModel
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public Type PageType { get; set; }
    }
}
