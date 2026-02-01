# JumpAndRun

A 2D Jump and Run game built with **MonoGame** and **.NET 8**.

## 🚀 Setup Guide (Linux)

### 1. Install .NET 8 SDK
Ensure you have the .NET 8 SDK installed.
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 8.0
```
Add dotnet to your path (add to `~/.bashrc` or `~/.zshrc` to make permanent):
```bash
export PATH=$PATH:$HOME/.dotnet
```

### 2. Install MonoGame Templates
Install the MonoGame project templates:
```bash
dotnet new install MonoGame.Templates.CSharp
```

### 3. Install System Dependencies (Linux)
MonoGame requires SDL2 and other libraries on Linux.
```bash
sudo apt-get update
sudo apt-get install -y libsdl2-2.0-0
```
*If you encounter "Failed to load library: libSDL2-2.0.so.0", this step is crucial.*

### 4. Build and Run
Navigate to the project directory and run:
```bash
dotnet restore
dotnet build
dotnet run
```

---

## 🏗️ Project Structure

```
JumpAndRun/
├── JumpAndRun.sln          # Visual Studio Solution file
├── JumpAndRun/
│   ├── JumpAndRun.csproj   # C# Project file
│   ├── Program.cs          # Entry point (Main)
│   ├── Game1.cs            # Core Game Class (Logic & Rendering)
│   ├── Content/            # Game Assets (Textures, Audio, Fonts)
│   │   └── Content.mgcb    # MonoGame Content Builder configuration
│   └── app.manifest        # Application manifest
```

## 🧩 Architecture

The game follows the standard **MonoGame Gameloop**:

1.  **Initialize()**:
    *   Sets up the graphics device.
    *   Initializes game logic variables (e.g., player position, stats).

2.  **LoadContent()**:
    *   Loads assets like textures and sounds into memory.
    *   *Currently creates a procedural texture for the player.*

3.  **Update(GameTime)**:
    *   **Input Handling**: polls Keyboard state.
    *   **Game Logic**: Updates player position, handles physics (gravity, collisions).
    *   Runs 60 times per second by default.

4.  **Draw(GameTime)**:
    *   Clears the screen.
    *   Draws sprites within `_spriteBatch.Begin()` and `End()`.

## 🎮 Controls
*   **Arrow Keys**: Move the player.
*   **Escape**: Exit the game.
