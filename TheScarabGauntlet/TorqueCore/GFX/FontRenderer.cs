//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GFX
{
    /// <summary>
    /// Renders fonts to the screen using the XNA SpriteBatch utilities.
    /// </summary>
    public class FontRenderer
    {

        #region Static methods, fields, constructors

        public static FontRenderer Instance
        {
            get
            {
                if (_instance == null)
                    new FontRenderer();
                return _instance;
            }
        }



        static FontRenderer _instance;

        #endregion


        #region Constructors

        /// <summary>
        /// Create FontRenderer singleton used for drawing fonts to the screen.
        /// </summary>
        public FontRenderer()
        {
            Assert.Fatal(FontRenderer._instance == null, "FontReader Constructor - FontRenderer already exists.");
            FontRenderer._instance = this;

            Reset();
        }

        #endregion


        #region Public methods

        public void Reset()
        {
            _fontBatch = new SpriteBatch(GFXDevice.Instance.Device);
        }



        /// <summary>
        /// Draw the given string at the specified location using the specified options.
        /// </summary>
        public void DrawString(Resource<SpriteFont> font, Vector2 location, Color color, string format, params object[] args)
        {
            DrawString(font, location, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, format, args);
        }



        public void DrawString(Resource<SpriteFont> font, Vector2 location, Color color, Vector2 scale, string format, params object[] args)
        {
            DrawString(font, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, format, args);
        }



        public void DrawString(Resource<SpriteFont> font, Vector2 location, Color color, float rotation, Vector2 origin, string format, params object[] args)
        {
            DrawString(font, location, color, rotation, origin, Vector2.One, SpriteEffects.None, format, args);
        }



        public void DrawString(Resource<SpriteFont> font, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, string format, params object[] args)
        {
            DrawString(font, location, color, rotation, origin, scale, SpriteEffects.None, format, args);
        }



        public void DrawString(Resource<SpriteFont> font, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, string format, params object[] args)
        {
            if (font.IsNull)
            {
                Assert.Fatal(false, "FontRenderer.DrawString: Font resource is invalid!");
                return;
            }

            // draw each character in the string
            _fontBatch.Begin(SpriteBlendMode.AlphaBlend);

            // Skip the format if we have no arguments... this saves
            // us unnessasary garbage collection on static text.
            if (args.Length > 0)
            {
                string str = String.Format(format, args);
                _fontBatch.DrawString(font.Instance, str, location, color, rotation, origin, scale, effects, 0.0f);
            }
            else
                _fontBatch.DrawString(font.Instance, format, location, color, rotation, origin, scale, effects, 0.0f);

            _fontBatch.End();
        }

        #endregion


        #region Private, protected, internal fields

        private SpriteBatch _fontBatch;

        #endregion
    }
}
