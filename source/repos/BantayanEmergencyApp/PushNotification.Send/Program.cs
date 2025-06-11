using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;

namespace PushNotification.Send
{
    class Program
    {
        static void Main(string[] args)
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("private_key.json")
            });

            //var registrationToken = "dSi-oMNHmxI:APA91bERf20su848lfKsDxwiMjeAoKPxO9MwXPa0HY5sLdL3Ep9KH17h_GZ2SvTGh81heIc2OHsBkPRLaIq9IgnrPS-lepO4BySUyx0JfkKqn_mIf5bVtyY";

            var message = new Message()
            {
                Data = new Dictionary<string, string>()
                {
                    {"myData", "1137" },
                },
                //Token = registrationToken,
                Topic ="all",
                Notification = new Notification()
                {
                    Title = "Test from code",
                    Body = "Test from code"
                }
            };

            string response =
                FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
            Console.WriteLine("Succesfully sent a message " + response);
        }

    }
}
