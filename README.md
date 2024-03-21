# Godot 4 Multiplayer Template

> As of 20/02/24 I am actively working on this, I suggest you try the "develop" branch, it has better comments, clearer and improved logic and better debug information. Once I feel its ready I will merge into the main branch and start working on documentation.

This multiplayer template includes the implementation of:

- Client Side Prediction and Reconciliation of the Player
- Entity Interpolation
- Clock Synchronization
- A few other multiplayer systems

Most of the code is inside the client/ and server/ folders.
You can use it to speed up your developing process, it is based on the client/auth-server model.

## Usage
You will need Godot 4.X and C# with at least Net6

## What it is
This example demonstrates how to implement a client-server architecture in Godot using the Godot networking API, this can work as a base for a proper game or just to learn how this techniques can be implemented. I did not use RPC calls, I sent packed bytes manually over the network.

This diagram tries to explain how the project is structured (note that it may differ from actual code since I'm actively developing this):
![Diagram](https://github.com/grazianobolla/godot4-multiplayer-template/assets/35064738/fe528305-a02b-4204-b0d9-7380397190b9)

## Video Example
This video shows the project in action, I'm simulating a network with 150ms (+ 80ms jitter) of latency (client to server and server to client) + 10% packet loss, and even in these horrible conditions the players have an _acceptable_ experience.
<video src="https://github.com/grazianobolla/godot4-multiplayer-template/assets/35064738/83292302-7101-4722-bdd6-0915fbb6858b" width="500px"></video>
Keep in mind I can't show Client Side Prediction on a video, you will have to download the project yourself!

**Contact me on Discord (Raz#4584) I will be happy to help**

