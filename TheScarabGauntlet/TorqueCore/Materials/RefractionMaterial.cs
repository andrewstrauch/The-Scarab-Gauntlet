//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Material that implements refraction.
    /// 
    /// Parameters
    /// 
    /// refractedTexture: The texture that is being refracted (the back buffer texture)
    /// normalMap: The normal map assigned to the material
    /// worldViewProjection: The world to screen space matrix for the object being rendered
    /// refractionAmount: The amount to refract the refracted texture as set on the material
    /// refractionViewport: The viewport that is being refracted
    /// refractionUVBounds: The minimum and maximum texture coordinates to use when looking up the back buffer
    /// 
    /// Techniques
    /// 
    /// RefractionTechnique_1_1: For shader model 1.1
    /// RefractionTechnique: For shader model 1.4 and above
    /// 
    /// </summary>
    public class RefractionMaterial : RenderMaterial, IRefractionMaterial
    {

        #region Constructors

        public RefractionMaterial()
        {
            EffectFilename = "RefractionEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool IsRefractive
        {
            get { return true; }
            set { }
        }



        /// <summary>
        /// This is only here to avoid warnings when deserializing 2D scenes. Don't actually use this
        /// property! TXB saves this property out since materials and textures have a 1 to 1 mapping.
        /// </summary>
        public string TextureFilename
        {
            get { return string.Empty; }
            set { }
        }



        /// <summary>
        /// The filename of the refraction normal map to use.
        /// </summary>
        public string NormalMapFilename
        {
            get { return _normalMapFilename; }
            set { _normalMapFilename = value; }
        }



        /// <summary>
        /// The amount to refract the texture.
        /// </summary>
        public float RefractionAmount
        {
            get { return _refractionAmount; }
            set { _refractionAmount = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the scene texture that will be refracted. This is called from the refraction manager.
        /// </summary>
        /// <param name="texture">The texture.</param>
        public void SetTexture(Texture2D texture)
        {
            _texture = texture;
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (_normalMap.IsNull && !string.IsNullOrEmpty(_normalMapFilename))
                _normalMap = ResourceManager.Instance.LoadTexture(_normalMapFilename);

            srs.Gfx.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            srs.Gfx.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            if (srs.Gfx.ShaderProfile < ShaderProfile.PS_1_4)
                return "RefractionTechnique_1_1";

            return "RefractionTechnique";
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _refractedTextureParameter = EffectManager.GetParameter(Effect, "refractedTexture");
            _normalMapParameter = EffectManager.GetParameter(Effect, "normalMap");
            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");

            _refractionAmountParameter = EffectManager.GetParameter(Effect, "refractionAmount");
            _refractionViewportParameter = EffectManager.GetParameter(Effect, "refractionViewport");
            _refractionUVBoundsParameter = EffectManager.GetParameter(Effect, "refractionUVBounds");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _refractedTextureParameter = null;
            _normalMapParameter = null;
            _worldViewProjectionParameter = null;

            _refractionAmountParameter = null;
            _refractionViewportParameter = null;
            _refractionUVBoundsParameter = null;
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            EffectManager.SetParameter(_refractedTextureParameter, _texture);
            EffectManager.SetParameter(_normalMapParameter, _normalMap.Instance);
            EffectManager.SetParameter(_refractionAmountParameter, _refractionAmount);

            if (_refractionViewportParameter != null)
            {
                Viewport view = srs.Gfx.Device.Viewport;
                float width = srs.Gfx.Device.DepthStencilBuffer.Width;
                float height = srs.Gfx.Device.DepthStencilBuffer.Height;

                Vector4 refractview = new Vector4();
                refractview.X = ((float)view.Width / width) * 0.5f;
                refractview.Y = ((float)view.Height / height) * 0.5f;
                refractview.Z = ((float)view.X / width) + refractview.X;
                refractview.W = ((float)view.Y / height) + refractview.Y;
                _refractionViewportParameter.SetValue(refractview);
            }
            if (_refractionUVBoundsParameter != null)
            {
                Viewport view = srs.Gfx.Device.Viewport;
                float width = srs.Gfx.Device.DepthStencilBuffer.Width;
                float height = srs.Gfx.Device.DepthStencilBuffer.Height;

                Vector4 uvminmax = new Vector4();
                uvminmax.X = (float)(view.X + 0.5) / width;
                uvminmax.Y = (float)(view.Y + 0.5) / height;
                uvminmax.Z = (float)(view.Width + view.X - 0.5) / width;
                uvminmax.W = (float)(view.Height + view.Y - 0.5) / height;
                _refractionUVBoundsParameter.SetValue(uvminmax);
            }
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
        }

        #endregion


        #region Private, protected, internal fields

        Texture2D _texture;

        string _normalMapFilename = string.Empty;
        Resource<Texture> _normalMap;

        float _refractionAmount;

        EffectParameter _refractedTextureParameter;
        EffectParameter _normalMapParameter;
        EffectParameter _worldViewProjectionParameter;

        EffectParameter _refractionAmountParameter;
        EffectParameter _refractionViewportParameter;
        EffectParameter _refractionUVBoundsParameter;

        #endregion
    }
}
