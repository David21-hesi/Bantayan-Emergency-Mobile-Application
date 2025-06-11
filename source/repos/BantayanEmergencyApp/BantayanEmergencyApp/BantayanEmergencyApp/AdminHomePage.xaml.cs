using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;

namespace BantayanEmergencyApp
{
    public partial class AdminHomePage : ContentPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public AdminHomePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCounts();

            // ✅ Subscribe to refresh message
            Xamarin.Forms.MessagingCenter.Subscribe<object>(this, "RefreshCounts", (sender) =>
            {
                LoadCounts();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // ✅ Unsubscribe to avoid duplicate triggers
            Xamarin.Forms.MessagingCenter.Unsubscribe<object>(this, "RefreshCounts");
        }

        private async void LoadCounts()
        {
            try
            {
                var allUsers = await firebaseClient.Child("Users").OnceAsync<UsersModel>();
                var allReports = await firebaseClient.Child("EmergencyReports").OnceAsync<object>();

                int pendingCount = allUsers.Count(u => u.Object.Status == "Pending");
                int approvedCount = allUsers.Count(u => u.Object.Status == "Approved");
                int rejectedCount = allUsers.Count(u => u.Object.Status == "Rejected");
                int bannedCount = allUsers.Count(u => u.Object.Status == "Banned");

                // ✅ Count of users with a pending contact number
                int pendingContactChangesCount = allUsers.Count(u =>
                    !string.IsNullOrEmpty(u.Object.PendingContactNumber));

                // ✅ Filter emergency reports (excluding soft-deleted ones)
                int emergencyReportsCount = allReports
                    .Where(r =>
                    {
                        var reportDict = r.Object as IDictionary<string, object>;
                        return reportDict == null || !reportDict.ContainsKey("status") || reportDict["status"]?.ToString() != "Deleted";
                    })
                    .Count();

                // ✅ Update UI labels
                lblPendingCount.Text = pendingCount.ToString();
                lblApprovedCount.Text = approvedCount.ToString();
                lblRejectedCount.Text = rejectedCount.ToString();
                lblBannedUsers.Text = bannedCount.ToString();
                lblEmergencyReports.Text = emergencyReportsCount.ToString();
                lblPendingContactChanges.Text = pendingContactChangesCount.ToString(); // 👈 Add this line

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load data: " + ex.Message, "OK");
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

        private async void OnPendingUsersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PendingUsersPage());
        }

        private async void OnApprovedUsersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ApprovedUsersPage());
        }

        private async void OnRejectedUsersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RejectedUsersPage());
        }

        private async void OnEmergencyReportsTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmergencyReportsPage());
        }

        private async void OnBannedUsersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BannedUsersPage());
        }

        private async void OnNotificationsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SendNotificationsPage());
        }
        private async void OnContactUsersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminPendingChangesPage());
        }
    }
}
