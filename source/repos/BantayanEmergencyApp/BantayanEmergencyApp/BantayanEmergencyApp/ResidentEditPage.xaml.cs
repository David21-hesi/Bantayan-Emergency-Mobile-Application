using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ResidentEditPage : ContentPage
    {
        private readonly FirebaseClient firebaseClient =
            new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        private EmergencyReport _reportToEdit;

        public ResidentEditPage()
        {
            InitializeComponent();
        }

        public ResidentEditPage(EmergencyReport report)
        {
            InitializeComponent();
            _reportToEdit = report;
            LoadReportData();
        }

        private void LoadReportData()
        {
            if (_reportToEdit != null)
            {
                authorityPicker.SelectedItem = _reportToEdit.Authority;
                incidentPicker.SelectedItem = _reportToEdit.IncidentType;
                locationPicker.SelectedItem = _reportToEdit.Location;
                descriptionEditor.Text = _reportToEdit.Description ?? "";
                reportImage.Source = !string.IsNullOrEmpty(_reportToEdit.ImageUrl)
                    ? ImageSource.FromFile(_reportToEdit.ImageUrl)
                    : "default_image.png"; // Fallback if no image
                statusLabel.Text = _reportToEdit.Status ?? "Awaiting Response";
            }
        }

        private async void SaveReport_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_reportToEdit == null)
                {
                    await DisplayAlert("Error", "No report to save.", "OK");
                    return;
                }

                // Update report properties with new values
                _reportToEdit.Authority = authorityPicker.SelectedItem?.ToString();
                _reportToEdit.IncidentType = incidentPicker.SelectedItem?.ToString();
                _reportToEdit.Location = locationPicker.SelectedItem?.ToString();
                _reportToEdit.Description = descriptionEditor.Text;
                _reportToEdit.Timestamp = DateTime.UtcNow;

                // ImageUrl is updated via UpdateImageButton_Clicked if changed; otherwise, it remains as is

                // Save to Firebase Realtime Database
                await firebaseClient
                    .Child("EmergencyReports")
                    .Child(_reportToEdit.ReportId)
                    .PutAsync(_reportToEdit);

                await DisplayAlert("Success", "Report updated successfully.", "OK");
                await Navigation.PushModalAsync(new ResidentsReportpage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to save report: " + ex.Message, "OK");
            }
        }

        private async void UpdateImageButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Emergency Evidence"
                });
                if (photo != null)
                {
                    // Update ImageUrl with the new local file path
                    _reportToEdit.ImageUrl = photo.FullPath;
                    reportImage.Source = ImageSource.FromFile(photo.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to pick image: " + ex.Message, "OK");
            }
        }

        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}