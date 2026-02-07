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
    2.  **Schedule**: NPCs check their `Schedule` for the current hour's task.
    3.  **Override**: If Needs are critical, the Schedule is overridden (e.g., go eat, go sleep).
*   **Update Frequency**: Can run synchronized with `GameTime` or largely independent ticks.

### 3. Scene Management
*   **Location**: `Core/SceneManager.cs`
*   **Pattern**: State Pattern.
*   **Usage**: The `Game1` class forwards `Update` and `Draw` calls to the active `Scene`. Scenes manage their own content loading/unloading.

### 4. World Representation
*   **Location**: `World/`
*   **Concept**: Locations are logical entities, not just tiles. NPCs navigate between `Location` objects (e.g., "Move from Home to Bakery").
```csharp
class Town {
    List<Location> Locations;
    // Methods to find nearest tavern, workplace, etc.
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
