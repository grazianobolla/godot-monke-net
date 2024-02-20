using Godot;
using MessagePack;

public partial class ServerClock : Node
{
	private SceneMultiplayer _multiplayer;

	public override void _Ready()
	{
		_multiplayer = GetTree().GetMultiplayer() as SceneMultiplayer;
		_multiplayer.PeerPacket += OnPacketReceived;
	}

	// When we receive a sync packet from a Client, we return it with the current Clock data
	private void OnPacketReceived(long id, byte[] data)
	{
		var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

		if (command is NetMessage.Sync sync)
		{
			sync.ServerTime = GetCurrentTick();
			_multiplayer.SendBytes(MessagePackSerializer.Serialize<NetMessage.ICommand>(sync), (int)id, MultiplayerPeer.TransferModeEnum.Unreliable, 1);
		}
	}

	public int GetCurrentTick()
	{
		return (int)Time.GetTicksMsec();
	}
}
