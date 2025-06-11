using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public partial class ResidentsReportpage : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public ObservableCollection<EmergencyReport> UserReports { get; set; }
        private ObservableCollection<EmergencyReport> AllReports = new ObservableCollection<EmergencyReport>();

        // Track hidden reports (soft delete)
        private List<string> hiddenReports = new List<string>();

        public ResidentsReportpage()
        {
            InitializeComponent();
            UserReports = new ObservableCollection<EmergencyReport>();
            BindingContext = this;
            LoadUserReports();
        }

        // Load only the reports of the currently logged-in user and sort by Timestamp (newest first)
        private async void LoadUserReports()
        {
            try
            {
                string userEmail = Preferences.Get("UserEmail", string.Empty);
                if (string.IsNullOrEmpty(userEmail))
                {
                    await DisplayAlert("Error", "No user is currently logged in.", "OK");
                    return;
                }

                // Load hidden reports from Preferences
                string hiddenReportsString = Preferences.Get("HiddenReports", "");
                hiddenReports = hiddenReportsString.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();

                var reports = await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>();

                UserReports.Clear();
                AllReports.Clear(); // Clear before adding new data

                var sortedUserReports = reports
                    .Select(r =>
                    {
                        var report = r.Object;
                        report.ReportId = r.Key;
                        return report;
                    })
                    .Where(r => r.Email == userEmail && !hiddenReports.Contains(r.ReportId)) // Exclude hidden reports
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                foreach (var report in sortedUserReports)
                {
                    report.StatusColor = GetStatusColor(report.Status);
                    UserReports.Add(report);
                    AllReports.Add(report); // Store in backup list
                }

                reportsListView.ItemsSource = UserReports;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load reports: " + ex.Message, "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue.ToLower(); // Convert input to lowercase for case-insensitive search

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Reset list if search is empty
                UserReports.Clear();
                foreach (var report in AllReports)
                {
                    UserReports.Add(report);
                }
            }
            else
            {
                // Filter reports
                var filteredReports = AllReports
                    .Where(r => r.IncidentType.ToLower().Contains(searchText) ||
                                r.Location.ToLower().Contains(searchText))
                    .ToList();

                UserReports.Clear();
                foreach (var report in filteredReports)
                {
                    UserReports.Add(report);
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

        private async void ToEditResidentReportPage(object sender, EventArgs e)
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
                await Navigation.PushModalAsync(new ResidentEditPage(selectedReport));
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
        private async void OnDeleteReportClicked(object sender, EventArgs e)
        {
            // Check if a report is selected
            if (sender is Button button && button.CommandParameter is EmergencyReport selectedReport)
            {
                // Show a confirmation prompt
                bool confirmDelete = await DisplayAlert("Confirm Delete",
                    $"Are you sure you want to remove this report from the list?",
                    "Yes", "No");

                if (!confirmDelete)
                {
                    return; // User canceled
                }

                try
                {
                    // Add the ReportId to hidden reports list
                    hiddenReports.Add(selectedReport.ReportId);

                    // Save hidden reports locally so they stay hidden even after restart
                    Preferences.Set("HiddenReports", string.Join(",", hiddenReports));

                    // Remove from UI
                    UserReports.Remove(selectedReport);

                    await DisplayAlert("Success", "Report removed from the list.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to remove report: " + ex.Message, "OK");
                }
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Get the logged-in user's email
            string userEmail = Preferences.Get("UserEmail", string.Empty);

            // Ensure email is not empty before navigating
            if (string.IsNullOrEmpty(userEmail))
            {
                await DisplayAlert("Error", "User email not found. Cannot navigate back.", "OK");
                return;
            }

            // Navigate to ResidentsPage with the required email parameter
            await Navigation.PushModalAsync(new Resident2Page(userEmail));
        }
    }

    // StringToBoolConverter added inside the same namespace
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            return false; // Return false if value is null or not a string
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not needed for this use case
        }
    }
}
