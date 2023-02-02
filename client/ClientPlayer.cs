using Godot;
using System.Collections.Generic;
using MessagePack;
using NetMessage;

struct PositionHistory
{
    public int Stamp;
    public Vector3 Position;
}

// Wrapper scene spawned by the MultiplayerSpawner
public partial class ClientPlayer : CharacterBody3D
{
    public const int REDUNDANCY_PACKETS = 6;

    private List<byte> _commands = new();
    private List<PositionHistory> _history = new();

    private Vector3 _velocity = Vector3.Zero;
    private int _seqStamp = 0;

    public override void _PhysicsProcess(double delta)
    {
        byte input = PackInput();
        SendInput(input, _seqStamp);
        MoveLocally(input);

        _history.Add(new PositionHistory { Stamp = _seqStamp, Position = this.Position });
        _seqStamp++;
    }

    public void ReceiveState(NetMessage.UserState state)
    {
        _history.RemoveAll(entry => (entry.Stamp < state.Stamp));

        var deviation = _history[0].Position - state.Position;

        if (deviation.Length() > 0.05f)
        {
            this.Position = state.Position;
            GD.PrintErr("Reconciliating!");
        }
    }

    private void SendInput(byte input, int timeStamp)
    {
        if (_commands.Count >= REDUNDANCY_PACKETS)
        {
            _commands.RemoveAt(0); //TODO: Lazy!
        }

        _commands.Add(input);

        var userCmd = new NetMessage.UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            Stamp = timeStamp,
            Commands = _commands.ToArray()
        };

        if (this.IsMultiplayerAuthority() && Multiplayer.GetUniqueId() != 1)
        {
            byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(userCmd);

            (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
                MultiplayerPeer.TransferModeEnum.UnreliableOrdered, 0);
        }
    }

    private void MoveLocally(byte input)
    {
        _velocity = PlayerMovement.ComputeMotion(this, _velocity, PlayerMovement.InputToDirection(input), 1 / 30.0);
        MoveAndCollide(_velocity * (1 / 30.0f));
    }

    private byte PackInput()
    {
        byte input = 0;

        if (Input.IsActionPressed("ui_right")) input |= (byte)InputFlags.Right;
        if (Input.IsActionPressed("ui_left")) input |= (byte)InputFlags.Left;
        if (Input.IsActionPressed("ui_up")) input |= (byte)InputFlags.Forward;
        if (Input.IsActionPressed("ui_down")) input |= (byte)InputFlags.Backward;

        return input;
    }
}
