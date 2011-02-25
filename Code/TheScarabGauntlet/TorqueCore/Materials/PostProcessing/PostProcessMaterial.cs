//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Material used for post processing effects. See PostProcessor for more information.
    /// </summary>
    public abstract class PostProcessMaterial : RenderMaterial
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The texture containing the rendered scene. This is set by the PostProcessor.
        /// </summary>
        public Texture Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            Assert.Fatal(_texture != null, "PostProcessMaterial._SetupGlobalParameters - Texture has not been set!");

            base._SetupGlobalParameters(srs, materialData);
            _baseTextureParameter.SetValue(_texture);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _baseTextureParameter = EffectManager.GetParameter(Effect, "baseTexture");
            Assert.Fatal(_baseTextureParameter != null, "PostProcessMaterial._LoadParameters - Unable to load parameter 'baseTexture'!");
        }



        protected override void _ClearParameters()
        {
            _baseTextureParameter = null;

            base._ClearParameters();
        }

        #endregion


        #region Private, protected, internal fields

        Texture _texture;
        EffectParameter _baseTextureParameter;

        #endregion
    }
}
