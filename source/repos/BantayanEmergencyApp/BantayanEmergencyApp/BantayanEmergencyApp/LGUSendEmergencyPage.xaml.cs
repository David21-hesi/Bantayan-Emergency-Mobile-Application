using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LGUSendEmergencyPage : ContentPage
    {
        private string loggedInUserEmail;
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private string imageUrl = null;

        private readonly Dictionary<string, List<string>> authorityMap = new Dictionary<string, List<string>>
        {
            { "Fire", new List<string> { "Fire Department", "All Authorities" } },
            { "Crime", new List<string> { "Police", "All Authorities" } },
            { "Medical Emergency", new List<string> { "Medical Team", "All Authorities" } },
            { "Power Outage", new List<string> { "Banelco", "All Authorities" } }
        };

        public LGUSendEmergencyPage(string email)
        {
            InitializeComponent();
            loggedInUserEmail = email;
            IncidentPicker.SelectedIndexChanged += IncidentPicker_SelectedIndexChanged;
        }

        private void IncidentPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedIncident = IncidentPicker.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(selectedIncident) && authorityMap.ContainsKey(selectedIncident))
            {
                var validAuthorities = authorityMap[selectedIncident];
                AuthorityPicker.ItemsSource = validAuthorities;
                AuthorityPicker.SelectedIndex = -1;
            }
            else
            {
                AuthorityPicker.ItemsSource = null;
            }
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
                        await DisplayAlert("Error", "Invalid file type. Please upload an image (JPG, JPEG, PNG).", "OK");
                        return;
                    }

                    string extension = Path.GetExtension(photo.FullPath);
                    string uniqueFilename = $"{Guid.NewGuid()}{extension}";
                    string localPath = Path.Combine(FileSystem.AppDataDirectory, uniqueFilename);

                    File.Copy(photo.FullPath, localPath, true);

                    imageUrl = localPath;
                    validIdImage.Source = ImageSource.FromFile(localPath);
                }
            }
            catch
            {
                await DisplayAlert("Error", "Failed to upload image. Please check app permissions and storage space.", "OK");
            }
        }

        private bool IsImageFile(string filePath)
        {
            string[] validExtensions = { ".jpg", ".jpeg", ".png" };
            string fileExtension = Path.GetExtension(filePath)?.ToLowerInvariant();
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

                if (string.IsNullOrEmpty(incidentType))
                {
                    await DisplayAlert("Warning", "Please select an Incident Type.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(authority))
                {
                    await DisplayAlert("Warning", "Please select an Authority.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(location))
                {
                    await DisplayAlert("Warning", "Please select a Location.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(description))
                {
                    await DisplayAlert("Warning", "Please enter a Description.", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(imageUrl))
                {
                    await DisplayAlert("Warning", "Please upload an Image.", "OK");
                    return;
                }

                if (!authorityMap.TryGetValue(incidentType, out var allowedAuthorities) ||
                    !allowedAuthorities.Contains(authority))
                {
                    await DisplayAlert("Error", $"The selected authority \"{authority}\" cannot handle \"{incidentType}\" incidents.", "OK");
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
                    ReportId = Guid.NewGuid().ToString(),
                    Email = loggedInUserEmail,
                    Authority = authority,
                    IncidentType = incidentType,
                    Location = location,
                    Description = description,
                    ImageUrl = imageUrl,
                    Status = "Awaiting Response",
                    Timestamp = DateTime.Now,
                    UserType = currentUser.Object.UserType
                };

                await firebaseClient
                    .Child("EmergencyReports")
                    .PostAsync(report);

                await DisplayAlert("Success", "Emergency report submitted successfully!", "OK");

                ResetForm();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to submit report: " + ex.Message, "OK");
            }
        }

        private void ResetForm()
        {
            AuthorityPicker.ItemsSource = null;
            AuthorityPicker.SelectedIndex = -1;
            IncidentPicker.SelectedIndex = -1;
            LocationPicker.SelectedIndex = -1;
            IncidentDescription.Text = string.Empty;
            validIdImage.Source = "image.png";
            imageUrl = null;
        }

        private async void ViewMyReports_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new LGUReportsPage(loggedInUserEmail));
        }
    }
}
