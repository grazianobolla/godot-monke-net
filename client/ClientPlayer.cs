using Godot;
using System.Collections.Generic;
using MessagePack;

// Wrapper scene spawned by the MultiplayerSpawner
public partial class ClientPlayer : CharacterBody3D
{
    private uint _stampCounter = 0;

    private List<NetMessage.MoveCommand> _commands = new();

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        var cmd = SendInput(input);
        MoveLocally(cmd);
        _stampCounter++;
    }

    public void ReceiveState(NetMessage.UserState state)
    {
        _commands.RemoveAll(command => (command.Stamp <= state.Stamp));

        // Re apply input
        Vector3 lastPos = state.Position;

        foreach (var cmd in _commands)
        {
            lastPos += PlayerMovement.ComputeMotion(cmd.Direction);
        }

        var div = (lastPos - Position).Length();

        if (div > 0.05f)
        {
            Position = state.Position;
            GD.PrintErr($"Reconciliating got {lastPos} user at {Position}");
        }
    }

    private NetMessage.MoveCommand SendInput(Vector2 input)
    {
        var cmd = new NetMessage.MoveCommand
        {
            DirX = input.x,
            DirY = input.y,
            Stamp = _stampCounter
        };

        _commands.Add(cmd);

        var userCmd = new NetMessage.UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            Commands = _commands.ToArray()
        };

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
        Position += PlayerMovement.ComputeMotion(command.Direction);
    }

    public int RedundantPackets
    {
        get { return _commands.Count; }
    }
}
