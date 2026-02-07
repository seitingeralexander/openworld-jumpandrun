# Architecture

## Overview
This project is a 2D Town Simulation and Jump & Run game built with **MonoGame** and **.NET 8**. The architecture is designed to decouple **Simulation Data** from **Rendering/View Logic**, allowing for a system-driven gameplay experience where NPCs live freely in a semantic world.

## High-Level Architecture

The application is structured into distinct layers:

1.  **Core**: Manages the application lifecycle, global state (Time), and Scene transitions.
2.  **World**: Defines the semantic environment (Town, Locations) without concern for pixels.
3.  **Simulation**: Handles the logic for NPCs, including Needs, Schedules, and AI.
4.  **Scenes**: Connects the Core, World, and Simulation layers to the MonoGame Rendering loop.

## Directory Structure

```text
JumpAndRun/
├── Core/           # System-level infrastructure
│   ├── Game1.cs    # Main Game Loop entry point
│   ├── TimeSystem.cs # Handles Day/Hour/Minute progression and events
│   ├── SimContext.cs # Container for global simulation state (Time, Town, NPCs)
│   ├── Scene.cs    # Abstract base class for game states
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
├── Scenes/         # Concrete game states (View Layer)
│   ├── TownScene.cs # Visualizes the Simulation
│   ├── TopDownScene.cs # Legacy/Alternative gameplay mode
│   └── SideScrollScene.cs # Legacy/Alternative gameplay mode
├── Entities/       # Shared game objects (ECS-lite)
└── Components/     # Reusable logic blocks (Collider, SpriteRenderer)
```

## Key Systems

### 1. Time System
*   **Location**: `Core/TimeSystem.cs`
*   **Function**: Tracks simulation time (Day, Hour, Minute).
*   **Scaling**: Decoupled from real-time (e.g., 1 real second = 1 game minute).
*   **Events**: Triggers callbacks (`OnHourChanged`, etc.) for systems to react to.

### 2. Simulation Loop
*   **Location**: `Simulation/NPCSystem.cs`
*   **Logic**:
    1.  **Decay**: Needs (Hunger, Energy) decrease over time.
    2.  **Recovery**: If at a providing Location, Needs increase based on `Location.NeedSatisfactionRates`.
    3.  **Critical Needs**: If Needs drop below 20%, NPC seeks the best Location (e.g., Home for Sleep).
    4.  **Hysteresis**: NPC stays at the recovery location until the need is fully satisfied (>95%).
    5.  **Schedule**: Otherwise, follows the daily routine.

### 3. Scene Management
*   **Location**: `Core/SceneManager.cs`
*   **Pattern**: State Pattern.
*   **Usage**: The `Game1` class forwards `Update` and `Draw` calls to the active `Scene`. Scenes manage their own content loading/unloading.

### 4. World Representation
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

1.  **Update Loop**:
    *   `Game1` -> `SceneManager` -> `TownScene`
    *   `TownScene` -> `TimeSystem` (Advance Clock)
    *   `TownScene` -> `NPCSystem` (Update Logic)
        *   `NPCSystem` modifies `NPC` state/position.

2.  **Draw Loop**:
    *   `Game1` -> `SceneManager` -> `TownScene`
    *   `TownScene` reads `NPC` state/position.
    *   `TownScene` draws sprites based on current state (Idle, Moving, Sleeping).
