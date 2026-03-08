using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using JumpAndRun.Scenes;

namespace JumpAndRun.Core
{
    /// <summary>
    /// Factory to create scenes by their string ID.
    /// Used for portal-based scene transitions.
    /// </summary>
    public static class SceneFactory
    {
        private static GraphicsDevice _graphicsDevice;
        private static ContentManager _contentManager;

        public static void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            _graphicsDevice = graphicsDevice;
            _contentManager = contentManager;
        }

        public static Scene Create(string sceneId)
        {
            if (_graphicsDevice == null || _contentManager == null)
            {
                throw new InvalidOperationException("SceneFactory not initialized. Call Initialize() first.");
            }

            switch (sceneId)
            {
                case "OpenWorldScene": return new OpenWorldScene(_graphicsDevice, _contentManager, SimContext.Instance);
                case "SideScrollScene": return new SideScrollScene(_graphicsDevice, _contentManager);
                default: 
                    // Any unmapped ID is assumed to be a dynamically generated town or specific interior missing a class
                    return new TownScene(_graphicsDevice, _contentManager, SimContext.Instance, sceneId);
            }
        }
    }
}
