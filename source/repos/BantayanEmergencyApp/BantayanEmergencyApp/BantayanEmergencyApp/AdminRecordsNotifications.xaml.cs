using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace BantayanEmergencyApp
{
    public partial class AdminRecordsNotifications : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public ObservableCollection<NotificationModel> Notifications { get; set; }
        public ObservableCollection<NotificationModel> FilteredNotifications { get; set; }
        private string searchText = string.Empty;

        public AdminRecordsNotifications()
        {
            InitializeComponent();
            Notifications = new ObservableCollection<NotificationModel>();
            FilteredNotifications = new ObservableCollection<NotificationModel>();
            BindingContext = this;
            LoadNotifications();
        }

        private async void LoadNotifications()
        {
            try
            {
                var notifications = await firebaseClient
                    .Child("Notifications")
                    .OnceAsync<NotificationModel>();

                Notifications.Clear();
                FilteredNotifications.Clear();

                var deletedNotifications = Preferences.Get("DeletedNotifications", string.Empty);
                var deletedList = deletedNotifications.Split(',').ToList();

                var sortedNotifications = notifications
                    .Select(n =>
                    {
                        var notification = n.Object;
                        notification.Timestamp = n.Object.Timestamp;
                        notification.NotificationId = n.Key;
                        return notification;
                    })
                    .OrderByDescending(n => DateTime.Parse(n.Timestamp))
                    .Where(n =>
                        !deletedList.Contains(n.NotificationId) &&
                        (string.IsNullOrEmpty(n.SenderRole) || n.SenderRole == "Admin"))
                    .ToList();

                foreach (var notification in sortedNotifications)
                {
                    Notifications.Add(notification);
                    FilteredNotifications.Add(notification);
                }

                UpdateNoNotificationsVisibility();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load notifications: " + ex.Message, "OK");
            }
        }


        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = e.NewTextValue?.ToLower() ?? string.Empty;
            FilteredNotifications.Clear();

            var filtered = Notifications.Where(n => n.Message.ToLower().Contains(searchText));
            foreach (var notification in filtered)
            {
                FilteredNotifications.Add(notification);
            }

            UpdateNoNotificationsVisibility();
        }

        private async void OnDeleteNotificationClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.CommandParameter is NotificationModel selectedNotification)
            {
                if (string.IsNullOrEmpty(selectedNotification.NotificationId))
                {
                    await DisplayAlert("Error", "Notification ID is missing. Cannot delete.", "OK");
                    return;
                }

                bool confirmDelete = await DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete the notification '{selectedNotification.Message}' sent to '{selectedNotification.RecipientGroup}'?",
                    "Yes", "No");

                if (!confirmDelete)
                {
                    return;
                }

                try
                {
                    var deletedNotifications = Preferences.Get("DeletedNotifications", string.Empty);
                    var deletedList = deletedNotifications.Split(',').ToList();
                    if (!deletedList.Contains(selectedNotification.NotificationId))
                    {
                        deletedList.Add(selectedNotification.NotificationId);
                        Preferences.Set("DeletedNotifications", string.Join(",", deletedList));
                    }

                    Notifications.Remove(selectedNotification);
                    FilteredNotifications.Remove(selectedNotification);

                    UpdateNoNotificationsVisibility();

                    await DisplayAlert("Success", "Notification deleted from UI.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to process delete: " + ex.Message, "OK");
                }
            }
        }

        private void UpdateNoNotificationsVisibility()
        {
            noNotificationsLayout.IsVisible = FilteredNotifications.Count == 0;
            notificationsListView.IsVisible = FilteredNotifications.Count > 0;
        }
    }
}
