using CybontrolX.Interfaces;
using System.Net.Sockets;

namespace CybontrolX.Services
{
    public class NetworkService : INetworkService
    {
        public bool IsPortOpen(string ipAddress, int port, int timeoutMs = 2000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(ipAddress, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
