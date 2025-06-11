using Xamarin.Forms;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace BantayanEmergencyApp
{
    public partial class EmergencyReportsPage : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public ObservableCollection<EmergencyReport> ReportsList { get; set; }
        public ObservableCollection<EmergencyReport> FilteredReportsList { get; set; } // Filtered list for search

        public EmergencyReportsPage()
        {
            InitializeComponent();
            ReportsList = new ObservableCollection<EmergencyReport>();
            FilteredReportsList = new ObservableCollection<EmergencyReport>();
            BindingContext = this; // Set BindingContext for UI binding
            LoadEmergencyReports();
        }

        // Load Emergency Reports from Firebase and sort by Timestamp (newest first)
        private async void LoadEmergencyReports()
        {
            try
            {
                var reports = await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>();

                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                ReportsList.Clear();
                FilteredReportsList.Clear();

                var sortedReports = reports
                    .Select(r => r.Object)
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                foreach (var report in sortedReports)
                {
                    var matchedUser = users.FirstOrDefault(u => u.Object.Email == report.Email);

                    if (matchedUser != null)
                    {
                        report.FullName = matchedUser.Object.FullName;

                        if (string.IsNullOrEmpty(report.ContactNumber))
                        {
                            report.ContactNumber = matchedUser.Object.ContactNumber ?? "N/A";
                        }
                    }
                    else
                    {
                        report.FullName = "Unknown User";

                        if (string.IsNullOrEmpty(report.ContactNumber))
                        {
                            report.ContactNumber = "N/A";
                        }
                    }

                    report.StatusColor = GetStatusColor(report.Status);
                    report.DeleteCommand = new Command(async () => await DeleteReport(report));

                    ReportsList.Add(report);
                    FilteredReportsList.Add(report);
                }

                reportsListView.ItemsSource = FilteredReportsList;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load reports: " + ex.Message, "OK");
            }
        }


        // Search Reports
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? "";

            FilteredReportsList.Clear();
            foreach (var report in ReportsList.Where(r =>
                r.FullName.ToLower().Contains(searchText) ||
                r.IncidentType.ToLower().Contains(searchText) ||
                r.Location.ToLower().Contains(searchText)))
            {
                FilteredReportsList.Add(report);
            }
        }

        // Delete Report
        private async Task DeleteReport(EmergencyReport report)
        {
            bool confirm = await DisplayAlert("Delete", "Are you sure you want to delete this report?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var reportToUpdate = (await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>())
                    .FirstOrDefault(r => r.Object.Timestamp == report.Timestamp);

                if (reportToUpdate != null)
                {
                    string reportKey = reportToUpdate.Key;

                    // ✅ Instead of deleting, update IsDeleted = true
                    await firebaseClient
                        .Child("EmergencyReports")
                        .Child(reportKey)
                        .PatchAsync(new { IsDeleted = true });

                    // ✅ Remove from UI
                    ReportsList.Remove(report);
                    FilteredReportsList.Remove(report);

                    await DisplayAlert("Success", "Report deleted successfully.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Report not found in Firebase.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to delete report: " + ex.Message, "OK");
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

        // Get Status Color
        private string GetStatusColor(string status)
        {
            switch (status)
            {
                case "Awaiting Response": return "#FFA500"; // Orange
                case "In Progress": return "#1E90FF"; // Blue
                case "Resolved": return "#008000"; // Green
                default: return "#808080"; // Gray
            }
        }
    }
}
