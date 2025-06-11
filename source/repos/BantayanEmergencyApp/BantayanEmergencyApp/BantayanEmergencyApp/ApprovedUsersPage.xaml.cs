using Xamarin.Forms;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using Xamarin.Essentials; // Added for Preferences API
using BantayanEmergencyApp.Services;

namespace BantayanEmergencyApp
{
    public partial class ApprovedUsersPage : ContentPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private List<UsersModel> approvedUsersList = new List<UsersModel>();
        private ObservableCollection<UsersModel> filteredApprovedUsersList = new ObservableCollection<UsersModel>();

        public ApprovedUsersPage()
        {
            InitializeComponent();
            LoadUsers();
            approvedUsersListView.ItemsSource = filteredApprovedUsersList;
        }

        private async void LoadUsers()
        {
            try
            {
                var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

                List<UsersModel> approvedUsers = new List<UsersModel>();

                foreach (var user in users)
                {
                    if (user.Object.Status == "Banned" && user.Object.BanEndDate.HasValue)
                    {
                        if (DateTime.UtcNow >= user.Object.BanEndDate.Value)
                        {
                            await firebaseClient
                                .Child("Users")
                                .Child(user.Key)
                                .PatchAsync(new { Status = "Approved", BanEndDate = (DateTime?)null });

                            user.Object.Status = "Approved";
                        }
                    }

                    if (user.Object.Status == "Approved" && !IsUserDeleted(user.Object.Email))
                    {
                        user.Object.IsExpanded = false;

                        // Handle missing DateApproved
                        if (string.IsNullOrEmpty(user.Object.DateApproved))
                        {
                            user.Object.DateApproved = DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }

                        approvedUsers.Add(user.Object);
                    }
                }

                // ✅ Sort by DateApproved descending
                approvedUsers = approvedUsers
                    .OrderByDescending(u => DateTime.TryParse(u.DateApproved, out DateTime dt) ? dt : DateTime.MinValue)
                    .ToList();

                approvedUsersList = approvedUsers;
                filteredApprovedUsersList.Clear();
                foreach (var user in approvedUsersList)
                {
                    filteredApprovedUsersList.Add(user);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredApprovedUsersList.Clear();
                foreach (var user in approvedUsersList)
                {
                    filteredApprovedUsersList.Add(user);
                }
            }
            else
            {
                var filtered = approvedUsersList
                    .Where(user => user.FullName.ToLower().Contains(searchText) ||
                                   user.Email.ToLower().Contains(searchText) ||
                                   user.UserType.ToLower().Contains(searchText))
                    .ToList();

                filteredApprovedUsersList.Clear();
                foreach (var user in filtered)
                {
                    filteredApprovedUsersList.Add(user);
                }
            }
        }

        private void OnUserTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is UsersModel tappedUser)
            {
                tappedUser.IsExpanded = !tappedUser.IsExpanded;
                approvedUsersListView.ItemsSource = null;
                approvedUsersListView.ItemsSource = filteredApprovedUsersList;
            }
        }

        private async void OnBanUserClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is ImageButton button && button.CommandParameter is UsersModel user)
                {
                    bool confirmBan = await DisplayAlert("Ban User", $"Are you sure you want to ban {user.FullName}?", "Yes", "No");
                    if (!confirmBan) return;

                    string action = await DisplayActionSheet("Select Ban Duration", "Cancel", null, "1 Day", "3 Days", "7 Days", "Permanently");

                    DateTime? banEndDate = null;

                    switch (action)
                    {
                        case "1 Day":
                            banEndDate = DateTime.UtcNow.AddDays(1);
                            break;
                        case "3 Days":
                            banEndDate = DateTime.UtcNow.AddDays(3);
                            break;
                        case "7 Days":
                            banEndDate = DateTime.UtcNow.AddDays(7);
                            break;
                        case "Permanently":
                            banEndDate = null;
                            break;
                        default:
                            return;
                    }

                    // ✅ Ask for Ban Reason
                    string banReason = await DisplayPromptAsync("Ban Reason", "Enter the reason for banning this user:", "OK", "Cancel", placeholder: "Enter reason...");
                    if (string.IsNullOrWhiteSpace(banReason))
                    {
                        banReason = "No reason provided.";
                    }

                    var userNode = (await firebaseClient
                        .Child("Users")
                        .OnceAsync<UsersModel>())
                        .FirstOrDefault(u => u.Object.Email == user.Email);

                    if (userNode == null)
                    {
                        await DisplayAlert("Error", "User not found in database.", "OK");
                        return;
                    }

                    string userKey = userNode.Key;

                    // ✅ Save Ban Reason to Firebase
                    await firebaseClient
                        .Child("Users")
                        .Child(userKey)
                        .PatchAsync(new
                        {
                            Status = "Banned",
                            BanEndDate = banEndDate,
                            BanReason = banReason  // <<< save ban reason
                });

                    // ✅ Push notification via IPushNotificationService
                    var pushService = DependencyService.Get<IPushNotificationService>();
                    string notificationTitle = "Account Banned";
                    string notificationBody = banEndDate.HasValue
                        ? $"You have been banned until {banEndDate.Value:MMMM dd, yyyy}. Reason: {banReason}"
                        : $"You have been permanently banned. Reason: {banReason}";

                    await pushService.SendPushNotificationAsync(notificationTitle, notificationBody, user.DeviceToken);

                    await DisplayAlert("Success", $"User has been banned {(banEndDate.HasValue ? $"until {banEndDate.Value:MMMM dd, yyyy}" : "permanently")}.", "OK");

                    LoadUsers();
                }
                else
                {
                    await DisplayAlert("Error", "User data not found!", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
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

        // ✅ Updated Delete Logic: Remove user from UI but keep them in Firebase
        private async void OnDeleteUserClicked(object sender, System.EventArgs e)
        {
            var button = sender as ImageButton;
            var user = button.BindingContext as UsersModel;

            if (user == null) return;

            bool confirm = await DisplayAlert("Confirm", $"Are you sure you want to delete {user.FullName}?", "Yes", "No");
            if (!confirm) return;

            // Mark user as deleted locally
            MarkUserAsDeleted(user.Email);

            // Remove from UI list
            approvedUsersList.Remove(user);
            filteredApprovedUsersList.Remove(user);

            await DisplayAlert("Success", "User has been removed from the UI but still exists in Firebase.", "OK");
        }

        // ✅ Store deleted users in Preferences (local storage)
        private void MarkUserAsDeleted(string email)
        {
            var deletedUsers = Preferences.Get("DeletedUsers", string.Empty);
            var updatedDeletedUsers = string.IsNullOrEmpty(deletedUsers) ? email : deletedUsers + "," + email;
            Preferences.Set("DeletedUsers", updatedDeletedUsers);
        }

        // ✅ Check if user is marked as deleted
        private bool IsUserDeleted(string email)
        {
            var deletedUsers = Preferences.Get("DeletedUsers", string.Empty);
            var deletedList = deletedUsers.Split(',').ToList();
            return deletedList.Contains(email);
        }
    }
}
