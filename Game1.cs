using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.Scenes;

namespace JumpAndRun;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // Setup world data (NPCs, Locations) once at game start
        WorldDataLoader.Initialize(SimContext.Instance);
        
        // Initialize SceneFactory for portal-based scene transitions
        SceneFactory.Initialize(GraphicsDevice, Content);
        
        // Run verification tests logic (Headless check)
        JumpAndRun.Tests.TestRunner.RunTests();

        // Load initial scene with shared SimContext
        SceneManager.Instance.LoadScene(new OpenWorldScene(GraphicsDevice, Content, SimContext.Instance));
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        // Content loading is now handled by Scenes, but we might need global content here
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        InputManager.Instance.Update();
        
        // Update simulation (Time, NPCs) at game level - persists across scenes
        SimContext.Instance.Update(gameTime);
        
        SceneManager.Instance.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Instance.Draw(_spriteBatch);

        base.Draw(gameTime);
    }
}
