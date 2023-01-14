using Godot;

// Encapsulates user input and other states
public struct UserCommand
{
    public int Id;
    public float DirX, DirY;
}

// Encapsulates current game state for a player
public struct GameState
{
    public int Id;
    public float X, Y, Z;
}