using Xamarin.Forms;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using System.Timers;
using BantayanEmergencyApp.Services;

namespace BantayanEmergencyApp
{
    public partial class BannedUsersPage : ContentPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private ObservableCollection<UsersModel> bannedUsersList = new ObservableCollection<UsersModel>();
        private ObservableCollection<UsersModel> filteredBannedUsersList = new ObservableCollection<UsersModel>();
        private Timer countdownTimer;

        public BannedUsersPage()
        {
            InitializeComponent();
            LoadBannedUsers();

            countdownTimer = new Timer(1000);
            countdownTimer.Elapsed += (sender, e) => UpdateRemainingTime();
            countdownTimer.Start();
        }

        private async void LoadBannedUsers()
        {
            try
            {
                var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

                bannedUsersList.Clear();
                filteredBannedUsersList.Clear();

                foreach (var user in users)
                {
                    if (user.Object.Status == "Banned" && (user.Object.IsDeleted == null || user.Object.IsDeleted == false))
                    {
                        bool isPermanentlyBanned = user.Object.BanEndDate == null;
                        TimeSpan remaining = user.Object.BanEndDate.HasValue
                            ? user.Object.BanEndDate.Value - DateTime.UtcNow
                            : TimeSpan.Zero;

                        var bannedUser = new UsersModel
                        {
                            FullName = user.Object.FullName,
                            UserType = user.Object.UserType,
                            Email = user.Object.Email,
                            Status = "Banned",
                            BanEndDate = user.Object.BanEndDate,
                            DeviceToken = user.Object.DeviceToken, // ✅ Use correct property
                            RemainingTime = isPermanentlyBanned
                               ? "Permanently Banned"
                               : $"{remaining.Days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s left"
                        };

                        bannedUsersList.Add(bannedUser);
                        filteredBannedUsersList.Add(bannedUser);
                    }
                }

                bannedUsersListView.ItemsSource = filteredBannedUsersList;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load banned users: {ex.Message}", "OK");
            }
        }

        private async void UpdateRemainingTime()
        {
            var expiredUsers = new List<UsersModel>();

            foreach (var user in bannedUsersList)
            {
                if (user.BanEndDate.HasValue)
                {
                    TimeSpan remaining = user.BanEndDate.Value - DateTime.UtcNow;

                    if (remaining.TotalSeconds <= 0 && user.Status == "Banned")
                    {
                        user.RemainingTime = "Ban Expired";
                        expiredUsers.Add(user);

                        var userNode = (await firebaseClient
                            .Child("Users")
                            .OnceAsync<UsersModel>())
                            .FirstOrDefault(u => u.Object.Email == user.Email);

                        if (userNode != null)
                        {
                            await firebaseClient
                                .Child("Users")
                                .Child(userNode.Key)
                                .PatchAsync(new { Status = "Approved", BanEndDate = (DateTime?)null });

                            user.Status = "Approved";

                            // ✅ Safely send push notification
                            if (!string.IsNullOrEmpty(user.DeviceToken))
                            {
                                await DependencyService.Get<IPushNotificationService>()
                                    .SendPushNotificationAsync("Ban Lifted", "Your ban period has expired. You may now use the app.", user.DeviceToken);
                            }
                        }
                    }
                    else
                    {
                        user.RemainingTime = $"{remaining.Days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s left";
                    }
                }
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                bannedUsersListView.ItemsSource = null;
                bannedUsersListView.ItemsSource = filteredBannedUsersList;
            });
        }

        private async void OnUnbanUserClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.CommandParameter is UsersModel user)
            {
                bool confirm = await DisplayAlert("Unban User", $"Do you want to unban {user.FullName}?", "Yes", "No");
                if (!confirm)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        bannedUsersListView.ItemsSource = null;
                        bannedUsersListView.ItemsSource = filteredBannedUsersList;
                    });
                    return;
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

                await firebaseClient
                    .Child("Users")
                    .Child(userKey)
                    .PatchAsync(new { Status = "Approved", BanEndDate = (DateTime?)null });

                await DisplayAlert("Success", $"{user.FullName} has been unbanned.", "OK");

                // ✅ Safely send push notification
                if (!string.IsNullOrEmpty(user.DeviceToken))
                {
                    await DependencyService.Get<IPushNotificationService>()
                        .SendPushNotificationAsync("Unbanned", "You have been unbanned by the admin. You can now access the app.", user.DeviceToken);
                }

                LoadBannedUsers();
            }
        }

        private async void OnDeleteUserClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.CommandParameter is UsersModel user)
            {
                bool confirm = await DisplayAlert("Delete User", $"Are you sure you want to remove {user.FullName} from the list?", "Yes", "No");
                if (!confirm)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        bannedUsersListView.ItemsSource = null;
                        bannedUsersListView.ItemsSource = filteredBannedUsersList;
                    });
                    return;
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

                await firebaseClient
                    .Child("Users")
                    .Child(userKey)
                    .PatchAsync(new { IsDeleted = true });

                await DisplayAlert("Success", $"{user.FullName} has been removed from the UI but still exists in Firebase.", "OK");

                Device.BeginInvokeOnMainThread(() =>
                {
                    bannedUsersList.Remove(user);
                    filteredBannedUsersList.Remove(user);
                    bannedUsersListView.ItemsSource = null;
                    bannedUsersListView.ItemsSource = filteredBannedUsersList;
                });
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower();
            filteredBannedUsersList.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var user in bannedUsersList)
                {
                    filteredBannedUsersList.Add(user);
                }
            }
            else
            {
                var filtered = bannedUsersList
                    .Where(user => user.FullName.ToLower().Contains(searchText) ||
                                   user.Email.ToLower().Contains(searchText) ||
                                   user.UserType.ToLower().Contains(searchText))
                    .ToList();

                foreach (var user in filtered)
                {
                    filteredBannedUsersList.Add(user);
                }
            }

            bannedUsersListView.ItemsSource = null;
            bannedUsersListView.ItemsSource = filteredBannedUsersList;
        }
    }
}
