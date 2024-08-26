using System;

namespace MonkeNet.Shared;

public class MonkeNetException : Exception
{
    public MonkeNetException() { }

    public MonkeNetException(string message)
        : base(message) { }

    public MonkeNetException(string message, Exception inner)
        : base(message, inner) { }
}