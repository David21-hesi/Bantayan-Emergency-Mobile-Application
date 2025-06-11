using Plugin.FirebasePushNotification;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BantayanEmergencyApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new LandingPage();

        }

        protected override void OnStart()
        {

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
