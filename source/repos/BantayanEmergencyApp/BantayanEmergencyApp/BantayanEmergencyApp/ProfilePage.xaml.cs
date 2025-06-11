using System;
using System.ComponentModel;
using System.Linq;
using Firebase.Database;
using Firebase.Database.Query;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProfilePage : ContentPage, INotifyPropertyChanged
    {
        private string loggedInUserEmail;
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private UsersModel currentUser;
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }
        public bool IsNotEditing => !IsEditing;

        public ProfilePage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
            BindingContext = this; // For binding IsEditing
            LoadUserDetails();
        }
        private async void LoadUserDetails()
        {
            try
            {
                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var userEntry = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);
                currentUser = userEntry?.Object;

                if (currentUser != null)
                {
                    FullNameLabel.Text = currentUser.FullName ?? "N/A";
                    EmailLabel.Text = currentUser.Email ?? "N/A";
                    AddressLabel.Text = currentUser.Address ?? "N/A";
                    ContactNumberLabel.Text = currentUser.ContactNumber ?? "N/A";
                    UserTypeLabel.Text = currentUser.UserType ?? "N/A";
                    StatusLabel.Text = currentUser.Status ?? "N/A";
                    AddressEntry.Text = currentUser.Address; // Pre-fill entry
                    ContactNumberEntry.Text = currentUser.ContactNumber; // Pre-fill entry
                }
                else
                {
                    await DisplayAlert("Error", "User details not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load user details: " + ex.Message, "OK");
            }
        }

        private void ToggleEditAddress(object sender, EventArgs e)
        {
            IsEditing = !IsEditing;
            if (!IsEditing)
            {
                AddressEntry.Text = currentUser?.Address; // Reset to original if canceled
            }
        }

        private void ToggleEditNumber(object sender, EventArgs e)
        {
            IsEditing = !IsEditing;
            if (!IsEditing)
            {
                ContactNumberEntry.Text = currentUser?.ContactNumber; // Reset to original if canceled
            }
        }

        private async void SaveAddress(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AddressEntry.Text))
                {
                    await DisplayAlert("Error", "Address cannot be empty.", "OK");
                    return;
                }

                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var userEntry = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);
                if (userEntry != null)
                {
                    currentUser.Address = AddressEntry.Text;
                    await firebaseClient
                        .Child("Users")
                        .Child(userEntry.Key) // Use the Firebase key to update the specific user
                        .PutAsync(currentUser);

                    AddressLabel.Text = currentUser.Address;
                    IsEditing = false;
                    await DisplayAlert("Success", "Address updated successfully!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "User not found in database.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to update address: " + ex.Message, "OK");
            }
        }

        private async void SaveNumber(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ContactNumberEntry.Text))
                {
                    await DisplayAlert("Error", "Contact number cannot be empty.", "OK");
                    return;
                }

                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var userEntry = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);
                if (userEntry != null)
                {
                    currentUser.ContactNumber = ContactNumberEntry.Text;
                    await firebaseClient
                        .Child("Users")
                        .Child(userEntry.Key) // Use the Firebase key to update the specific user
                        .PutAsync(currentUser);

                    ContactNumberLabel.Text = currentUser.ContactNumber;
                    IsEditing = false;
                    await DisplayAlert("Success", "Contact number updated successfully!", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "User not found in database.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to update contact number: " + ex.Message, "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}