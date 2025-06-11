using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.IO;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.Generic;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ResidentsPage : TabbedPage
    {
        private string loggedInUserEmail;
        UserRepository userRepository = new UserRepository();
        private string imageUrl = null;

        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        private ObservableCollection<NotificationModel> notificationsList =
            new ObservableCollection<NotificationModel>();

        public ResidentsPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;

            // ✅ Load notifications on start
            LoadNotifications();

            // ✅ Bind ListView to ObservableCollection
            NotificationsListView.ItemsSource = notificationsList;
        }

        // ✅ Override OnBackButtonPressed with fixed async handling
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

        // ✅ Load notifications from Firebase
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
                    if (item.Object.RecipientGroup == "Residents" || item.Object.RecipientGroup == "All Users")
                    {
                        notificationsList.Add(item.Object);
                    }
                }

                firebaseClient.Child("Notifications")
                    .AsObservable<NotificationModel>()
                    .Subscribe(d =>
                    {
                        if (d.Object != null && (d.Object.RecipientGroup == "Residents" || d.Object.RecipientGroup == "All Users"))
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

        // ✅ Select and Upload Image
        // ✅ Select and Save Image Locally
        private async void validIdImage_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();

                if (photo != null)
                {
                    // Check if the file is an image
                    if (!IsImageFile(photo.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    // ✅ Save Image Locally
                    string localPath = Path.Combine(FileSystem.AppDataDirectory, Path.GetFileName(photo.FullPath));
                    File.Copy(photo.FullPath, localPath, true);

                    imageUrl = localPath; // ✅ Assign the image path properly
                    validIdImage.Source = ImageSource.FromFile(localPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload an image: " + ex.Message, "OK");
            }
        }

        // ✅ Validate Image Type
        private bool IsImageFile(string filePath)
        {
            string[] validExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(filePath)?.ToLower();
            return validExtensions.Contains(fileExtension);
        }

        // ✅ Submit Emergency Report
        private async void SubmitEmergencyReport(object sender, EventArgs e)
        {
            try
            {
                string authority = AuthorityPicker.SelectedItem?.ToString();
                string incidentType = IncidentPicker.SelectedItem?.ToString();
                string location = LocationPicker.SelectedItem?.ToString();
                string description = IncidentDescription.Text?.Trim();

                // 🚨 Ensure all fields are filled
                if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(incidentType) ||
                    string.IsNullOrEmpty(location) || string.IsNullOrEmpty(description) ||
                    string.IsNullOrEmpty(imageUrl)) // Image is required
                {
                    await DisplayAlert("Warning", "All fields and image are required.", "OK");
                    return;
                }

                // 🔥 Fetch the logged-in user's details from Firebase
                var users = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                var currentUser = users.FirstOrDefault(u => u.Object.Email == loggedInUserEmail);

                if (currentUser == null)
                {
                    await DisplayAlert("Error", "User details not found.", "OK");
                    return;
                }

                // ✅ Create emergency report object with user's email
                var report = new EmergencyReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    Email = loggedInUserEmail,  // 🔥 Store user's email for matching later
                    Authority = authority,
                    IncidentType = incidentType,
                    Location = location,
                    Description = description,
                    ImageUrl = imageUrl,  // ✅ Local file path (not Firebase)
                    Status = "Awaiting Response",
                    Timestamp = DateTime.UtcNow
                };

                // ✅ Save to Firebase
                await firebaseClient
                    .Child("EmergencyReports")
                    .PostAsync(report);

                await DisplayAlert("Success", "Emergency report submitted successfully!", "OK");

                // ✅ Clear fields after submission
                AuthorityPicker.SelectedIndex = -1;
                IncidentPicker.SelectedIndex = -1;
                LocationPicker.SelectedIndex = -1;
                IncidentDescription.Text = string.Empty;
                validIdImage.Source = "placeholder.png"; // Reset image
                imageUrl = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to submit report: " + ex.Message, "OK");
            }
        }

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
            if (confirm)
            {
                Preferences.Remove("UserEmail"); // Clear stored email
                Preferences.Remove("UserRole");  // Clear role (if stored)

                // Navigate back to Login Page
                Application.Current.MainPage = (new LoginPage());
            }
        }


        private async void ViewMyReports_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new ResidentsReportpage());
        }
    }

    public class EmergencyReport : INotifyPropertyChanged
    {
        public string ReportId { get; set; }
        public string Email { get; set; }
        public string Authority { get; set; }
        public string IncidentType { get; set; }
        public string UserType { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Hidden { get; set; } = false;

        public string FullName { get; set; }
        public string ContactNumber { get; set; }

        public string DeviceToken { get; set; }
        public Command DeleteCommand { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string ReportReason { get; set; }

        public string StatusColor { get; set; }
        public Command EditReportCommand { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public ICommand ToggleExpandCommand { get; set; }
        public List<string> HiddenBy { get; set; }

        public EmergencyReport()
        {
            ToggleExpandCommand = new Command(() => IsExpanded = !IsExpanded);
        }

        // ✅ Computed button state properties
        public bool CanAcknowledge => Status == "Awaiting Response" && !IsReportImplemented;
        public bool CanInProgress => (Status == "Awaiting Response" || Status == "Acknowledged") && !IsReportImplemented;
        public bool CanResolve => Status != "Resolved" && !IsReportImplemented;

        // ✅ Add CanReport logic (adjust as needed)
        public bool CanReport => Status != "Reported" && Status != "Resolved";

        public bool IsReportImplemented => Status == "Reported" || Status == "Resolved";

        // ✅ UpdateStatus triggers button state changes
        public void UpdateStatus(string newStatus)
        {
            Status = newStatus;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(CanAcknowledge));
            OnPropertyChanged(nameof(CanInProgress));
            OnPropertyChanged(nameof(CanResolve));
            OnPropertyChanged(nameof(CanReport)); // Add this to notify changes
            OnPropertyChanged(nameof(IsReportImplemented)); // Notify when the report is implemented
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}