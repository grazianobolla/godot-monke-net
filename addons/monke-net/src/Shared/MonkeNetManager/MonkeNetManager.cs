using Godot;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using System;
using System.Linq;
using System.Reflection;

namespace MonkeNet.Shared;

public partial class MonkeNetManager : Node
{
    public static MonkeNetManager Instance { get; private set; }
    public bool IsServer { get; private set; } = false;

    private INetworkManager _networkManager;

    public override void _Ready()
    {
        _networkManager = GetNode("NetworkManagerEnet") as INetworkManager;
        RegisterNetworkMessages();
        Instance = this;
    }

    public void CreateClient(string address, int port)
    {
        IsServer = false;
        var clientManagerScene = GD.Load<PackedScene>("res://addons/monke-net/scenes/ClientManager.tscn");
        var clientManager = clientManagerScene.Instantiate() as Client.ClientManager;
        AddChild(clientManager);

        // TODO: pass configurations as struct/.ini
        clientManager.Initialize(_networkManager, address, port);
    }

    public void CreateServer(int port)
    {
        IsServer = true;
        var serverManagerScene = GD.Load<PackedScene>("res://addons/monke-net/scenes/ServerManager.tscn");
        var serverManager = serverManagerScene.Instantiate() as Server.ServerManager;
        AddChild(serverManager);

        // TODO: pass configurations as struct/.ini
        serverManager.Initialize(_networkManager, port);
    }

    private static void RegisterNetworkMessages()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(t => typeof(IPackableMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        foreach (var t in types)
        {
            var attrib = Attribute.GetCustomAttribute(t, typeof(RegisterMessageAttribute)) ??
                throw new MonkeNetException($"The type {t.FullName} doesn't have the {typeof(RegisterMessageAttribute).Name} attribute!");

            RegisterMessageAttribute registerMessageAttribute = (RegisterMessageAttribute)attrib;
            var messageInstance = Activator.CreateInstance(t);
            MessageSerializer.Types.Add((byte)registerMessageAttribute.MessageType, (IPackableMessage)messageInstance);
            GD.Print($"Registered network message {t.FullName}");
        }
    }
}