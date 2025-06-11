using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android.Util;
using Android;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Firebase;
using Firebase.Messaging;
using Plugin.FirebasePushNotification; 

namespace BantayanEmergencyApp.Droid
{
    [Activity(Label = "BantayanEmergencyApp", Icon = "@mipmap/BANTAYAN", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // 🔔 Request POST_NOTIFICATIONS permission (Android 13+)
            if ((int)Build.VERSION.SdkInt >= 33) // Android 13 (Tiramisu)
            {
                const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
                if (ContextCompat.CheckSelfPermission(this, PostNotificationsPermission) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { PostNotificationsPermission }, 1001);
                }
            }


            // Initialize Firebase
            FirebaseApp.InitializeApp(this);

            // Register for push notifications
            CrossFirebasePushNotification.Current.RegisterForPushNotifications();

            // Process any notification tapped while app was closed
            FirebasePushNotificationManager.ProcessIntent(this, Intent);

            // Attempt to print the current token (may be null at first)
            var token = CrossFirebasePushNotification.Current.Token;
            if (!string.IsNullOrEmpty(token))
            {
                Log.Debug("FCM", $"Current Token: {token}");
            }
            else
            {
                Log.Debug("FCM", "Token not yet available.");
            }

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App());

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("general", "General", NotificationImportance.High)
                {
                    Description = "General notifications"
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            FirebasePushNotificationManager.ProcessIntent(this, intent);
        }
    }
}
