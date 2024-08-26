# 🐒 Monke-Net
<a href="https://discord.gg/EmyhsVZCnZ"><img alt="Static Badge" src="https://img.shields.io/badge/Discord-5865F2?logo=discord&logoColor=ffffff"></a> ![GitHub License](https://img.shields.io/github/license/grazianobolla/godot-monke-net) [![CodeFactor](https://www.codefactor.io/repository/github/grazianobolla/godot-monke-net/badge)](https://www.codefactor.io/repository/github/grazianobolla/godot-monke-net)

C# Godot Addon that facilitates creating robust multiplayer games using the Client-Authoritative Server architecture, including client side prediction, entity interpolation, lag compensation and more!

---

## 📚 Background
After many years since my first attempt at making a functional multiplayer game, I came to one conclusion: developing multiplayer games is hard, specially when leaving the P2P architecture and opting for a more "competitive" approach like having an authoritative server. This project aims to provide a starting point that can be used to speed up the time it takes to go from idea to reality, utilizing its features like:
- [x] CharacterBody Client Side Prediction and Reconciliation
- [x] Snapshot Interpolation for smooth visuals
- [x] Clock Synchronization between the server and clients
- [x] State Replication between clients
- [ ] Delta Compression for inputs/entity states
- [ ] Lag Compensation for client to client interactions

> [!IMPORTANT]
> Keep in mind my approach was completely personal, as I don't have prior experience with any other networking solutions for game engines like Fishnet/Fusion/UnrealEngine etc, I wrote what was useful for me and that might not be the best approach, if you have any thought/opinion about how the addon does stuff **please let me know**. 

## 🧩 Dependencies and .NET environment
MonkeNet requires your game project to use .NET 8, also it heavily uses [ImGui](https://github.com/ocornut/imgui) to display very important debug information, this means you will also have to install [ImGui Godot](https://github.com/pkdawson/imgui-godot) in your project, that's MonkeNet only dependency, altough I plan to remove it/make it modular in the future.

## 💾 Installation
MonkeNet is a Godot addon, to start using it copy the `addons\monke-net\` folder, paste it into your project `addons\` folder, and enable the plugin in your project settings. After this, you will have access to all MonkeNet resources.

## 📦 This Repository (Demo Project)
This repository is the developing environment for the addon, including tests and a demo project showcasing MonkeNet capabilities. If you have any trouble getting it to work, cloning this repository might be a good starting point that you can later adapt to your games requirements.
<video src="https://github.com/user-attachments/assets/af4b5049-51e4-44cd-b38f-22c4ce614369" width="600px"></video>
<sup>Example recorded with 200ms lag, 5% packet loss, 10% out of order, 10% duplicated, 10% throttle in Clumsy 0.3</sup>

## 📐 Project Structure (WIP)
MonkeNet is structured in different "components" that are Nodes inside the Godot engine, these components work together to provide different functionalities. Usually for the same funcionality there is a Client component and a Server component altough they do different things. Here there are some examples:

- `ClientEntityManager.cs` might handle *requesting* an entity on the server while `ServerEntityManager.cs` actually takes that request and spawns the entity.
- `ClientNetworkClock.cs` receives clock data from the server and updates its internal state, while the `ServerNetworkClock.cs` just runs a simple clock that increments each tick.

### 🐵 MonkeNet Singleton
The `MonkeNetManager` class is a singleton that can be used anywhere in your project and allows you to start either a server or a client.

### 🖥️ Client Side Components 
- Client Manager
- Entity Manager
- Network Clock
- Snapshot Interpolator

### 🖧 Server Side Components
- Server Manager
- Entity Manager
- Network Clock

### 🤝 Shared Components
- Message Serializer
- Entity Spawner
- Network Manager

---

If you have any questions, [please contact me on Discord](https://discord.gg/EmyhsVZCnZ), I'll be happy to help.
