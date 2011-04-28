//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Provides support for post processing effects in the engine. A PostProcessor object can be set on the TorqueEngineComponent
    /// for full screen effects (i.e effects that affect the entirity of the screen and everything rendered to it, including GUI
    /// controls) or on the GuiSceneview to affect just the objects in the actual scene.
    /// 
    /// This PostProcessor just takes a single PostProcessMaterial and uses it to render the full screen quad to the back buffer.
    /// That can be used for simple effects like full screen blur. For more complex effects (like bloom), this class can be
    /// derived from, and have it's Run method redefined. For an example, see BloomPostProcessor.
    /// </summary>
    public class PostProcessor
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The material to render the full screen quad with.
        /// </summary>
        public PostProcessMaterial Material
        {
            get { return _material; }
            set { _material = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets up the effect. This is called once when the post processor is set on the GuiSceneview to
        /// initialize any necessary data for the effect. The base implementation does nothing.
        /// </summary>
        /// <param name="width">The width of the quad (in pixels) that will be rendered.</param>
        /// <param name="height">The height of the quad (in pixels) that will be rendered.</param>
        public virtual void Setup(int width, int height)
        {
        }



        /// <summary>
        /// Cleans up the effect. This is called once when the post processor is removed from the
        /// GuiSceneview. The base implementation does nothing.
        /// </summary>
        public virtual void Cleanup()
        {
        }



        /// <summary>
        /// This is called after the scene has been rendered to actually perform the post processing effect.
        /// The base implementation simply calls SceneRenderer.RenderManager.RenderQuad, passing it the
        /// material, position, and size to use for rendering.
        /// </summary>
        /// <param name="texture">The texture containing the rendered scene.</param>
        /// <param name="position">The position on the screen to render at.</param>
        /// <param name="size">The size of the quad to render.</param>
        public virtual void Run(Texture texture, Vector2 position, Vector2 size)
        {
            if (_material == null)
            {
                TorqueConsole.Warn("\nPostProcessor.Run - Cannot perform post processing. No material.");
                return;
            }

            GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = false;

            _material.Texture = texture;
            SceneRenderer.RenderManager.RenderQuad(_material, position, size);

            GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = true;
        }

        #endregion


        #region Private, protected, internal fields

        PostProcessMaterial _material;

        #endregion
    }
}
