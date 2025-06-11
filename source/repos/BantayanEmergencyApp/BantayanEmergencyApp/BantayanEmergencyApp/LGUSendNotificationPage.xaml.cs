using BantayanEmergencyApp.Services;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class LGUSendNotificationPage : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private string loggedInUserEmail;

        public LGUSendNotificationPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
        }

        private async void OnSendNotificationClicked(object sender, EventArgs e)
        {
            string recipientGroup = recipientPicker.SelectedItem as string;
            string messageContent = notificationEditor.Text?.Trim();

            if (string.IsNullOrEmpty(recipientGroup))
            {
                await DisplayAlert("Warning", "Please select recipients.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(messageContent))
            {
                await DisplayAlert("Warning", "Message cannot be empty.", "OK");
                return;
            }

            try
            {
                // ✅ Fetch the logged-in user's UserType from Firebase
                var userSnapshot = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var loggedInUser = userSnapshot
                    .FirstOrDefault(u => u.Object.Email == loggedInUserEmail)?.Object;

                if (loggedInUser == null)
                {
                    await DisplayAlert("Error", "Failed to find logged-in user info.", "OK");
                    return;
                }

                string senderRole = loggedInUser.UserType;
                var timestamp = DateTime.Now.ToString("MMMM dd, yyyy h:mmtt");

                // Save the notification
                var response = await firebaseClient
                    .Child("Notifications")
                    .PostAsync(new NotificationModel
                    {
                        Message = messageContent,
                        RecipientGroup = recipientGroup,
                        Timestamp = timestamp,
                        SenderRole = senderRole
                    });

                string generatedId = response.Key;

                // Update with ID
                await firebaseClient
                    .Child("Notifications")
                    .Child(generatedId)
                    .PutAsync(new NotificationModel
                    {
                        NotificationId = generatedId,
                        Message = messageContent,
                        RecipientGroup = recipientGroup,
                        Timestamp = timestamp,
                        SenderRole = senderRole
                    });

                // Send push notification
                await SendPushToSelectedGroup(recipientGroup, messageContent);

                await DisplayAlert("Success", "Notification sent successfully!", "OK");
                recipientPicker.SelectedIndex = -1;
                notificationEditor.Text = string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to send notification: " + ex.Message, "OK");
            }
        }

        private async Task SendPushToSelectedGroup(string group, string message)
        {
            var usersSnapshot = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

            IEnumerable<UsersModel> targetUsers = new List<UsersModel>();

            if (group == "Residents")
            {
                targetUsers = usersSnapshot
                    .Where(u => u.Object.UserType == "Resident" && u.Object.Status == "Approved")
                    .Select(u => u.Object);
            }
            else if (group == "Authorities")
            {
                var authorityTypes = new List<string> { "Police", "Banelco", "Fire Department", "Medical Team" };

                targetUsers = usersSnapshot
                    .Where(u => authorityTypes.Contains(u.Object.UserType) && u.Object.Status == "Approved")
                    .Select(u => u.Object);
            }
            else if (group == "All Users")
            {
                targetUsers = usersSnapshot
                    .Where(u => u.Object.Status == "Approved")
                    .Select(u => u.Object);
            }

            foreach (var user in targetUsers)
            {
                if (!string.IsNullOrWhiteSpace(user.DeviceToken))
                {
                    await DependencyService.Get<IPushNotificationService>()
                        .SendPushNotificationAsync("Emergency Notification", message, user.DeviceToken);
                }
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new LGURecordsNotifications(loggedInUserEmail));
        }
    }
}
