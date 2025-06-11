using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace BantayanEmergencyApp
{
    public class UserRepository
    {
        string webAPIKey = "AIzaSyAkvyDdp4jwmMIZv2B1xV5ewL-iALwHkcM";
        FirebaseAuthProvider authProvider;
        FirebaseClient firebaseClient;

        public UserRepository()
        {
            authProvider = new FirebaseAuthProvider(new FirebaseConfig(webAPIKey));
            firebaseClient = new FirebaseClient("https://emergencyalertapp-26fc4-default-rtdb.firebaseio.com/");
        }

        private async Task<bool> CheckIfEmailExists(string email)
        {
            try
            {
                var existingUser = await firebaseClient
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                return existingUser.Any(user => user.Object.Email == email);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking email: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> Register(string email, string fullname, string password, string contactnumber,string facePicturePath, string address, string usertype,
            string validIdPath, string deviceToken, string validIdBack, string certificateImage)
        {
            try
            {
                bool emailExists = await CheckIfEmailExists(email);
                if (emailExists)
                {
                    throw new Exception("EMAIL_EXISTS");
                }

                var authResult = await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                if (authResult != null)
                {
                    string userId = authResult.User.LocalId;

                    await firebaseClient.Child("Users").Child(userId).PutAsync(new
                    {
                        UserId = userId,  // ✅ Store Firebase User ID
                        FullName = fullname,
                        Email = email,
                        ContactNumber = contactnumber,
                        Address = address,
                        UserType = usertype,
                        ValidID = validIdPath,
                        ValidIDBack = validIdBack,
                        FacePicture = facePicturePath,
                        CertificateImage = certificateImage,
                        Status = "Pending",
                        DeviceToken = deviceToken,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()// ✅ Save token

                    });

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }

        public async Task<UsersModel> GetUserByEmail(string email)
        {
            var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

            return users
                .Where(u => u.Object.Email == email)
                .Select(u => new UsersModel
                {
                    UserId = u.Key,  // ✅ Store Firebase Key
                    FullName = u.Object.FullName,
                    Email = u.Object.Email,
                    ContactNumber = u.Object.ContactNumber,
                    Address = u.Object.Address,
                    UserType = u.Object.UserType,
                    ValidID = u.Object.ValidID,
                    Status = u.Object.Status,
                    RejectionReason = u.Object.RejectionReason,
                    BanReason = u.Object.BanReason,
                    BanEndDate = u.Object.BanEndDate // 🛠️ ADD THIS LINE
        })
                .FirstOrDefault();
        }


        public async Task<UsersModel> GetUserByContactNumber(string contactNumber)
        {
            try
            {
                var query = await firebaseClient // ✅ Fixed
                    .Child("Users")
                    .OnceAsync<UsersModel>();

                return query
                    .Select(item => item.Object)
                    .FirstOrDefault(user => user.ContactNumber == contactNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching user by contact number: " + ex.Message);
                return null;
            }
        }

        public async Task<string> SignIn(string email, string password)
        {
            try
            {
                var authResult = await authProvider.SignInWithEmailAndPasswordAsync(email, password);

                if (authResult.User == null)
                {
                    throw new Exception("Invalid login credentials.");
                }

                return authResult.FirebaseToken;
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine("Login Error: " + ex.Reason);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login Error: " + ex.Message);
                return null;
            }
        }

        public async Task<bool>ResetPassword(string email)
        {
            await authProvider.SendPasswordResetEmailAsync(email);
            return true;
        }
        public async Task<List<UsersModel>> GetAllUsers()
        {
            var users = await firebaseClient.Child("Users").OnceAsync<UsersModel>();

            return users.Select(u => new UsersModel
            {
                UserId = u.Key,
                FullName = u.Object.FullName,
                Email = u.Object.Email,
                ContactNumber = u.Object.ContactNumber,
                Address = u.Object.Address,
                UserType = u.Object.UserType,
                ValidID = u.Object.ValidID,
                Status = u.Object.Status
            }).ToList();
        }
    }
}
