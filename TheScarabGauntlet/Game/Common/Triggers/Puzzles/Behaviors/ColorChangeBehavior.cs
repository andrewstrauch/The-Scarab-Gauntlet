#region Using Directives
using System;

using Microsoft.Xna.Framework.Graphics;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Materials;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Core;
#endregion

namespace PlatformerStarter.Common.Triggers.Puzzles
{
    class ColorChangeBehavior : PuzzleBehavior
    {
        #region Private Members
        private Color newColor;
        #endregion

        #region Public Properties

        public Color NewColor
        {
            get { return newColor; }
            set { newColor = value; }
        }
        #endregion

        #region Public Routines

        /// <summary>
        /// Turns the object invisible (if it isn't already) and disables all collision.
        /// </summary>
        /// <param name="puzzleObject">The object to make disappear.</param>
        public override void Execute(T2DSceneObject puzzleObject)
        {
            T2DStaticSprite sprite = puzzleObject as T2DStaticSprite;

            if (sprite != null)
            {
                newColor = new Color(255, 0, 0, 0);
                SimpleMaterial material = TorqueObjectDatabase.Instance.FindObject<SimpleMaterial>(sprite.Material.Name);
                if (material == null)
                    return;

                Texture2D tex = material.Texture.Instance as Texture2D;
                SurfaceFormat format = tex.Format;

                int numPixels = tex.Width * tex.Height;
                Color[] data = new Color[numPixels];

                if (tex.GraphicsDevice.Textures[0] == tex)
                    tex.GraphicsDevice.Textures[0] = null;

                tex.GetData<Color>(data);

                for (int i = 0; i < numPixels; ++i)
                {
                    Color c = data[i];
                    c = newColor;
                    data[i] = c;
                }
                tex.SetData<Color>(data);
            }

            active = false;
        }

        #endregion
    }
}
