using Android.Content.Res;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using BantayanEmergencyApp.Droid; // Adjust if needed
using BantayanEmergencyApp.Services;
using System;

[assembly: Dependency(typeof(AndroidPushNotificationService))]
namespace BantayanEmergencyApp.Droid
{
    public class AndroidPushNotificationService : IPushNotificationService
    {
        private static bool isInitialized = false;

        public async Task SendPushNotificationAsync(string title, string body, string deviceToken)
        {
            if (!isInitialized)
            {
                AssetManager assets = Android.App.Application.Context.Assets;
                using (var stream = assets.Open("private_key.json"))
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromStream(stream)
                    });
                }
                isInitialized = true;
            }

            var message = new Message()
            {
                Token = deviceToken, // ✅ Send to specific device
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
        {
            { "customData", "value" }
        }
            };

            System.Diagnostics.Debug.WriteLine($"[PushNotification] Sending to: {deviceToken}");
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            System.Diagnostics.Debug.WriteLine("Message sent: " + response);
        }


    }
}