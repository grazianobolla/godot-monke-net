using Godot;
using System.Collections.Generic;
using MessagePack;

namespace NetMessage
{
    [MessagePack.Union(0, typeof(UserCommand))]
    [MessagePack.Union(1, typeof(GameSnapshot))]
    [MessagePack.Union(2, typeof(Sync))]
    public interface ICommand { }

    // Encapsulates user input and other client actions
    [MessagePackObject]
    public partial struct UserCommand : ICommand
    {
        [Key(0)]
        public int Id;

        [Key(1)]
        public float DirX;

        [Key(2)]
        public float DirY;

        [IgnoreMember]
        public Vector2 Direction
        {
            get { return new Vector2(DirX, DirY); }
        }
    }

    // Game state for a given point in time
    [MessagePackObject]
    public partial struct GameSnapshot : ICommand
    {
        [Key(0)]
        public UserState[] States;

        [Key(1)]
        public int Time;
    }

    // Used to calculate latency
    [MessagePackObject]
    public partial struct Sync : ICommand
    {
        [Key(0)]
        public int ClientTime;

        [Key(1)]
        public int ServerTime;
    }

    // Encapsulates current state for a player (gets sent with gameSnapshot)
    [MessagePackObject]
    public partial struct UserState
    {
        [Key(0)]
        public int Id;

        [Key(1)]
        public float X;

        [Key(2)]
        public float Y;

        [Key(3)]
        public float Z;

        [IgnoreMember]
        public Vector3 Position
        {
            get { return new Vector3(X, Y, Z); }
        }
    }
}