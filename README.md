# 🔫 Bera-MiniShooterz

**Bera-MiniShooterz** is a fast-paced, top-down **3v3 multiplayer shooter** inspired by the core mechanics and style of *Brawl Stars*. Designed with modularity and extensibility in mind, the game blends **real-time multiplayer via Unity Netcode (NGO)**, fluid gameplay, and a clean UI/UX layer using DoTween and Unity UI tools. Whether it's Player-vs-Player (PvP) or with AI bots, every aspect of the game has been tuned for tactical fun and snappy responsiveness.

---

## 🧩 Core Gameplay

Bera-MiniShooterz is a **round-based 3v3 shooter**, where players battle in bite-sized arenas using distinct characters and weapons. The core gameplay loop revolves around:

- **Strategic Shooting** — Players aim and fire projectiles with limited ammo and timed reloads.
- **Auto Reloading** — Each shot initiates a reload timer per bullet with ongoing reload even while firing.
- **Character Abilities** — Characters will have distinct stats and effects, giving gameplay variety.
- **Health & Respawn Mechanics** — Players are temporarily removed from play on defeat and auto-respawned after a delay.

---

## 🌐 Networking with Unity NGO

Built on top of **Unity Netcode for GameObjects**, Bera-MiniShooterz uses NGO’s features for a stable and deterministic multiplayer experience:

### Features Used:

- **Lobby Management via Unity Lobby & Relay**
  - Host and join lobbies
  - Sync player data such as readiness and stats (XP, K/D)
- **State Sync**
  - NetworkVariable and ServerRPC usage for syncing bullets, player movement, and score data
- **Object Pooling with NGO**
  - Bullet and visual effect pooling with `NetworkObjectPool`
  - Efficient spawn/despawn synced across clients

---

## 🔫 Shooting & Weapon System

A refined shooter system handles bullet spawning, ammo count, and reload dynamics:

- **Aiming using Raycast or SphereCast**
  - Bullets use ray-based max distance or stop early on collisions
- **UI-Based Shooting Feedback**
  - World-space canvas with a visual indicator to show max distance and hit point
- **Auto Reload System**
  - Each shot triggers a staggered reload per bullet
  - Shooting continues while reloading happens in parallel
- **Ammo Count Sync**
  - Reload updates are sent via RPCs to keep all players in sync

---

## 🧠 AI Bot Support

When human players are not available, **AI bots** step in to fill roles:

- Target nearest enemy
- Handle basic movement, aiming, and shooting
- Easily expandable via `ICombat`, `ICharacter`, and AI behavior tree interfaces

---

## 🎮 Character Selection

- **Rotating Turntable Selection UI**
  - Implemented using DoTween
  - Character selection by index (not just left/right nav)
- **Animated Button Switches**
  - UI buttons animate into place using DoTween when switching characters
- **Character Stats Panel**
  - Displays real-time attributes like health, ammo, fire rate, etc.

---

## 🏆 XP, Stats & Leaderboards with PlayFab

**PlayFab integration** adds meta-progression and competitive features:

- **Authentication**
  - Automatic login and account creation
- **Player Stats**
  - Save and retrieve XP, Token, and K/D stats
- **Leaderboard**
  - Dynamic leaderboard shows surrounding entries (player + one above/below)
  - Displays name, XP, and K/D info

---

## 🎯 Aiming & Hit Detection

- Supports precise **line-based** and **sphere-based aiming**
- Uses either **Line Renderer** or **World-Space UI Images** to visually communicate bullet path
- Impact visuals and hit logic are synced via server commands

---

## 🧱 Modularity & Architecture

All systems are designed for **modular extensibility**:

### Core Classes and Interfaces:
- `CharacterMovement` — Manages character movement
- `CharacterShooter` — Handles shooting and aiming
- `HealthSystem` — Manages health and damage
- `PlayerInputHandler` — Manages player input (movement, shooting, etc.)
- `AimingController` — Handles aiming and hit detection
- `PlayerCharacterController` — Orchestrates character actions
- `AICharacterController` — AI behavior tree interface
- `PlayerDetails` — Stores and updates character attributes
- `IDamageable` — Interface for taking damage
- `ICombat` — Handles shooting logic
- `IStates` — Interface for state management (e.g., idle, moving, shooting)

### Netcode Logic:
- `LobbyManager` — Manages lobby state and player connections
- `RelayManager` — Handles server-client communication for game logic

### Game Manager:
Handles all round logic:
- Player readiness check with coroutine-based and coroutine-free methods
- Player spawn management
- Round timer, win condition check, and match cleanup
- Event system for HUD and audio to respond to gameplay state

### Player Data:
- `PlayFabNetManager` — PlayFab integration for authentication and player stats
- `SaveManager` — Manages player data (e.g., character stats, details)

### Other Managers and Systems:
- `AudioManager` — Manages audio effects and music
- `MenuManager` — Handles menu logic and UI
- `NetworkAPIManager` — Handles network requests and responses

---
## Sound and VFX 
- **3D Sound Propagation**: Realistic sound propagation using Unity's built-in 3D
- **Impact Visuals**: Customizable impact effects for bullets and melee attacks
- **Hit Detection**: Precise hit detection for bullets and melee attacks
- **Audio Cues**: Customizable audio cues for various events (e.g., shooting, hitting)
- **Sound Effects**: Customizable sound effects for various events (e.g., footsteps, jumping)
- **VFX**: Customizable visual effects for various events (e.g., bullet trails, explosions)

---

## 📦 Object Pooling System

Custom pooling system for bullets and effects via `NetworkObjectPool`:

- Avoids instantiating/despawning NetworkObjects every time
- Ensures proper ownership and authority when bullets are pooled
- Cleaner performance with minimal GC impact

---

## 🧠 Advanced State Systems

- **Player Ready Check**
  - Coroutine and non-coroutine based loop to monitor player readiness
  - Includes retry attempts and fallback to minimum players
- **Stat Updates and Sync**
  - Stats update instantly on event triggers like kills, match end, etc.

---

## 📁 Project Setup Summary

- ✅ Unity Netcode for GameObjects
- ✅ Unity Lobby + Relay (UAS)
- ✅ PlayFab SDK (Authentication, Leaderboards)
- ✅ DoTween (UI Animations)
- ✅ World Space UI (Health Bars, Aim Indicators)
- ✅ Unity’s New Input System

---

## 🎨 UX and Visuals

- Clean minimalist UI inspired by Brawl Stars
- Dynamic character selection UI
- Bullet visuals, reload indicators, and aiming markers are intuitive
- Lobby screen, loading transitions, and match result panels

---

## 🚀 Future Plans

- 💥 **Ultimate Abilities**
- 👕 **Character Skins**
- 📲 **Mobile Optimization**
- 🧠 **Smarter AI with Decision Trees**
- 🏹 **Projectile Variants: Explosives, Lasers, etc.**
- 🧩 **Map Variety & Obstacles**

---

## 💻 Contributors & Acknowledgements

This game system is designed by a small, passionate team with an eye for modularity and gameplay feel. Thanks to the Unity Netcode community and Brawl Stars for gameplay inspiration.

---

**Welcome to the battlefield. Pick your character. Lock and load. It’s Bera-MiniShooterz time! 🔫🔥**
