namespace CybontrolX.Interfaces
{
    public interface INetworkService
    {
        bool IsPortOpen(string ipAddress, int port, int timeoutMs);
    }
}
