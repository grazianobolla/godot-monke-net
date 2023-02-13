# Godot 4 Multiplayer Template
This multiplayer template includes the impmenentation of:

- Client Side Prediction and Reconciliation
- Entity Interpolation
- Clock Synchronization
- A few other multiplayer systems

You can use it to speed up your developing process, it is based on the client/auth-server model.

There are some improvements still left to do, mainly:
- The tickrate of the simulation in defined by the physics_process, which right now is set at 60hz, which might be too much.
- Separate CSP and Reconciliation into different classes, keep code clean.
- Player to Player collisions are not implemented, can be easily added but they depend heavily on the type of game.
- Comment a bunch of the code, make it easier to understand for learning purposes.

## Usage
You will need Godot 4.X and C# with at least Net6

## What it is
This example demonstrates how to implement a client-server architecture in Godot using the Godot networking API, this can work as a base for a proper game or just to learn how this techniques can be implemented. I did not use RPC calls, I sent packed bytes manually over the network.

**Contact me on Discord (Raz#4584) I will be happy to help**
