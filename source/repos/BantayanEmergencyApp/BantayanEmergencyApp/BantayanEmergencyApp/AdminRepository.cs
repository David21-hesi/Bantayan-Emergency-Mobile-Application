using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BantayanEmergencyApp
{
    public class AdminRepository
    {
        string webAPIKEY = "AIzaSyArpRGLUfJ6nOWxDGeVexQgmrjP6_7EP2s";
        FirebaseAuthProvider authProvider;

        public AdminRepository()
        {
            authProvider = new FirebaseAuthProvider(new FirebaseConfig(webAPIKEY));
        }
        public async Task<string> SignIn(string email, string password)
        {
            var token = await authProvider.SignInWithEmailAndPasswordAsync(email, password);
            if (!string.IsNullOrEmpty(token.FirebaseToken))
            {
                return token.FirebaseToken;
            }
            return "";
        }
    }
}
