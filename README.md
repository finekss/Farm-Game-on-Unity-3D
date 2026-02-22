# 🌾 Cozy Farm Sandbox (Unity 6000.3)

A 3D cosy farm sandbox game built with a modular, feature-driven architecture.

The project focuses on clean architecture, scalability, and long-term maintainability.  
Designed as an experimental foundation for:

- 🧱 Farm & Building systems  
- 🛠 Resource gathering & automation  
- 🌙 Day/Night cycle with raids  
- 🧠 Modular Feature-based architecture  
- 🧩 Event-driven systems  
- 🔮 Future-ready for Coop  

---

# 🎮 Game Concept

You build and expand a peaceful farm within a limited territory.

However, during the night, mischievous entities attempt to:

- Steal resources  
- Sabotage buildings  
- Disrupt automation  

The player must:

- Build defenses  
- Automate production  
- Track and react to threats  
- Improve infrastructure  

The experience balances:

> Relaxing progression + light strategic tension

---

# 🏗 Architecture Overview

The project follows a **Feature-based Game Loop architecture**.

### Core Principles

- No direct system-to-system dependencies  
- Event-driven communication  
- Decoupled logic from Unity MonoBehaviours  
- Clear separation of responsibilities  

---

## 🔄 High-Level Flow
UnityShell (MonoBehaviour bridge)
↓
Main (Core container)
↓
Feature modules

---

# 🧠 Core Systems

## Main

Acts as the composition root and feature container.

Responsibilities:

- Registering features  
- Executing game loop (`Tick`)  
- Saving game data  
- Providing feature access  

---

## EventBus

Handles decoupled communication between systems.

Responsibilities:

- Subscribe / Unsubscribe  
- Publish events  
- Eliminate direct feature references  

---

## GameStateMachine

Controls global game states.

States include:

- MainMenu  
- Loading  
- Playing  
- Paused  
- GameOver  

---

## TimeSystem

Manages in-game time.

Responsibilities:

- Day/Night cycle  
- Phase switching  
- Time progression  

---

## GameManager

Orchestrates high-level gameplay logic.

Responsibilities:

- Reacting to events  
- Starting raids  
- Coordinating systems  

---

# 🧩 Player Architecture

Player is separated into clear layers:

Player
├── CharacterController (collision engine)
├── PlayerController (movement logic)
├── PlayerInputHandler (input abstraction)


### Responsibilities

**CharacterController**
- Collision handling  
- Slope and step resolution  

**PlayerController**
- Movement logic  
- Jump / roll / stamina  
- State transitions  

**PlayerInputHandler**
- Input System integration  
- Converts input into commands  

---

# 💾 Save System

The project uses DTO-based serialization.

Unity types like `Vector3` and `Quaternion` are wrapped in:

- `SerializableVector3`  
- `SerializableQuaternion`  

This prevents serialization issues and keeps save data engine-agnostic.

Game data is stored in:
GameData

Saved via:
PlayerPrefs + JSON (Newtonsoft)


Future plan: migrate to file-based save system.

---

# 🚀 Current Status

- ✔ Player controller implemented  
- ✔ Feature-based architecture  
- ✔ EventBus system  
- ✔ GameStateMachine  
- ✔ TimeSystem  
- ✔ Save system foundation  

---

# 🔮 Planned Systems

- Resource system  
- Building system  
- Raid system  
- AI (ECS-based)  
- Defense structures  
- Automation chains  
- Coop-ready architecture  

---

# 🛠 Tech Stack

- Unity 6000.3  
- C#  
- Unity Input System  
- Cinemachine  
- Newtonsoft.Json  

---

# 🤝 Contributing

This project is open for experimentation and architectural discussions.

If you are interested in:

- Game architecture  
- Modular systems  
- Sandbox design  
- ECS integration  

Feel free to fork and explore.

---

# 📜 License

MIT