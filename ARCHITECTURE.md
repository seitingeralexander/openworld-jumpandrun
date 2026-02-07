# Architecture

## Overview
This project is a 2D Town Simulation and Jump & Run game built with **MonoGame** and **.NET 8**. The architecture is designed to decouple **Simulation Data** from **Rendering/View Logic**, allowing for a system-driven gameplay experience where NPCs live freely in a semantic world.

## High-Level Architecture

The application is structured into distinct layers:

1.  **Core**: Manages the application lifecycle, global state (Time, SimContext), Scene transitions, and World initialization.
2.  **World**: Defines the semantic environment (Town, Locations) without concern for pixels.
3.  **Simulation**: Handles the logic for NPCs, including Needs, Schedules, and AI.
4.  **Scenes**: Connects the Core, World, and Simulation layers to the MonoGame Rendering loop (view-only).

## Directory Structure

```text
JumpAndRun/
├── Core/           # System-level infrastructure
│   ├── SimContext.cs # SINGLETON - Container for global simulation state (Time, Town, NPCs, NPCSystem)
│   ├── TimeSystem.cs # Handles Day/Hour/Minute progression and events
│   ├── WorldDataLoader.cs # Initializes world data (Locations, NPCs) at game start
│   ├── Scene.cs    # Abstract base class for game states (receives SimContext)
│   └── SceneManager.cs # Handles switching between Scenes
├── World/          # Semantic world data
│   ├── Town.cs     # Collection of Locations
│   └── Location.cs # Definition of places (Home, Bakery, etc.)
├── Simulation/     # Agent logic and data
│   ├── NPC.cs      # Core entity data (Name, Pos, State)
│   ├── NPCSystem.cs # Logic loop for updating NPCs (Needs decay, Schedule execution)
│   ├── Needs.cs    # Hunger, Energy, etc.
│   ├── Schedule.cs # Daily routine definitions
│   └── Background.cs # Static character traits
├── Scenes/         # Concrete game states (View Layer - READ ONLY from simulation)
│   ├── TownScene.cs # Visualizes the Simulation
│   ├── TopDownScene.cs # Legacy/Alternative gameplay mode
│   └── SideScrollScene.cs # Legacy/Alternative gameplay mode
├── Entities/       # Shared game objects (ECS-lite)
└── Components/     # Reusable logic blocks (Collider, SpriteRenderer)
```

## Key Systems

### 1. SimContext (Singleton)
*   **Location**: `Core/SimContext.cs`
*   **Pattern**: Singleton - persists across scene changes
*   **Contains**: TimeSystem, Town, NPCs list, NPCSystem
*   **Update**: Called by `Game1.Update()` to tick all simulation systems

### 2. WorldDataLoader
*   **Location**: `Core/WorldDataLoader.cs`
*   **Function**: Initializes world data (Locations, NPCs, Schedules) at game start
*   **Called**: Once in `Game1.Initialize()` before loading any scene

### 3. Time System
*   **Location**: `Core/TimeSystem.cs`
*   **Function**: Tracks simulation time (Day, Hour, Minute).
*   **Scaling**: Decoupled from real-time (e.g., 1 real second = 1 game minute).
*   **Events**: Triggers callbacks (`OnHourChanged`, etc.) for systems to react to.

### 4. Simulation Loop
*   **Location**: `Simulation/NPCSystem.cs`
*   **Logic**:
    1.  **Decay**: Needs (Hunger, Energy) decrease over time.
    2.  **Recovery**: If at a providing Location, Needs increase based on `Location.NeedSatisfactionRates`.
    3.  **Critical Needs**: If Needs drop below 20%, NPC seeks the best Location (e.g., Home for Sleep).
    4.  **Hysteresis**: NPC stays at the recovery location until the need is fully satisfied (>95%).
    5.  **Schedule**: Otherwise, follows the daily routine.

### 5. Scene Management
*   **Location**: `Core/SceneManager.cs`
*   **Pattern**: State Pattern.
*   **Usage**: The `Game1` class forwards `Update` and `Draw` calls to the active `Scene`. 
*   **SimContext**: Passed to scenes via base constructor. Scenes READ from SimContext, don't own it.
*   **Persistence**: NPCs and world state persist across scene transitions.

### 6. World Representation
*   **Location**: `World/`
*   **Concept**: Locations are logical entities that act as **Need Providers**.
```csharp
class Location {
    Dictionary<NeedType, float> NeedSatisfactionRates; // e.g., { Hunger: 20.0f }
}
class Town {
    Location GetBestLocationForNeed(NeedType need);
}
```

## Data Flow

1.  **Initialization**:
    *   `Game1.Initialize()` -> `WorldDataLoader.Initialize(SimContext.Instance)`
    *   World data (Locations, NPCs) created once and stored in SimContext singleton

2.  **Update Loop**:
    *   `Game1` -> `SimContext.Instance.Update()` (Time + NPCSystem)
    *   `Game1` -> `SceneManager` -> `ActiveScene.Update()` (Player input, Camera)
    *   Simulation runs regardless of which scene is active

3.  **Draw Loop**:
    *   `Game1` -> `SceneManager` -> `ActiveScene.Draw()`
    *   Scene reads `Context.NPCs` and `Context.Town` to render
    *   NPCs persist across scene transitions

