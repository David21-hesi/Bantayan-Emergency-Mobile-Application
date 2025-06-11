using Xamarin.Forms;
using Firebase.Database;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Firebase.Database.Query;

namespace BantayanEmergencyApp
{
    public partial class RejectedUsersPage : ContentPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private List<UsersModel> rejectedUsersList = new List<UsersModel>();
        private ObservableCollection<UsersModel> filteredRejectedUsersList = new ObservableCollection<UsersModel>();

        public RejectedUsersPage()
        {
            InitializeComponent();
            LoadUsers();
            rejectedUsersListView.ItemsSource = filteredRejectedUsersList; // Bind to the filtered list
        }

        private async void LoadUsers()
        {
            var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

            // Filter out soft-deleted users and only load rejected users
            rejectedUsersList = users
                .Where(u => u.Object.Status == "Rejected" && !u.Object.IsDeleted)  // Exclude soft-deleted users
                .Select(u => u.Object)
                .ToList();

            // Populate the filtered list initially
            UpdateFilteredUsersList();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredUsersList(e.NewTextValue);
        }

        private async void OnDeleteUserClicked(object sender, System.EventArgs e)
        {
            var button = sender as ImageButton;
            var user = button.BindingContext as UsersModel;

            if (user == null) return;

            bool confirm = await DisplayAlert("Confirm", $"Are you sure you want to delete {user.FullName}?", "Yes", "No");
            if (!confirm) return;

            var userSnapshot = (await firebaseClient.Child("Users").OnceAsync<UsersModel>())
                                .FirstOrDefault(u => u.Object.Email == user.Email);

            if (userSnapshot != null)
            {
                // Soft delete: Update the IsDeleted flag
                await firebaseClient
                    .Child("Users")
                    .Child(userSnapshot.Key)
                    .PatchAsync(new { IsDeleted = true });

                await DisplayAlert("Success", "User has been marked as deleted.", "OK");

                // Refresh the list
                LoadUsers();
            }
        }

        private void UpdateFilteredUsersList(string searchText = "")
        {
            filteredRejectedUsersList.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? rejectedUsersList.Where(u => !u.IsDeleted)  // Exclude soft-deleted users
                : rejectedUsersList
                    .Where(user =>
                        !user.IsDeleted && // Exclude soft-deleted users
                        (user.FullName.ToLower().Contains(searchText.ToLower()) ||
                         user.Email.ToLower().Contains(searchText.ToLower()) ||
                         user.UserType.ToLower().Contains(searchText.ToLower())))
                    .ToList();

            foreach (var user in filtered)
            {
                filteredRejectedUsersList.Add(user);
            }
        }


    }
}
