# Architecture

## Overview
This project is a 2D Town Simulation and Jump & Run game built with **MonoGame** and **.NET 8**. The architecture is designed to decouple **Simulation Data** from **Rendering/View Logic**, allowing for a system-driven gameplay experience where NPCs live freely in a semantic world.

## High-Level Architecture

The application is structured into distinct layers:

1.  **Core**: Manages the application lifecycle, global state (Time, SimContext), Scene transitions, and World initialization.
2.  **World**: Defines the semantic environment (Town, Locations) without concern for pixels.
3.  **Simulation**: Handles the logic for Player, NPCs, including Needs, Schedules, and AI.
4.  **Scenes**: Connects the Core, World, and Simulation layers to the MonoGame Rendering loop (view-only).

```mermaid
graph TD
    subgraph Game [Game Loop]
        G[Game1]
        SM[SceneManager]
    end

    subgraph SimContext [SimContext Singleton - Persistent State]
        subgraph PlayerData [Player]
            P[Position]
            PS[PlayerStats]
            INV[Inventory]
            EQ[Equipment]
        end
        
        subgraph NPCData [NPCs]
            NPC1[NPC List]
            Needs[Needs: Hunger, Energy, Social]
            Schedule[Schedule: Daily Routine]
        end
        
        TS[TimeSystem]
        NS[NPCSystem]
        T[Town/Locations]
    end

    subgraph World [World Layer]
        LOC[Locations]
        RATES[NeedSatisfactionRates]
    end

    subgraph Scenes [View Layer - Read Only]
        TS2[TownScene]
        TDS[TopDownScene]
        SSS[SideScrollScene]
    end

    subgraph Controllers [Scene-Specific Controllers]
        TPV[TopDownController]
        SPV[SideScrollController]
    end

    G -->|Initialize| WDL[WorldDataLoader]
    G -->|Update| SimContext
    G --> SM
    SM --> Scenes

    NS -->|decays| Needs
    NS -->|follows| Schedule
    NS -->|queries| T
    TS -->|triggers| NS

    TS2 -->|creates| TPV
    SSS -->|creates| SPV
    
    TPV -.->|syncs Position| P
    SPV -.->|syncs Position| P
    
    Scenes -.->|reads| SimContext
    T --> LOC
    LOC --> RATES
```

## Directory Structure

```text
JumpAndRun/
├── Core/           # System-level infrastructure
│   ├── SimContext.cs # SINGLETON - Container for global simulation state
│   ├── TimeSystem.cs # Handles Day/Hour/Minute progression and events
│   ├── WorldDataLoader.cs # Initializes world data (Locations, NPCs) at game start
│   ├── Scene.cs    # Abstract base class for game states (receives SimContext)
│   └── SceneManager.cs # Handles switching between Scenes
├── World/          # Semantic world data
│   ├── Town.cs     # Collection of Locations
│   └── Location.cs # Definition of places (Home, Bakery, etc.)
├── Simulation/     # Agent logic and data
│   ├── Player.cs   # Player data (Stats, Inventory, Equipment) - PERSISTENT
│   ├── PlayerStats.cs # RPG-like statistics (Level, Health, Strength, etc.)
│   ├── Inventory.cs # Item storage and management
│   ├── Equipment.cs # Equipped items and stat bonuses
│   ├── NPC.cs      # Core entity data (Name, Pos, State)
│   ├── NPCSystem.cs # Logic loop for updating NPCs
│   ├── Needs.cs    # Hunger, Energy, etc.
│   ├── Schedule.cs # Daily routine definitions
│   └── Background.cs # Static character traits
├── Scenes/         # Concrete game states (View Layer - READ ONLY from simulation)
│   ├── TownScene.cs # Visualizes the Simulation
│   ├── TopDownScene.cs # Legacy/Alternative gameplay mode
│   └── SideScrollScene.cs # Legacy/Alternative gameplay mode
├── Entities/       # Shared game objects (ECS-lite)
└── Components/     # Reusable logic blocks (Collider, SpriteRenderer, Controllers)
    ├── TopDownController.cs # Syncs position with Player data
    └── SideScrollController.cs # Syncs position with Player data
```

## Key Systems

### 1. SimContext (Singleton)
*   **Location**: `Core/SimContext.cs`
*   **Pattern**: Singleton - persists across scene changes
*   **Contains**: Player, TimeSystem, Town, NPCs list, NPCSystem
*   **Update**: Called by `Game1.Update()` to tick all simulation systems

### 2. Player System
*   **Location**: `Simulation/Player.cs`, `PlayerStats.cs`, `Inventory.cs`, `Equipment.cs`
*   **Pattern**: Model-View separation (data persists, views are scene-specific)
*   **Data Flow**: 
    *   Controllers read position from `Player` on `Start()`
    *   Controllers write position back to `Player` on `Update()`
    *   Different scenes create different visual representations (TopDown vs SideScroll)

### 3. WorldDataLoader
*   **Location**: `Core/WorldDataLoader.cs`
*   **Function**: Initializes world data (Locations, NPCs, Schedules) at game start
*   **Called**: Once in `Game1.Initialize()` before loading any scene

### 4. Time System
*   **Location**: `Core/TimeSystem.cs`
*   **Function**: Tracks simulation time (Day, Hour, Minute).
*   **Scaling**: Decoupled from real-time (e.g., 1 real second = 1 game minute).
*   **Events**: Triggers callbacks (`OnHourChanged`, etc.) for systems to react to.

### 5. NPC Simulation Loop
*   **Location**: `Simulation/NPCSystem.cs`
*   **Logic**:
    1.  **Decay**: Needs (Hunger, Energy) decrease over time.
    2.  **Recovery**: If at a providing Location, Needs increase based on `Location.NeedSatisfactionRates`.
    3.  **Critical Needs**: If Needs drop below 20%, NPC seeks the best Location (e.g., Home for Sleep).
    4.  **Hysteresis**: NPC stays at the recovery location until the need is fully satisfied (>95%).
    5.  **Schedule**: Otherwise, follows the daily routine.

### 6. Scene Management
*   **Location**: `Core/SceneManager.cs`
*   **Pattern**: State Pattern.
*   **Usage**: The `Game1` class forwards `Update` and `Draw` calls to the active `Scene`. 
*   **SimContext**: Passed to scenes via base constructor. Scenes READ from SimContext, don't own it.
*   **Persistence**: Player, NPCs, and world state persist across scene transitions.

### 7. World Representation
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

### Initialization
*   `Game1.Initialize()` -> `WorldDataLoader.Initialize(SimContext.Instance)`
*   World data (Locations, NPCs, Player) created once and stored in SimContext singleton

### Update Loop
*   `Game1` -> `SimContext.Instance.Update()` (Time + NPCSystem)
*   `Game1` -> `SceneManager` -> `ActiveScene.Update()` (Player input, Camera)
*   Simulation runs regardless of which scene is active

### Draw Loop
*   `Game1` -> `SceneManager` -> `ActiveScene.Draw()`
*   Scene reads `Context.Player`, `Context.NPCs`, and `Context.Town` to render

### Scene Transition
```
┌─────────────────────────────────────────────────────────────┐
│ 1. Old Scene unloads → Controller syncs Position to Player  │
│ 2. New Scene loads → Controller reads Position from Player  │
│ 3. Player.Stats, Inventory, NPCs unchanged (never destroyed)│
└─────────────────────────────────────────────────────────────┘
```

## Future Considerations

| Feature | Notes |
|---------|-------|
| Equipment Effects | Stats modifiers from equipped items |
| Save/Load | Serialize Player + SimContext to disk |
| Different Sprites | TopDown vs SideScroll sprite sheets per scene |


