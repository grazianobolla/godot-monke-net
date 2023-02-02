using Godot;
using System.Collections.Generic;
using MessagePack;

struct PositionHistory
{
    public int Stamp;
    public Vector3 Position;
}
// Wrapper scene spawned by the MultiplayerSpawner
public partial class ClientPlayer : CharacterBody3D
{
    public const int REDUNDANCY_PACKETS = 6;
    public int RedundantPackets { get; private set; } = 0;

    private NetMessage.MoveCommand[] _commands = new NetMessage.MoveCommand[REDUNDANCY_PACKETS];
    private List<PositionHistory> _history = new();
    private int _seqStamp = 0;

    private Vector3 _velocity = Vector3.Zero;

    public override void _PhysicsProcess(double delta)
    {
        byte input = PackInput();
        var cmd = SendInput(input);
        MoveLocally(cmd);
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

    private NetMessage.MoveCommand SendInput(byte input)
    {
        var cmd = new NetMessage.MoveCommand
        {
            Input = input,
            Stamp = _seqStamp
        };

        _commands[_seqStamp % REDUNDANCY_PACKETS] = cmd;

        var userCmd = new NetMessage.UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            Commands = _commands
        };

        RedundantPackets = userCmd.Commands.Length;

        if (this.IsMultiplayerAuthority() && Multiplayer.GetUniqueId() != 1)
        {
            byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(userCmd);

            (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
                MultiplayerPeer.TransferModeEnum.UnreliableOrdered, 0);
        }

        return cmd;
    }

    private void MoveLocally(NetMessage.MoveCommand command)
    {
        _velocity = PlayerMovement.ComputeMotion(this, _velocity, command.Direction, 1 / 30.0);
        MoveAndCollide(_velocity * (1 / 30.0f));
    }

    private byte PackInput()
    {
        byte input = 0;

        if (Input.IsActionPressed("ui_right")) input |= 0x1;
        if (Input.IsActionPressed("ui_left")) input |= 0x2;
        if (Input.IsActionPressed("ui_up")) input |= 0x4;
        if (Input.IsActionPressed("ui_down")) input |= 0x8;

        return input;
    }
}
