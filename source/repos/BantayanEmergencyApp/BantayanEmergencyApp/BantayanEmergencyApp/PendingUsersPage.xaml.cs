using Xamarin.Forms;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using Xamarin.Forms.PlatformConfiguration;
using BantayanEmergencyApp.Services;
using Xamarin.Essentials;

namespace BantayanEmergencyApp
{
    public partial class PendingUsersPage : ContentPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private List<UsersModel> pendingUsersList = new List<UsersModel>();
        private ObservableCollection<UsersModel> filteredPendingUsersList = new ObservableCollection<UsersModel>();

        public PendingUsersPage()
        {
            InitializeComponent();
            LoadUsers();
            pendingUsersListView.ItemsSource = filteredPendingUsersList; // Bind to filtered list
        }

        private async void LoadUsers()
        {
            var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();
            pendingUsersList = users
                .Where(u => u.Object.Status == "Pending") // Filter only Pending users
                .Select(u => u.Object)
                .OrderByDescending(u => u.Timestamp) // Order users by their registration timestamp (latest first)
                .ToList();

            // Populate the filtered list initially
            UpdateFilteredUsersList();
        }

        private void UpdateFilteredUsersList(string searchText = "")
        {
            filteredPendingUsersList.Clear();
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? pendingUsersList
                : pendingUsersList.Where(user =>
                      user.FullName.ToLower().Contains(searchText.ToLower()) ||
                      user.Email.ToLower().Contains(searchText.ToLower()) ||
                      user.UserType.ToLower().Contains(searchText.ToLower()))
                  .ToList();

            foreach (var user in filtered)
            {
                filteredPendingUsersList.Add(user);
            }
        }

        private async void OnImageTapped(object sender, EventArgs e)
        {
            if (sender is Image image && e is TappedEventArgs tappedEventArgs)
            {
                var imageUrl = tappedEventArgs.Parameter?.ToString();

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Create a full-screen page
                    var fullScreenPage = new ContentPage
                    {
                        BackgroundColor = Color.Black,
                        Padding = 0,
                        Content = new Grid
                        {
                            Children =
                    {
                        new Image
                        {
                            Source = imageUrl,
                            Aspect = Aspect.AspectFit,
                            VerticalOptions = LayoutOptions.Fill,
                            HorizontalOptions = LayoutOptions.Fill,
                            Margin = 0
                        }
                    }
                        }
                    };

                    // Add tap gesture to close
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (s, args) =>
                    {
                        await Navigation.PopModalAsync();
                    };
                    fullScreenPage.Content.GestureRecognizers.Add(tapGesture);

                    // Push the page modally with no animation to make it instant
                    await Navigation.PushModalAsync(new NavigationPage(fullScreenPage)
                    {
                        BarBackgroundColor = Color.Black,
                        BarTextColor = Color.White
                    }, false);
                }
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredUsersList(e.NewTextValue);
        }

        private async void ApproveUser_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var user = button?.BindingContext as UsersModel;
            if (user == null) return;

            // Show a confirmation dialog before approving the user
            bool isConfirmed = await DisplayAlert("Confirm Approval",
                                                   $"Are you sure you want to approve {user.FullName}?",
                                                   "Yes", "No");

            if (isConfirmed)
            {
                var userSnapshot = (await firebaseClient.Child("Users").OnceAsync<UsersModel>())
                                    .FirstOrDefault(u => u.Object.Email == user.Email);

                if (userSnapshot != null)
                {
                    // Update user status to "Approved"
                    await firebaseClient.Child("Users").Child(userSnapshot.Key).PatchAsync(new
                    {
                        Status = "Approved",
                        DateApproved = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") // ISO 8601 UTC format
                    });

                    // Show success alert for admin
                    await DisplayAlert("Approval", "User has been successfully approved!", "OK");

                    // Send push notification if the user has a device token
                    if (!string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var pushService = DependencyService.Get<IPushNotificationService>();
                        await pushService.SendPushNotificationAsync("Account Approved", $"{user.FullName}'s account has been approved!", user.DeviceToken);
                    }

                    // Reload the users list after approval
                    LoadUsers();
                }
            }
        }

        private async void RejectUser_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var user = button?.BindingContext as UsersModel;
            if (user == null) return;

            var reason = await DisplayPromptAsync("Rejection Reason", $"Enter the reason for rejecting {user.FullName}:");

            // If user cancelled the prompt, 'reason' will be null
            if (reason == null)
            {
                return; // Simply return, no alert shown
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                await DisplayAlert("Error", "Rejection reason cannot be empty.", "OK");
                return;
            }

            var userSnapshot = (await firebaseClient.Child("Users").OnceAsync<UsersModel>())
                                .FirstOrDefault(u => u.Object.Email == user.Email);

            if (userSnapshot != null)
            {
                await firebaseClient.Child("Users").Child(userSnapshot.Key).PatchAsync(new
                {
                    Status = "Rejected",
                    RejectionReason = reason
                });

                await DisplayAlert("Success", "User Rejected!", "OK");

                var pushService = DependencyService.Get<IPushNotificationService>();
                await pushService.SendPushNotificationAsync("Account Rejected", $"{user.FullName}'s account has been rejected. Reason: {reason}", user.DeviceToken);

                LoadUsers(); // Reload users after rejection
            }
        }
        private void OnUserTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is UsersModel user)
            {
                user.IsExpanded = !user.IsExpanded;
                pendingUsersListView.ItemsSource = null;
                pendingUsersListView.ItemsSource = filteredPendingUsersList;
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var contactNumber = button?.CommandParameter.ToString(); // Get the contact number from the button's CommandParameter

            if (!string.IsNullOrEmpty(contactNumber))
            {
                // Display the action sheet to let the user select the contact number
                string action = await DisplayActionSheet("Call User", "Cancel", null, contactNumber);

                if (action != "Cancel" && !string.IsNullOrEmpty(action))
                {
                    // Remove any spaces and call the selected number
                    await Launcher.OpenAsync($"tel:{action.Replace(" ", "")}");
                }
            }
            else
            {
                // If the contact number is null or empty, handle that case
                await DisplayAlert("Error", "Contact number is not available.", "OK");
            }
        }
    }
}
