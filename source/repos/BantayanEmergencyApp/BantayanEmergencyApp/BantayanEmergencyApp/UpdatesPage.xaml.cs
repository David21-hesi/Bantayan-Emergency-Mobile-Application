using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UpdatesPage : ContentPage
    {
        private string loggedInUserEmail;
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private ObservableCollection<NotificationModel> notificationsList = new ObservableCollection<NotificationModel>();
        private ObservableCollection<NotificationModel> filteredNotificationsList = new ObservableCollection<NotificationModel>();

        public UpdatesPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email.Replace(".", "_"); // Firebase doesn't allow "." in keys
            NotificationsListView.ItemsSource = filteredNotificationsList;
            LoadNotifications();
        }

        private async void LoadNotifications()
        {
            try
            {
                // Get notifications that this user has deleted
                var deletedSnapshot = await firebaseClient
                    .Child("DeletedNotifications")
                    .Child(loggedInUserEmail)
                    .OnceAsync<object>(); // safer: treat value as object first

                var deletedNotificationIds = deletedSnapshot
                    .Where(d => d.Object is bool b && b) // only true values
                    .Select(d => d.Key)
                    .ToList();

                // Fetch all notifications
                var notifications = await firebaseClient
                    .Child("Notifications")
                    .OnceAsync<NotificationModel>();

                notificationsList.Clear();
                filteredNotificationsList.Clear();

                foreach (var item in notifications)
                {
                    var notification = item.Object;
                    notification.NotificationId = item.Key;

                    // Exclude notifications deleted by this user
                    if ((notification.RecipientGroup == "Residents" || notification.RecipientGroup == "All Users") &&
                        !deletedNotificationIds.Contains(notification.NotificationId))
                    {
                        notificationsList.Add(notification);
                        filteredNotificationsList.Add(notification);
                    }
                }

                // Subscribe to real-time updates
                firebaseClient.Child("Notifications")
                    .AsObservable<NotificationModel>()
                    .Subscribe(d =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            if (d.Object != null && (d.Object.RecipientGroup == "Residents" || d.Object.RecipientGroup == "All Users"))
                            {
                                var existingNotification = notificationsList.FirstOrDefault(n => n.NotificationId == d.Key);

                                if (existingNotification != null)
                                {
                                    if (deletedNotificationIds.Contains(d.Key))
                                    {
                                        // Remove if it's deleted for this user
                                        notificationsList.Remove(existingNotification);
                                        filteredNotificationsList.Remove(existingNotification);
                                    }
                                    else
                                    {
                                        // Update existing notification details
                                        existingNotification.Message = d.Object.Message;
                                        existingNotification.Timestamp = d.Object.Timestamp;
                                    }
                                }
                                else if (!deletedNotificationIds.Contains(d.Key))
                                {
                                    // Add new notification if it's not deleted for this user
                                    d.Object.NotificationId = d.Key;
                                    notificationsList.Insert(0, d.Object);
                                    filteredNotificationsList.Insert(0, d.Object);
                                }
                            }
                        });
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load notifications: " + ex.Message, "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue.ToLower();

            filteredNotificationsList.Clear();
            foreach (var notification in notificationsList)
            {
                if (notification.Message.ToLower().Contains(searchText))
                {
                    filteredNotificationsList.Add(notification);
                }
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var imageButton = (ImageButton)sender;
            var notification = imageButton.CommandParameter as NotificationModel;

            if (notification == null) return;

            bool confirm = await DisplayAlert("Delete", "Are you sure you want to delete this notification?", "Yes", "No");
            if (!confirm) return;

            try
            {
                // Mark this notification as deleted for this user only
                await firebaseClient
                    .Child("DeletedNotifications")
                    .Child(loggedInUserEmail)
                    .Child(notification.NotificationId)
                    .PutAsync(true);

                // Remove from UI lists
                notificationsList.Remove(notification);
                filteredNotificationsList.Remove(notification);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to delete notification: " + ex.Message, "OK");
            }
        }

    }
}
