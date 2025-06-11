using BantayanEmergencyApp.Services;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReportLGUPage : ContentPage
    {
        private string loggedInUserEmail;
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public ObservableCollection<EmergencyReport> ReportsList { get; set; }
        public ObservableCollection<EmergencyReport> FilteredReports { get; set; }

        public ReportLGUPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
            ReportsList = new ObservableCollection<EmergencyReport>();
            FilteredReports = new ObservableCollection<EmergencyReport>();
            BindingContext = this;
            LoadEmergencyReports();
        }

        private async void LoadEmergencyReports()
        {
            try
            {
                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var loggedInUser = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);
                if (loggedInUser == null || string.IsNullOrEmpty(loggedInUser.Object.UserType))
                {
                    await DisplayAlert("Error", "Failed to retrieve user details.", "OK");
                    return;
                }

                string userType = loggedInUser.Object.UserType;

                ReportsList.Clear();
                FilteredReports.Clear();

                // Fetch reports directly as EmergencyReport objects
                var reports = await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>();

                var sortedReports = reports
                    .Select(r =>
                    {
                        var report = r.Object;
                        report.ReportId = r.Key;
                        report.StatusColor = GetStatusColor(report.Status);
                        report.HiddenBy = report.HiddenBy ?? new List<string>();

                        var matchedUser = users.FirstOrDefault(u => u.Object.Email == report.Email);
                        report.FullName = matchedUser?.Object.FullName ?? "Unknown User";

                        if (string.IsNullOrEmpty(report.ContactNumber))
                        {
                            report.ContactNumber = matchedUser?.Object.ContactNumber ?? "N/A";
                        }

                        return report;
                    })
                    .Where(r =>
                        (r.Authority == userType || r.Authority == "All Authorities") &&
                        !r.HiddenBy.Contains(loggedInUserEmail))
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                foreach (var report in sortedReports)
                {
                    System.Diagnostics.Debug.WriteLine($"Report ID: {report.ReportId}, Timestamp: {report.Timestamp}");
                    ReportsList.Add(report);
                    FilteredReports.Add(report);
                }

                reportsLGUListView.ItemsSource = FilteredReports;
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

        private async Task UpdateReportStatus(string reportId, string newStatus)
        {
            try
            {
                await firebaseClient
                    .Child("EmergencyReports")
                    .Child(reportId)
                    .PatchAsync(new { Status = newStatus });

                var reportToUpdate = ReportsList.FirstOrDefault(r => r.ReportId == reportId);
                if (reportToUpdate != null)
                {
                    reportToUpdate.StatusColor = GetStatusColor(newStatus);
                    reportToUpdate.UpdateStatus(newStatus); // this triggers UI refresh for CanAcknowledge, CanInProgress, CanResolve

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        string searchText = reportSearchBar?.Text?.ToLower() ?? "";
                        FilteredReports.Clear();
                        var filteredResults = ReportsList
                            .Where(r => r.FullName.ToLower().Contains(searchText) ||
                                        r.IncidentType.ToLower().Contains(searchText) ||
                                        r.Location.ToLower().Contains(searchText))
                            .ToList();
                        foreach (var report in filteredResults)
                        {
                            FilteredReports.Add(report);
                        }
                        reportsLGUListView.ItemsSource = null;
                        reportsLGUListView.ItemsSource = FilteredReports;
                    });
                }

                await DisplayAlert("Success", $"Report status updated to {newStatus}.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to update report status: " + ex.Message, "OK");
            }
        }
        private async Task SendPushToReporter(EmergencyReport report, string reason = null)
        {
            try
            {
                if (report == null)
                    return;

                string deviceToken = report.DeviceToken;

                // If DeviceToken in the report is empty, fall back to fetching from Users by email
                if (string.IsNullOrWhiteSpace(deviceToken) && !string.IsNullOrWhiteSpace(report.Email))
                {
                    var userSnapshot = (await firebaseClient.Child("Users").OnceAsync<UsersModel>())
                                        .FirstOrDefault(u => u.Object.Email == report.Email);

                    if (userSnapshot?.Object != null && !string.IsNullOrWhiteSpace(userSnapshot.Object.DeviceToken))
                    {
                        deviceToken = userSnapshot.Object.DeviceToken;
                    }
                }

                // Proceed if we have a valid token
                if (!string.IsNullOrWhiteSpace(deviceToken))
                {
                    var pushService = DependencyService.Get<IPushNotificationService>();

                    string title = "Emergency Report Update";
                    string message = $"Your report has been marked as '{report.Status}' by the authorities.";

                    if (report.Status == "Reported" && !string.IsNullOrWhiteSpace(reason))
                    {
                        message += $"\nReason: {reason}";
                    }

                    Debug.WriteLine($"[PushNotification] Sending to token: {deviceToken}");

                    await pushService.SendPushNotificationAsync(title, message, deviceToken);
                }
                else
                {
                    Debug.WriteLine("Push notification skipped: No valid device token.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Push notification error: " + ex.Message);
            }
        }

        private async void OnAcknowledgeClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button?.CommandParameter as string;
            await UpdateReportStatus(reportId, "Acknowledged");

            var report = await firebaseClient.Child("EmergencyReports").Child(reportId).OnceSingleAsync<EmergencyReport>();
            report.Status = "Acknowledged";
            await SendPushToReporter(report);
        }

        private async void OnInProgressClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button?.CommandParameter as string;
            await UpdateReportStatus(reportId, "In Progress");

            var report = await firebaseClient.Child("EmergencyReports").Child(reportId).OnceSingleAsync<EmergencyReport>();
            report.Status = "In Progress";
            await SendPushToReporter(report);
        }

        private async void OnResolvedClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button?.CommandParameter as string;
            await UpdateReportStatus(reportId, "Resolved");

            var report = await firebaseClient.Child("EmergencyReports").Child(reportId).OnceSingleAsync<EmergencyReport>();
            report.Status = "Resolved";
            await SendPushToReporter(report);
        }

        private async void OnCallClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var contactNumber = button?.CommandParameter.ToString(); // Get the contact number from the button's CommandParameter

            if (!string.IsNullOrEmpty(contactNumber))
            {
                // Display the action sheet to let the user select the contact number
                string action = await DisplayActionSheet("Call User", "Cancel", null, contactNumber);

                if (action != "Cancel" && !string.IsNullOrEmpty(action))
                {
                    // Remove any spaces and call the selected number
                    await Launcher.OpenAsync($"tel:{action.Replace(" ", "")}");
                }
            }
            else
            {
                // If the contact number is null or empty, handle that case
                await DisplayAlert("Error", "Contact number is not available.", "OK");
            }
        }
        private async void OnReportedClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button?.CommandParameter as string;

            // Prompt the authority to enter a reason for reporting
            string reason = await DisplayPromptAsync("Report Emergency", "Please enter the reason for reporting this emergency report:", "Submit", "Cancel");

            if (string.IsNullOrWhiteSpace(reason))
            {
                await DisplayAlert("Error", "You must provide a reason to report.", "OK");
                return;
            }

            // 🛠 Step 1: Load the report
            var report = await firebaseClient.Child("EmergencyReports").Child(reportId).OnceSingleAsync<EmergencyReport>();
            if (report == null)
            {
                await DisplayAlert("Error", "Failed to load report data.", "OK");
                return;
            }

            // 🛠 Step 2: Update its status and save the reason in memory
            report.Status = "Reported";
            report.ReportReason = reason;  // Save the reason to the report

            // 🛠 Step 3: Save the updated report back to Firebase
            await firebaseClient.Child("EmergencyReports").Child(reportId).PutAsync(report);

            // 🛠 Step 4: Send the push notification with the reason
            await SendPushToReporter(report, reason);

            // ✅ Step 5: Show confirmation alert
            await DisplayAlert("Reported", "The emergency report has been reported successfully.", "OK");

            LoadEmergencyReports();
        }


        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? "";

            FilteredReports.Clear();

            var filteredResults = ReportsList
                .Where(r => r.FullName.ToLower().Contains(searchText) ||
                            r.IncidentType.ToLower().Contains(searchText) ||
                            r.Location.ToLower().Contains(searchText))
                .ToList();

            foreach (var report in filteredResults)
            {
                FilteredReports.Add(report);
            }

            reportsLGUListView.ItemsSource = null;
            reportsLGUListView.ItemsSource = FilteredReports;
        }

        private async void OnImageTapped(object sender, EventArgs e)
        {
            if (sender is Image image && e is TappedEventArgs tappedEventArgs)
            {
                var imageUrl = tappedEventArgs.Parameter?.ToString();

                if (!string.IsNullOrEmpty(imageUrl))
                {
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

                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (s, args) =>
                    {
                        await Navigation.PopModalAsync();
                    };
                    fullScreenPage.Content.GestureRecognizers.Add(tapGesture);

                    await Navigation.PushModalAsync(new NavigationPage(fullScreenPage)
                    {
                        BarBackgroundColor = Color.Black,
                        BarTextColor = Color.White
                    }, false);
                }
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            var reportId = button?.CommandParameter as string;

            if (string.IsNullOrEmpty(reportId))
            {
                await DisplayAlert("Error", "Invalid report ID.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete Report",
                "Are you sure you want to remove this report from your view?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                var report = (await firebaseClient
                    .Child("EmergencyReports")
                    .Child(reportId)
                    .OnceSingleAsync<EmergencyReport>());

                if (report == null)
                {
                    await DisplayAlert("Error", "Report not found.", "OK");
                    return;
                }

                report.HiddenBy = report.HiddenBy ?? new List<string>();

                if (!report.HiddenBy.Contains(loggedInUserEmail))
                {
                    report.HiddenBy.Add(loggedInUserEmail);

                    await firebaseClient
                        .Child("EmergencyReports")
                        .Child(reportId)
                        .PatchAsync(new { HiddenBy = report.HiddenBy });

                    var updatedReport = await firebaseClient
                        .Child("EmergencyReports")
                        .Child(reportId)
                        .OnceSingleAsync<EmergencyReport>();

                    if (updatedReport.HiddenBy == null || !updatedReport.HiddenBy.Contains(loggedInUserEmail))
                    {
                        await DisplayAlert("Error", "Failed to update HiddenBy list in Firebase.", "OK");
                        return;
                    }

                    var reportToRemove = ReportsList.FirstOrDefault(r => r.ReportId == reportId);
                    if (reportToRemove != null)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ReportsList.Remove(reportToRemove);
                            FilteredReports.Remove(reportToRemove);
                            reportsLGUListView.ItemsSource = null;
                            reportsLGUListView.ItemsSource = FilteredReports;
                        });
                    }

                    await DisplayAlert("Success", "Report removed from your view.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to remove report: " + ex.Message, "OK");
            }
        }
    }
}