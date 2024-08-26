using System;
namespace MonkeNet.NetworkMessages;

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
public class RegisterMessageAttribute(MessageTypeEnum messageType, Type structType) : Attribute
{
    public MessageTypeEnum MessageType { get; private set; } = messageType;
    public Type StructType { get; private set; } = structType;
}
