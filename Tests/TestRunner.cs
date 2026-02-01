using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using JumpAndRun.Core;
using JumpAndRun.Entities;
using JumpAndRun.Components;
using System.Collections.Generic;

namespace JumpAndRun.Tests
{
    public class TestRunner
    {
        public static void RunTests()
        {
            Console.WriteLine("Running Logic Tests...");
            InputManager.Instance.EnableTestMode();

            TestSideScrollPhysics();
            TestTopDownMovement();

            InputManager.Instance.DisableTestMode();
            Console.WriteLine("All Tests Completed.");
        }

        private static void TestSideScrollPhysics()
        {
            Console.WriteLine("\n[Test] SideScroll Physics");

            // Setup
            var player = new GameObject() { Position = new Vector2(100, 300) };
            var collider = new BoxCollider(24, 48);
            player.AddComponent(collider);
            
            var platform = new GameObject() { Position = new Vector2(100, 400) }; // Floor at 400
            platform.AddComponent(new BoxCollider(100, 20));

            var platforms = new List<GameObject> { platform };
            
            // Camera mock (dummy view)
            var viewport = new Viewport(0,0,800,600);
            var camera = new Camera(viewport);

            var controller = new SideScrollController() 
            { 
                Camera = camera,
                Platforms = platforms
            };
            player.AddComponent(controller);

            // Test 1: Gravity
            Console.WriteLine("Step 1: Verify Gravity");
            float startY = player.Position.Y;
            SimulateFrames(player, 10); // Run 10 frames (~160ms)
            if (player.Position.Y > startY)
                Console.WriteLine("PASS: Player fell down (Gravity works).");
            else
                Console.WriteLine($"FAIL: Player did not fall. Y: {player.Position.Y}");

            // Test 2: Collision / Landing
            Console.WriteLine("Step 2: Verify Floor Collision");
            // Allow enough time to fall to 400
            SimulateFrames(player, 100); 
            
            // Expected landing Y: Platform Y (400) - Player Height (48) + offset handling?
            // BoxCollider offset is -Width/2, -Height/2.
            // So Bounds Top is Pos.Y - 24. 
            // Platform Bounds Top is 400 - 10 = 390.
            // Collision logic sets Player Pos so Bounds Bottom matches Platform Bounds Top.
            // Player Bounds Bottom = Pos.Y + 24.
            // Target: Pos.Y + 24 = 390 => Pos.Y = 366.
            
            // Let's check if velocity Y is 0
            if (System.Math.Abs(controller.Velocity.Y) < 0.1f)
                Console.WriteLine($"PASS: Player stopped falling at Y={player.Position.Y}.");
            else
                Console.WriteLine($"FAIL: Player is still moving Y={controller.Velocity.Y}.");

            // Test 3: Jump
            Console.WriteLine("Step 3: Verify Jump");
            InputManager.Instance.SimulateKeyDown(Keys.Space);
            SimulateFrames(player, 1); // Trigger jump frame
            InputManager.Instance.SimulateKeyDown(Keys.None); // Release

            if (controller.Velocity.Y < 0)
                Console.WriteLine("PASS: Player velocity is negative (Jumping).");
            else
                Console.WriteLine($"FAIL: Player did not jump. Vel Y: {controller.Velocity.Y}");
        }

        private static void TestTopDownMovement()
        {
            Console.WriteLine("\n[Test] TopDown Movement");
            
            // Setup
            var player = new GameObject() { Position = new Vector2(100, 100) };
            
            // Camera mock
            var viewport = new Viewport(0,0,800,600);
            var camera = new Camera(viewport);
            // Camera is at 100,100 initially? No, Camera.Position is center?
            // Camera.Follow(100,100) -> Transform translates world (100,100) to screen center (400,300).
            camera.Follow(player.Position);

            var controller = new TopDownController() 
            { 
                Camera = camera,
                Speed = 100f
            };
            player.AddComponent(controller);

            // Test 1: Move towards Mouse (Up/North)
            // Screen Center is 400,300. Player is at Screen Center.
            // Place Mouse at 400, 200 (Above player).
            InputManager.Instance.SimulateMousePosition(400, 200);
            
            // Press W (Forward towards mouse)
            InputManager.Instance.SimulateKeyDown(Keys.W);
            
            Vector2 startPos = player.Position;
            SimulateFrames(player, 10); // 0.16s * 100 speed = 16 pixels approx
            
            // Start (100,100). Mouse relative is (0, -100) -> Up.
            // Player should move Y- (Up).
            if (player.Position.Y < startPos.Y)
                 Console.WriteLine($"PASS: Player moved Up towards mouse. NewPos: {player.Position}");
            else
                 Console.WriteLine($"FAIL: Player did not move up. NewPos: {player.Position}");

        }

        private static void SimulateFrames(GameObject obj, int frames)
        {
            var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)); // 60 FPS
            for(int i=0; i<frames; i++)
            {
                InputManager.Instance.Update(); // Updates simulated state
                obj.Update(gameTime);
            }
        }
    }
}
