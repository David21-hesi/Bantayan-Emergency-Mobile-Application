using System.Threading.Tasks;

namespace BantayanEmergencyApp.Services // Use the correct namespace
{
    public interface IPushNotificationService
    {
        Task SendPushNotificationAsync(string title, string body, string deviceToken);

    }
} 