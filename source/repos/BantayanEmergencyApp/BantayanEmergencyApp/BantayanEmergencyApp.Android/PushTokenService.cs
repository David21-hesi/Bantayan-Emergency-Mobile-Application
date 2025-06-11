using System.Threading.Tasks;
using Firebase.Iid;
using Xamarin.Forms;
using BantayanEmergencyApp.Droid;
using BantayanEmergencyApp.Tokens;

[assembly: Dependency(typeof(PushTokenService))]
namespace BantayanEmergencyApp.Droid
{
    public class PushTokenService : IPushTokenService
    {
        public async Task<string> GetDeviceTokenAsync()
        {
            var instanceId = FirebaseInstanceId.Instance;
            var token = instanceId.Token;
            return await Task.FromResult(token);
        }
    }
}
