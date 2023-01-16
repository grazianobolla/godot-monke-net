using Godot;
using System.Runtime.InteropServices;

// Encapsulates user input and other states
public struct UserCommand
{
    public int Id;
    public float DirX, DirY;
}

// Encapsulates current game state for a player
public struct UserState
{
    public int Id;
    public float X, Y, Z;

    public Vector3 Position
    {
        get { return new Vector3(X, Y, Z); }
    }
}

public struct GameSnapshot
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public UserState[] States = new UserState[1];
    public double Time;
    public GameSnapshot(double time) { Time = time; }
}