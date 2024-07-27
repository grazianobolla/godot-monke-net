# Godot Monke-Net

The final goal of this project is to create an Addon for Godot that allows you to easily develop multiplayer solutions in a fast and robust way, especially for frenetic real-time games. Although the project now only exists in C#, in the future it will be ported to a GDExtension to allow both GDScript and C# to be used, but this is a long way off today. In the current state of the project (if you download the project from this repository and open it in Godot) you will see a completely networked First Person Controller, with very robust implementations of Client Side Prediction and Reconciliation, as well as you will be able to see other clients perfectly thanks to Snapshot Interpolation. It's a very good start to working on an FPS game. Although it is not very flexible, in the future Nodes will be created that will make the work much easier, and implementing Vehicles or Platforms on the network will be just a matter of dragging and dropping a script/node.

<video src="https://github.com/user-attachments/assets/af4b5049-51e4-44cd-b38f-22c4ce614369" width="600px"></video>
<sup>Example recorded with 200ms lag, 5% packet loss, 10% out of order, 10% duplicated, 10% throttle in Clumsy 0.3</sup>

## What does it include now?
This multiplayer template includes the implementation of:

- Client Side Prediction and Reconciliation of the Player
- Entity Interpolation
- Clock Synchronization
- A few other multiplayer systems

Most of the code is inside the client/ and server/ folders.
You can use it to speed up your developing process, it is based on the client/auth-server model.

## Usage
You will need Godot 4.X and C# with at least Net6, just run the game in Godot. If you are alone and want to connect to your own server, run 2 instances of the project by setting Dobug->Run Multiple Instances to _2 Instances_

This diagram tries to explain how the project is structured (note that it may differ from actual code since I'm actively developing this):
![Diagram](https://github.com/grazianobolla/godot4-multiplayer-template/assets/35064738/fe528305-a02b-4204-b0d9-7380397190b9)

## Video Example
This video shows the project in action, I'm simulating a network with 150ms (+ 80ms jitter) of latency (client to server and server to client) + 10% packet loss, and even in these horrible conditions the players have an _acceptable_ experience.
<video src="https://github.com/grazianobolla/godot4-multiplayer-template/assets/35064738/83292302-7101-4722-bdd6-0915fbb6858b" width="600px"></video>
Keep in mind I can't show Client Side Prediction on a video, you will have to download the project yourself!

**Contact me on Discord (Raz#4584) I will be happy to help**
