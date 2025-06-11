using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LGUPage : TabbedPage
    {
        private string loggedInUserEmail;
        UserRepository userRepository = new UserRepository();
        private string imageUrl = null;

        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private ObservableCollection<NotificationModel> notificationsList =
            new ObservableCollection<NotificationModel>();
        public ObservableCollection<EmergencyReport> ReportsList { get; set; }

        public LGUPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;

            LoadNotifications();

            NotificationsAuthoritiesListView.ItemsSource = notificationsList;

            ReportsList = new ObservableCollection<EmergencyReport>();
            BindingContext = this;
            LoadEmergencyReports();
        }

        protected override bool OnBackButtonPressed()
        {
            // Call an async helper method and handle the dialog without blocking
            Device.BeginInvokeOnMainThread(async () =>
            {
                bool answer = await DisplayAlert("Confirmation", "Are you sure you want to exit?", "Yes", "No");
                if (answer)
                {
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow(); // Closes the app
                }
            });
            return true; // Prevent back navigation
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

                var reports = await firebaseClient
                    .Child("EmergencyReports")
                    .OnceAsync<EmergencyReport>();

                var sortedReports = reports
                    .Select(r =>
                    {
                        var report = r.Object;
                        report.ReportId = r.Key;

                // Get the user who submitted this report
                var matchedUser = users.FirstOrDefault(u => u.Object.Email == report.Email);
                        report.FullName = matchedUser?.Object.FullName ?? "Unknown User";
                        report.ContactNumber = matchedUser?.Object.ContactNumber ?? "N/A";

                        report.StatusColor = GetStatusColor(report.Status);
                        return report;
                    })
                    // 🔹 Ensure all reports assigned to the authority remain visible regardless of status
                    .Where(r => r.Authority == userType || r.Authority == "All Authorities")
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                foreach (var report in sortedReports)
                {
                    ReportsList.Add(report);
                }

                reportsLGUListView.ItemsSource = ReportsList;
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

        private async void LoadNotifications()
        {
            try
            {
                var notifications = await firebaseClient
                    .Child("Notifications")
                    .OnceAsync<NotificationModel>();

                notificationsList.Clear();

                foreach (var item in notifications)
                {
                    if (item.Object.RecipientGroup == "Authorities" || item.Object.RecipientGroup == "All Users")
                    {
                        notificationsList.Add(item.Object);
                    }
                }

                firebaseClient.Child("Notifications")
                    .AsObservable<NotificationModel>()
                    .Subscribe(d =>
                    {
                        if (d.Object != null && (d.Object.RecipientGroup == "Authorities" || d.Object.RecipientGroup == "All Users"))
                        {
                            if (!notificationsList.Any(n => n.Message == d.Object.Message))
                            {
                                notificationsList.Insert(0, d.Object);
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load notifications: " + ex.Message, "OK");
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

                // 🔹 Update the report status locally without removing it
                var reportToUpdate = ReportsList.FirstOrDefault(r => r.ReportId == reportId);
                if (reportToUpdate != null)
                {
                    reportToUpdate.Status = newStatus;
                    reportToUpdate.StatusColor = GetStatusColor(newStatus);

                    // 🔹 Refresh UI manually
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        reportsLGUListView.ItemsSource = null;
                        reportsLGUListView.ItemsSource = ReportsList;
                    });
                }

                await DisplayAlert("Success", $"Report status updated to {newStatus}.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to update report status: " + ex.Message, "OK");
            }
        }


        private async void OnAcknowledgeClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button.CommandParameter as string;
            await UpdateReportStatus(reportId, "Acknowledged");
        }

        private async void OnInProgressClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button.CommandParameter as string;
            await UpdateReportStatus(reportId, "In Progress");
        }

        private async void OnResolvedClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string reportId = button.CommandParameter as string;
            await UpdateReportStatus(reportId, "Resolved");
        }

        private async void validIdImage_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();

                if (photo != null)
                {
                    if (!IsImageFile(photo.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    string localPath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(photo.FullPath));
                    File.Copy(photo.FullPath, localPath, true);

                    imageUrl = localPath;
                    validIdImage.Source = ImageSource.FromFile(localPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload an image: " + ex.Message, "OK");
            }
        }

        private bool IsImageFile(string filePath)
        {
            string[] validExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(filePath)?.ToLower();
            return validExtensions.Contains(fileExtension);
        }

        private async void SubmitEmergencyReport(object sender, EventArgs e)
        {
            try
            {
                string authority = AuthorityPicker.SelectedItem?.ToString();
                string incidentType = IncidentPicker.SelectedItem?.ToString();
                string location = LocationPicker.SelectedItem?.ToString();
                string description = IncidentDescription.Text?.Trim();

                if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(incidentType) ||
                    string.IsNullOrEmpty(location) || string.IsNullOrEmpty(description) ||
                    string.IsNullOrEmpty(imageUrl))
                {
                    await DisplayAlert("Warning", "All fields and image are required.", "OK");
                    return;
                }

                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var currentUser = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);

                if (currentUser == null)
                {
                    await DisplayAlert("Error", "User details not found.", "OK");
                    return;
                }

                var report = new EmergencyReport
                {
                    ReportId = Guid.NewGuid().ToString(), // This will be overwritten by Firebase key
                    Email = loggedInUserEmail,
                    Authority = authority,
                    IncidentType = incidentType,
                    Location = location,
                    Description = description,
                    ImageUrl = imageUrl,
                    Status = "Awaiting Response",
                    Timestamp = DateTime.UtcNow,
                    FullName = currentUser.Object.FullName,
                    ContactNumber = currentUser.Object.ContactNumber
                };

                var response = await firebaseClient
                    .Child("EmergencyReports")
                    .PostAsync(report);

                report.ReportId = response.Key; // Update with Firebase-generated key
                ReportsList.Insert(0, report); // Add to UI immediately

                await DisplayAlert("Success", "Emergency report submitted successfully!", "OK");

                AuthorityPicker.SelectedIndex = -1;
                IncidentPicker.SelectedIndex = -1;
                LocationPicker.SelectedIndex = -1;
                IncidentDescription.Text = string.Empty;
                validIdImage.Source = "placeholder.png";
                imageUrl = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to submit report: " + ex.Message, "OK");
            }
        }

        private async void ViewMyReports_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new LGUReportsPage(loggedInUserEmail));
        }
    }
}