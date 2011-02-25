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
    /// Material used with the ZPassEffect for doing a Z pre pass.
    /// </summary>
    public class ZPassMaterial : RenderMaterial
    {
        public ZPassMaterial()
        {
            EffectFilename = "ZPassEffect";
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
            EffectManager.SetParameter(_opacityParameter, renderInstance.Opacity);

            ITextureMaterial material = renderInstance.Material as ITextureMaterial;
            if (material != null && !material.Texture.IsNull)
                EffectManager.SetParameter(_baseTextureParameter, material.Texture.Instance);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
            _baseTextureParameter = EffectManager.GetParameter(Effect, "baseTexture");
            _opacityParameter = EffectManager.GetParameter(Effect, "opacity");
        }



        protected override void _ClearParameters()
        {
            _worldViewProjectionParameter = null;
            _baseTextureParameter = null;
            _opacityParameter = null;

            base._ClearParameters();
        }



        EffectParameter _worldViewProjectionParameter;
        EffectParameter _baseTextureParameter;
        EffectParameter _opacityParameter;
    }
}
