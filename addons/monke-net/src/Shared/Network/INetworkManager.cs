namespace MonkeNet.Shared;

public interface INetworkManager
{
    public enum PacketModeEnum
    {
        Reliable, Unreliable
    }

    public enum NetworkStatisticEnum
    {
        SentBytes, ReceivedBytes, SentPackets, ReceivedPackets,
        PacketLoss, RoundTripTime
    }

    public delegate void ClientConnectedEventHandler(long id);
    public event ClientConnectedEventHandler ClientConnected;

    public delegate void ClientDisconnectedEventHandler(long id);
    public event ClientDisconnectedEventHandler ClientDisconnected;

    public delegate void PacketReceivedEventHandler(long id, byte[] bin);
    public event PacketReceivedEventHandler PacketReceived;

    public void CreateServer(int port, int maxClients = 32);
    public void CreateClient(string address, int port);
    public void SendBytes(byte[] bin, int id, int channel, PacketModeEnum mode);
    public int GetNetworkId();

    #region Statistics
    public int PopStatistic(NetworkStatisticEnum statistic);
    #endregion
}
