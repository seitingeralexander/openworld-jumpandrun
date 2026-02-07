using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace JumpAndRun.Core
{
    public static class DebugFont
    {
        private static Texture2D _fontTexture;
        private static readonly Dictionary<char, Rectangle> _glyphs = new Dictionary<char, Rectangle>();
        private const int CharWidth = 3;
        private const int CharHeight = 5;

        // 3x5 font definitions
        // . = empty, # = pixel
        private static readonly Dictionary<char, string[]> _charDefs = new Dictionary<char, string[]>
        {
            {'A', new[]{ ".#.", "#.#", "###", "#.#", "#.#" }},
            {'B', new[]{ "##.", "#.#", "##.", "#.#", "##." }},
            {'C', new[]{ ".##", "#..", "#..", "#..", ".##" }},
            {'D', new[]{ "##.", "#.#", "#.#", "#.#", "##." }},
            {'E', new[]{ "###", "#..", "##.", "#..", "###" }},
            {'F', new[]{ "###", "#..", "##.", "#..", "#.." }},
            {'G', new[]{ ".##", "#..", "#.#", "#.#", ".##" }},
            {'H', new[]{ "#.#", "#.#", "###", "#.#", "#.#" }},
            {'I', new[]{ "###", ".#.", ".#.", ".#.", "###" }},
            {'J', new[]{ "###", ".#.", ".#.", "#.#", ".#." }},
            {'K', new[]{ "#.#", "##.", "#..", "##.", "#.#" }},
            {'L', new[]{ "#..", "#..", "#..", "#..", "###" }},
            {'M', new[]{ "#.#", "###", "#.#", "#.#", "#.#" }},
            {'N', new[]{ "###", "#.#", "#.#", "#.#", "#.#" }},
            {'O', new[]{ ".#.", "#.#", "#.#", "#.#", ".#." }},
            {'P', new[]{ "##.", "#.#", "##.", "#..", "#.." }},
            {'Q', new[]{ ".#.", "#.#", "#.#", ".##", "..#" }},
            {'R', new[]{ "##.", "#.#", "##.", "#.#", "#.#" }},
            {'S', new[]{ ".##", "#..", ".#.", "..#", "##." }},
            {'T', new[]{ "###", ".#.", ".#.", ".#.", ".#." }},
            {'U', new[]{ "#.#", "#.#", "#.#", "#.#", "###" }},
            {'V', new[]{ "#.#", "#.#", "#.#", "#.#", ".#." }},
            {'W', new[]{ "#.#", "#.#", "#.#", "###", "#.#" }},
            {'X', new[]{ "#.#", "#.#", ".#.", "#.#", "#.#" }},
            {'Y', new[]{ "#.#", "#.#", ".#.", ".#.", ".#." }},
            {'Z', new[]{ "###", "..#", ".#.", "#..", "###" }},
            {'0', new[]{ ".#.", "#.#", "#.#", "#.#", ".#." }},
            {'1', new[]{ "##.", ".#.", ".#.", ".#.", "###" }},
            {'2', new[]{ "##.", "..#", ".#.", "#..", "###" }},
            {'3', new[]{ "##.", "..#", ".#.", "..#", "##." }},
            {'4', new[]{ "#.#", "#.#", "###", "..#", "..#" }},
            {'5', new[]{ "###", "#..", "###", "..#", "##." }},
            {'6', new[]{ ".#.", "#..", "###", "#.#", ".#." }},
            {'7', new[]{ "###", "..#", ".#.", ".#.", ".#." }},
            {'8', new[]{ ".#.", "#.#", ".#.", "#.#", ".#." }},
            {'9', new[]{ ".#.", "#.#", "###", "..#", ".#." }},
            {'.', new[]{ "...", "...", "...", "...", ".#." }},
            {'-', new[]{ "...", "...", "###", "...", "..." }},
            {':', new[]{ "...", ".#.", "...", ".#.", "..." }},
            {'!', new[]{ ".#.", ".#.", ".#.", "...", ".#." }},
            {' ', new[]{ "...", "...", "...", "...", "..." }}
        };

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if (_fontTexture != null) return;

            int count = _charDefs.Count;
            _fontTexture = new Texture2D(graphicsDevice, count * CharWidth, CharHeight);
            Color[] data = new Color[_fontTexture.Width * _fontTexture.Height];

            // Fill transparent
            for(int i=0; i<data.Length; i++) data[i] = Color.Transparent;

            int idx = 0;
            foreach (var kvp in _charDefs)
            {
                char c = kvp.Key;
                string[] rows = kvp.Value;
                int startX = idx * CharWidth;
                _glyphs[c] = new Rectangle(startX, 0, CharWidth, CharHeight);

                for (int y = 0; y < CharHeight; y++)
                {
                    for (int x = 0; x < CharWidth; x++)
                    {
                        if (x < rows[y].Length && rows[y][x] == '#')
                        {
                            data[y * _fontTexture.Width + (startX + x)] = Color.White;
                        }
                    }
                }
                idx++;
            }

            _fontTexture.SetData(data);
        }

        public static void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 2.0f)
        {
            if (_fontTexture == null) return;

            Vector2 cursor = position;
            foreach (char c in text.ToUpper())
            {
                if (_glyphs.TryGetValue(c, out Rectangle rect))
                {
                    spriteBatch.Draw(_fontTexture, cursor, rect, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                cursor.X += (CharWidth + 1) * scale;
            }
        }
        
        public static Vector2 MeasureString(string text, float scale = 2.0f)
        {
             return new Vector2(text.Length * (CharWidth + 1) * scale, CharHeight * scale);
        }
    }
}
