using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class LGUReportsPage : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private string loggedInUserEmail;
        public ObservableCollection<EmergencyReport> MyReportsList { get; set; }
        private ObservableCollection<EmergencyReport> allReports; // Store all reports for filtering

        public LGUReportsPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
            MyReportsList = new ObservableCollection<EmergencyReport>();
            allReports = new ObservableCollection<EmergencyReport>();
            lgureportsListView.ItemsSource = MyReportsList;
            LoadMyReports();
        }

        // Load reports of the logged-in authority and sort by Timestamp (newest first)
        private async void LoadMyReports()
        {
            try
            {
                var reports = await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>();

                MyReportsList.Clear();
                allReports.Clear();

                // Filter reports for the logged-in user and sort by Timestamp descending, excluding soft-deleted reports
                var sortedReports = reports
                    .Select(r =>
                    {
                        var report = r.Object;
                        report.ReportId = r.Key; // Assign ReportId from the original key
                        report.StatusColor = GetStatusColor(report.Status);
                        report.ToggleExpandCommand = new Command(() => report.IsExpanded = !report.IsExpanded); // Add toggle command
                        return report;
                    })
                    .Where(r => r.Email == loggedInUserEmail && !r.IsDeleted) // Exclude soft-deleted reports
                    .OrderByDescending(r => r.Timestamp) // Sort by Timestamp, newest first
                    .ToList();

                foreach (var report in sortedReports)
                {
                    MyReportsList.Add(report);
                    allReports.Add(report); // Keep a full copy for search
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load reports: " + ex.Message, "OK");
            }
        }

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

        // Search when pressing the search button
        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            FilterReports(ReportsSearchBar.Text);
        }

        // Real-time search as user types
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterReports(e.NewTextValue);
        }

        private void FilterReports(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                MyReportsList.Clear();
                foreach (var report in allReports.Where(r => !r.IsDeleted)) // Exclude soft-deleted reports
                {
                    MyReportsList.Add(report);
                }
            }
            else
            {
                var filteredReports = allReports
                    .Where(r => !r.IsDeleted && // Exclude soft-deleted reports
                                (r.IncidentType.ToLower().Contains(searchText.ToLower()) ||
                                 r.Location.ToLower().Contains(searchText.ToLower())))
                    .ToList();

                MyReportsList.Clear();
                foreach (var report in filteredReports)
                {
                    MyReportsList.Add(report);
                }
            }

            lgureportsListView.ItemsSource = MyReportsList;
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

        private async void OnDeleteReportClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button?.CommandParameter as string;

            bool confirm = await DisplayAlert("Delete Report", "Are you sure you want to delete this report?", "Yes", "No");
            if (!confirm) return;

            try
            {
                // Find the report in the list
                var reportToRemove = MyReportsList.FirstOrDefault(r => r.ReportId == reportId);
                if (reportToRemove != null)
                {
                    // Set the IsDeleted property to true (soft delete)
                    await firebaseClient
                        .Child("EmergencyReports")
                        .Child(reportToRemove.ReportId)
                        .PatchAsync(new { IsDeleted = true });

                    // Remove from the UI (filtered and full list)
                    MyReportsList.Remove(reportToRemove);
                    allReports.Remove(reportToRemove);

                    await DisplayAlert("Success", "Report deleted successfully.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to delete report: " + ex.Message, "OK");
            }
        }

        private async void ToEditLGUReportPage(object sender, EventArgs e)
        {
            // Check if a report is selected
            if (sender is Button button && button.CommandParameter is EmergencyReport selectedReport)
            {
                // Define statuses that should block editing
                string[] nonEditableStatuses = { "Acknowledged", "In Progress", "Resolved", "Reported" };

                // Check if the selected report's status is in the non-editable list
                if (nonEditableStatuses.Contains(selectedReport.Status))
                {
                    await DisplayAlert("Restricted", "This report cannot be edited as it is already " + selectedReport.Status + ".", "OK");
                    return; // Exit the method, preventing navigation
                }

                // If status is editable (e.g., "Awaiting Response"), proceed to edit page
                await Navigation.PushModalAsync(new LGUEditPage(selectedReport));
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Get the logged-in user's email
            string userEmail = loggedInUserEmail;

            // Ensure email is not empty before navigating
            if (string.IsNullOrEmpty(userEmail))
            {
                await DisplayAlert("Error", "User email not found. Cannot navigate back.", "OK");
                return;
            }

            // Navigate to ResidentsPage with the required email parameter
            await Navigation.PushModalAsync(new LGU2Page(userEmail));
        }

    }
}