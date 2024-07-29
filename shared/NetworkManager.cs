using Godot;

/*
	Singleton, abstracts Network handling and events (Both for a server and client)
*/
public partial class NetworkManager : Node
{
	public enum PacketMode
	{
		Reliable, Unreliable
	}

	[Signal] public delegate void PlayerConnectedEventHandler(long id);
	[Signal] public delegate void PlayerDisconnectedEventHandler(long id);
	[Signal] public delegate void PacketReceivedEventHandler(long id, byte[] bin);

	public static NetworkManager Instance { get; private set; }
	private SceneMultiplayer _multiplayer;

	public override void _Ready()
	{
		_multiplayer = Multiplayer as SceneMultiplayer;

		_multiplayer.PeerConnected += OnPeerConnected;
		_multiplayer.PeerDisconnected += OnPeerDisconnected;
		_multiplayer.PeerPacket += OnPacketReceived;

		Instance = this;
	}

	public void CreateServer(int port, int maxClients = 32)
	{
		ENetMultiplayerPeer enet = new();
		enet.CreateServer(port, maxClients);
		_multiplayer.MultiplayerPeer = enet;
		GD.Print($"Created server, Port:{port} Max Clients:{maxClients}");
	}

	public void ConnectToServer(string address, int port)
	{
		ENetMultiplayerPeer enet = new();
		enet.CreateClient(address, port);
		_multiplayer.MultiplayerPeer = enet;
		GD.Print($"Client {_multiplayer.GetUniqueId()} connected to {address}:{port}");
	}

	public void SendBytes(byte[] bin, int id, int channel, PacketMode mode)
	{
		MultiplayerPeer.TransferModeEnum m = mode == PacketMode.Reliable ? MultiplayerPeer.TransferModeEnum.Reliable : MultiplayerPeer.TransferModeEnum.Unreliable;
		_multiplayer.SendBytes(bin, id, m, channel);
	}

	public int GetNetworkId()
	{
		return _multiplayer.GetUniqueId();
	}

	private void OnPeerConnected(long id)
	{
		EmitSignal(SignalName.PlayerConnected, id);
	}

	private void OnPeerDisconnected(long id)
	{
		EmitSignal(SignalName.PlayerDisconnected, id);
	}

	private void OnPacketReceived(long id, byte[] bin)
	{
		EmitSignal(SignalName.PacketReceived, id, bin);
	}
}
