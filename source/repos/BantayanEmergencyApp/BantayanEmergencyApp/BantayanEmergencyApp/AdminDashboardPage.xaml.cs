using Xamarin.Forms;
using Firebase.Database;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BantayanEmergencyApp
{
    public partial class AdminDashboardPage : FlyoutPage
    {
        FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public AdminDashboardPage()
        {
            InitializeComponent();
            Detail = new NavigationPage(new AdminHomePage()); // Default Page
            IsPresented = false; // Close the flyout after selecting a menu item
        }

        private void OnHomeClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new AdminHomePage()); // Navigate back to Admin Home Page
            IsPresented = false;
        }

        private void OnPendingUsersClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new PendingUsersPage());
            IsPresented = false;
        }

        private void OnApprovedUsersClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new ApprovedUsersPage());
            IsPresented = false;
        }

        private void OnRejectedUsersClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new RejectedUsersPage());
            IsPresented = false;
        }

        private void OnReportsClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new EmergencyReportsPage());
            IsPresented = false;
        }

        private void OnNotificationsClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new SendNotificationsPage());
            IsPresented = false;
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirmLogout = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (confirmLogout)
            {
                // Replace the whole navigation stack
                Application.Current.MainPage = new LoginPage(); 
            }
        }


        private void OnBannedUserClicked(object sender, EventArgs e)
        {
            Detail = new NavigationPage(new BannedUsersPage()); // ✅ Matches the actual file name
            IsPresented = false;
        }

    }
}
