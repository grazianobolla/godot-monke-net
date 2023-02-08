# Godot 4 Multiplayer Template
This multiplayer template includes the impmenentation of:

- Client Side Prediction
- Entity Interpolation

You can use it to speed up your developing process, it is based on the client/auth server model.
There are some improvements still left to do, but it works fine for real time multiplayer games.

## Usage
You will need Godot 4.X and C# with at least Net6

## What it is
This example demonstrates how to implement a client-server architecture in Godot using the Godot networking API, this can work as a base for a proper game or just to learn how this techniques can be implemented. I did not use RPC calls, I sent packed bytes manually over the network, due to godot being prepared for P2P instead of Client/Server RPC calls might be a little confusing for the Client/Server architecture.

## Client Server Architecture
Normally Godot multiplayer examples show a Peer to Peer architecture, where all clients are interconnected, and where they can freely communicate with each other, for me, it's easier to implement and prototype, but also this in general and specially in Godot is a mess, besides it having many drawbacks.

### Client-Server
To solve some of those problems a client-server architecture is needed, where there is a central authority (the server) who is in charge of controlling the clients and their interactions, this also makes it easier to prevent cheating, since all information first has to go through a server.

![P2P vs Server Based](https://sites.google.com/site/cis3347cruzguzman014/_/rsrc/1480320465440/module-2/client-server-and-peer-to-peer-networking/p2p-network-vs-server.jpg?height=206&width=400 "P2P vs Server Based")

**Contact me on Discord (Raz#4584) I will be happy to help**