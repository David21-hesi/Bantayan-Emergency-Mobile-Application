using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BantayanEmergencyApp.Tokens
{
    public interface IPushTokenService
    {
        Task<string> GetDeviceTokenAsync();
    }

}
