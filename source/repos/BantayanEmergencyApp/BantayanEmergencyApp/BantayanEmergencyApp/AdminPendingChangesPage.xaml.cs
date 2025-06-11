using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using BantayanEmergencyApp.Services;

namespace BantayanEmergencyApp
{
    public partial class AdminPendingChangesPage : ContentPage
    {
        private FirebaseClient firebase = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public AdminPendingChangesPage()
        {
            InitializeComponent();
            LoadPendingChanges();
        }

        // Load users with pending contact number changes
        private async void LoadPendingChanges()
        {
            try
            {
                var users = await firebase.Child("Users").OnceAsync<UsersModel>();
                var pendingUsers = new List<UsersModel>();

                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.Object.PendingContactNumber))
                    {
                        user.Object.Key = user.Key; // Assign Firebase key to user object
                        pendingUsers.Add(user.Object);
                    }
                }

                PendingChangesListView.ItemsSource = pendingUsers;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load pending changes: {ex.Message}", "OK");
            }
        }

        // When an admin selects a pending change, show options to approve or reject
        private async void OnPendingChangeSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedUser = e.SelectedItem as UsersModel;
            if (selectedUser == null || string.IsNullOrWhiteSpace(selectedUser.Key))
            {
                await DisplayAlert("Error", "Invalid user selected or missing key.", "OK");
                return;
            }

            bool isApproved = await DisplayAlert("Approve Contact Change",
                                                  $"Do you want to approve the contact number change for {selectedUser.Email}?",
                                                  "Approve", "Reject");

            var pushService = DependencyService.Get<IPushNotificationService>();

            if (isApproved)
            {
                // Approve the change: Update ContactNumber and clear PendingContactNumber
                await firebase.Child("Users").Child(selectedUser.Key).PatchAsync(new
                {
                    ContactNumber = selectedUser.PendingContactNumber,
                    PendingContactNumber = (string)null
                });

                await DisplayAlert("Success", "Contact number approved and updated!", "OK");

                // Send push notification if device token exists
                if (!string.IsNullOrWhiteSpace(selectedUser.DeviceToken))
                {
                    await pushService.SendPushNotificationAsync(
                        "Contact Number Approved",
                        "Your contact number change has been approved and updated.",
                        selectedUser.DeviceToken);
                }
            }
            else
            {
                // Reject the change: Clear PendingContactNumber
                await firebase.Child("Users").Child(selectedUser.Key).PatchAsync(new
                {
                    PendingContactNumber = (string)null
                });

                await DisplayAlert("Rejected", "Contact number change request rejected.", "OK");

                // Send push notification if device token exists
                if (!string.IsNullOrWhiteSpace(selectedUser.DeviceToken))
                {
                    await pushService.SendPushNotificationAsync(
                        "Contact Number Rejected",
                        "Your contact number change request has been rejected.",
                        selectedUser.DeviceToken);
                }
            }

            // Reload the pending changes list after approving/rejecting
            LoadPendingChanges();
        }
    }
}
