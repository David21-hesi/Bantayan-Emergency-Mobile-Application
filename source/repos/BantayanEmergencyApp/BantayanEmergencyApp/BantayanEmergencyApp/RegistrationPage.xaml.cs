using System;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using Xamarin.Essentials;
using BantayanEmergencyApp.Tokens;

namespace BantayanEmergencyApp
{
    public partial class RegistrationPage : ContentPage
    {
        UserRepository userRepository = new UserRepository();
        private string validIdPath = null;
        private string validIdBackPath = null; // ✅ Added for back of ID
        private string facePicturePath = null;
        private string certificateImagePath = null;

        public RegistrationPage()
        {
            InitializeComponent();
        }

        private async void buttonRegister_Clicked(object sender, EventArgs e)
        {
            try
            {
                string fullname = fullnameEntry.Text?.Trim();
                string email = emailaddressEntry.Text?.Trim();
                string contactnumber = contactnumberEntry.Text?.Trim();
                string password = passwordEntry.Text;
                string confirmpassword = confirmpasswordEntry.Text;
                string address = PickerAddress.SelectedItem?.ToString();
                string usertype = PickerUserType.SelectedItem?.ToString();

                // 🚨 VALIDATION CHECKS 🚨
                if (string.IsNullOrEmpty(fullname) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(contactnumber) || string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(confirmpassword) || string.IsNullOrEmpty(address) ||
                    string.IsNullOrEmpty(usertype) || string.IsNullOrEmpty(validIdPath) ||
                    string.IsNullOrEmpty(validIdBackPath) || string.IsNullOrEmpty(facePicturePath)) // ✅ added back ID check
                {
                    await DisplayAlert("Warning", "All fields are required including Valid ID (front and back) and Face Picture.", "OK");
                    return;
                }

                // Email Validation
                if (!IsValidEmail(email))
                {
                    await DisplayAlert("Warning", "Please enter a valid email address. It should follow the standard format (example@example.com).", "OK");
                    return;
                }

                // Contact Number Validation
                if (!Regex.IsMatch(contactnumber, @"^\d{10,11}$"))
                {
                    await DisplayAlert("Warning", "Invalid contact number. It should be 10-11 digits and consist of only numbers.", "OK");
                    return;
                }

                // Password Validation
                if (!IsValidPassword(password))
                {
                    await DisplayAlert("Warning", "Password must be at least 8 characters long and include an uppercase letter, lowercase letter, number, and special character.", "OK");
                    return;
                }

                // Confirm Password Validation
                if (password != confirmpassword)
                {
                    await DisplayAlert("Warning", "Passwords do not match. Please re-enter the passwords.", "OK");
                    return;
                }

                // Check for Existing Users
                UsersModel existingUser = await userRepository.GetUserByEmail(email);
                UsersModel existingContact = await userRepository.GetUserByContactNumber(contactnumber);

                if (existingUser != null)
                {
                    if (existingUser.Status == "Approved" || existingUser.Status == "Pending")
                    {
                        await DisplayAlert("Warning", "This email is already registered and cannot be used. Please use a different email address.", "OK");
                        return;
                    }
                    else if (existingUser.Status == "Rejected")
                    {
                        await DisplayAlert("Notice", "This email was previously rejected during registration. If you believe this was a mistake, please contact the admin or register using a different email address.", "OK");
                        return;
                    }
                }

                if (existingContact != null)
                {
                    if (existingContact.Status == "Approved" || existingContact.Status == "Pending")
                    {
                        await DisplayAlert("Warning", "This contact number is already registered and cannot be used. Please use another contact number.", "OK");
                        return;
                    }
                }

                // 📲 GET DEVICE TOKEN
                string deviceToken = await DependencyService.Get<IPushTokenService>().GetDeviceTokenAsync();

                if (string.IsNullOrEmpty(deviceToken))
                {
                    await DisplayAlert("Warning", "Device token not available yet. Please try again in a few moments.", "OK");
                    return;
                }

                // ✅ Registration Process
                bool isSave = await userRepository.Register(
                    email, fullname, password, contactnumber,
                    facePicturePath, address, usertype, validIdPath, deviceToken, validIdBackPath, certificateImagePath); // ✅ use validIdBackPath

                if (isSave)
                {
                    await DisplayAlert("Success",
                        "Registration Successful! Please wait for admin approval. If the admin has any questions or requires further clarification regarding your registration, they will contact you via phone.",
                        "OK");
                    ClearFields();
                    await Navigation.PushModalAsync(new AboutPage());
                }

                else
                {
                    await DisplayAlert("Failed", "Registration Failed! Please try again.", "OK");
                }
            }
            catch (Exception exception)
            {
                await DisplayAlert("Error", "An error occurred: " + exception.Message, "OK");
            }
        }

        private bool IsValidEmail(string email)
        {
            // Improved email regex for better validation (e.g., handling uncommon characters)
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidPassword(string password)
        {
            // Ensure password contains at least one uppercase letter, one lowercase letter, one digit, one special character, and is at least 8 characters long.
            return Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_]).{8,}$");
        }

        // Ensure the uploaded ID is a valid image
        private bool IsImageFile(string filePath)
        {
            string[] validExtensions = { ".jpg", ".jpeg", ".png" };
            string extension = System.IO.Path.GetExtension(filePath)?.ToLower();
            return Array.Exists(validExtensions, ext => ext == extension);
        }

        // Custom validation to check face picture (approximate based on resolution)
        private bool IsFacePictureValid(string facePicturePath)
        {
            var facePic = ImageSource.FromFile(facePicturePath);
            // Validate image size or resolution (for example, check pixel dimensions if you need)
            return true; // Simple placeholder, modify based on your logic.
        }

        private async void ToPickValidID(object sender, EventArgs e)
        {
            try
            {
                var validId = await MediaPicker.PickPhotoAsync();

                if (validId != null)
                {
                    if (!IsImageFile(validId.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    validIdPath = validId.FullPath;
                    validIdImage.Source = ImageSource.FromFile(validId.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload a valid ID: " + ex.Message, "OK");
            }
        }

        private async void ToPickValidIDBack(object sender, EventArgs e)
        {
            try
            {
                var validIdBack = await MediaPicker.PickPhotoAsync();

                if (validIdBack != null)
                {
                    if (!IsImageFile(validIdBack.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    validIdBackPath = validIdBack.FullPath; // ✅ set back path
                    validIdBackImage.Source = ImageSource.FromFile(validIdBack.FullPath); // ✅ assumes you have validIdBackImage in XAML
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload the back of valid ID: " + ex.Message, "OK");
            }
        }

        private async void ToPickFacePicture(object sender, EventArgs e)
        {
            try
            {
                var facePicture = await MediaPicker.PickPhotoAsync();

                if (facePicture != null)
                {
                    if (!IsImageFile(facePicture.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    facePicturePath = facePicture.FullPath;
                    facePictureImage.Source = ImageSource.FromFile(facePicture.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload face picture: " + ex.Message, "OK");
            }
        }
        private async void certificateImage_Clicked(object sender, EventArgs e)
        {
            try
            {
                var certificateImage = await MediaPicker.PickPhotoAsync();

                if (certificateImage != null)
                {
                    if (!IsImageFile(certificateImage.FullPath))
                    {
                        await DisplayAlert("Error", "Invalid file type. Please upload an image file (JPG, PNG).", "OK");
                        return;
                    }

                    certificateImagePath = certificateImage.FullPath;
                    certificatePictureImage.Source = ImageSource.FromFile(certificateImage.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to upload face picture: " + ex.Message, "OK");
            }
        }

        private void ClearFields()
        {
            fullnameEntry.Text = string.Empty;
            emailaddressEntry.Text = string.Empty;
            contactnumberEntry.Text = string.Empty;
            passwordEntry.Text = string.Empty;
            confirmpasswordEntry.Text = string.Empty;
            PickerAddress.SelectedItem = null;
            PickerUserType.SelectedItem = null;
            validIdPath = null;
            validIdBackPath = null;
            validIdImage.Source = "placeholder.png";
            validIdBackImage.Source = "placeholder.png"; // ✅ assumes placeholder
            facePicturePath = null;
            facePictureImage.Source = "placeholder.png";
        }

        private bool isPasswordHidden = true;
        private bool isConfirmPasswordHidden = true;

        private void TogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordHidden = !isPasswordHidden;
            passwordEntry.IsPassword = isPasswordHidden;
            togglePasswordButton.Source = isPasswordHidden ? "eye.png" : "hidden.png";
        }

        private void ToggleConfirmPasswordVisibility(object sender, EventArgs e)
        {
            isConfirmPasswordHidden = !isConfirmPasswordHidden;
            confirmpasswordEntry.IsPassword = isConfirmPasswordHidden;
            toggleConfirmPasswordButton.Source = isConfirmPasswordHidden ? "eye.png" : "hidden.png";
        }

    }
}
