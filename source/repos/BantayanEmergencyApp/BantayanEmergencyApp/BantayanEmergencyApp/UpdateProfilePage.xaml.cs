using System;
using Xamarin.Forms;
using Xamarin.Essentials;
using Firebase.Database;
using Firebase.Database.Query;
using System.Threading.Tasks;

namespace BantayanEmergencyApp
{
    public partial class UpdateProfilePage : ContentPage
    {
        private string userId;
        private string validIdPath = null;
        private string validIdBackPath = null;
        private string facePicturePath = null;

        private FirebaseClient firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");

        public UpdateProfilePage(string userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadUserDetails();
        }

        private async void LoadUserDetails()
        {
            try
            {
                var user = await firebaseClient
                    .Child("Users")
                    .Child(userId)
                    .OnceSingleAsync<UsersModel>();

                if (user != null)
                {
                    fullnameEntry.Text = user.FullName;
                    emailaddressEntry.Text = user.Email;
                    contactnumberEntry.Text = user.ContactNumber;
                    PickerAddress.SelectedItem = user.Address;
                    PickerUserType.SelectedItem = user.UserType;

                    if (!string.IsNullOrEmpty(user.ValidID))
                    {
                        validIdPath = user.ValidID;
                        validIdImage.Source = ImageSource.FromFile(validIdPath);
                    }

                    if (!string.IsNullOrEmpty(user.ValidIDBack))
                    {
                        validIdBackPath = user.ValidIDBack;
                        validIdBackImage.Source = ImageSource.FromFile(validIdBackPath);
                    }

                    if (!string.IsNullOrEmpty(user.FacePicture))
                    {
                        facePicturePath = user.FacePicture;
                        facePhotoImage.Source = ImageSource.FromFile(facePicturePath);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load profile: " + ex.Message, "OK");
            }
        }

        private async void SaveProfileChanges(object sender, EventArgs e)
        {
            try
            {
                string fullname = fullnameEntry.Text;
                string contactnumber = contactnumberEntry.Text;
                string address = PickerAddress.SelectedItem?.ToString();
                string usertype = PickerUserType.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(fullname) || string.IsNullOrEmpty(contactnumber) ||
                    string.IsNullOrEmpty(address) || string.IsNullOrEmpty(usertype))
                {
                    await DisplayAlert("Warning", "Please fill in all fields.", "OK");
                    return;
                }

                await firebaseClient
                    .Child("Users")
                    .Child(userId)
                    .PatchAsync(new
                    {
                        FullName = fullname,
                        ContactNumber = contactnumber,
                        Address = address,
                        UserType = usertype,
                        ValidID = validIdPath,
                        ValidIDBack = validIdBackPath,
                        FacePicture = facePicturePath
                    });

                await DisplayAlert("Success", "Profile updated successfully!", "OK");
                await Navigation.PushModalAsync(new LoginPage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to update profile: " + ex.Message, "OK");
            }
        }

        private async void ToPickValidID(object sender, EventArgs e)
        {
            try
            {
                var validId = await MediaPicker.PickPhotoAsync();
                if (validId != null)
                {
                    validIdPath = validId.FullPath;
                    validIdImage.Source = ImageSource.FromFile(validId.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload front ID: " + ex.Message, "OK");
            }
        }

        private async void ToPickValidIDBack(object sender, EventArgs e)
        {
            try
            {
                var backId = await MediaPicker.PickPhotoAsync();
                if (backId != null)
                {
                    validIdBackPath = backId.FullPath;
                    validIdBackImage.Source = ImageSource.FromFile(backId.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload back ID: " + ex.Message, "OK");
            }
        }

        private async void CancelUpdate(object sender, EventArgs e)
        {
                await Navigation.PushModalAsync(new LGU_login());
        }

        private async void ToPickFacePhoto(object sender, EventArgs e)
        {
            try
            {
                var face = await MediaPicker.PickPhotoAsync();
                if (face != null)
                {
                    facePicturePath = face.FullPath;
                    facePhotoImage.Source = ImageSource.FromFile(face.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload face photo: " + ex.Message, "OK");
            }

        }
    }
}
