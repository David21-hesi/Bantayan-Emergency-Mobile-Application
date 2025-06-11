using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Support.V4.App;
using Android.Util;
using Firebase.Messaging;
using System;

namespace BantayanEmergencyApp.Droid
{
    // 👇 Android 12+ requirement: Exported = true
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "FCM";
        const string CHANNEL_ID = "approve_channel";

        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            Log.Debug("FCM_TOKEN", $"New FCM token: {token}");
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            string title = message.GetNotification()?.Title ?? "No Title";
            string body = message.GetNotification()?.Body ?? "No Body";

            Log.Debug(TAG, $"🚨 Message received: {title} - {body}");

            SendNotification(title, body);
        }

        private void SendNotification(string title, string messageBody)
        {
            var notificationManager = NotificationManagerCompat.From(this);

            // Create notification channel (required for Android 8.0+)
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, "Approval Notifications", NotificationImportance.High)
                {
                    Description = "Used for user approval notifications"
                };
                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }

            // Create a BigTextStyle to make the notification expandable
            var bigTextStyle = new NotificationCompat.BigTextStyle()
                .BigText(messageBody)
                .SetBigContentTitle(title)
                .SetSummaryText("Tap to view details");

            var notificationBuilder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.BANTAYAN)
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.BANTAYAN))
                .SetContentTitle(title)
                .SetContentText(messageBody)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.High)
                .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                .SetStyle(bigTextStyle);

            notificationManager.Notify(new Random().Next(), notificationBuilder.Build());
        }
    }
}
