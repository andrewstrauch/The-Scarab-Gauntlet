//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Materials;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// PostProcessMaterial that simply copies the texture directly to the back buffer.
    /// </summary>
    public class CopyPostProcessMaterial : PostProcessMaterial
    {

        #region Constructors

        public CopyPostProcessMaterial()
        {
            EffectFilename = "SimpleEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The opacity at which to render the effect. Setting this to something less than one
        /// will give a fake motion blur effect.
        /// </summary>
        public float BlendAmount
        {
            get { return _blendAmount; }
            set { _blendAmount = value; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (_blendAmount < 1.0f)
            {
                srs.Gfx.Device.RenderState.AlphaBlendEnable = true;
                srs.Gfx.Device.RenderState.SourceBlend = Blend.SourceAlpha;
                srs.Gfx.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            }

            return "CopyTechnique";
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);
            EffectManager.SetParameter(_opacityParameter, _blendAmount);
            EffectManager.SetParameter(_worldViewProjectionParameter, Matrix.Identity);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();
            _opacityParameter = EffectManager.GetParameter(Effect, "opacity");
            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();
            _opacityParameter = null;
            _worldViewProjectionParameter = null;
        }

        #endregion


        #region Private, protected, internal fields

        float _blendAmount = 1.0f;
        EffectParameter _opacityParameter;
        EffectParameter _worldViewProjectionParameter;

        #endregion
    }
}
