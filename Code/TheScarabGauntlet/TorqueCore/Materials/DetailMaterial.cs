//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using System.Xml.Serialization;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Material for rendering extra detail on an object. This is used by the XTerrain.
    /// 
    /// Parameters
    /// 
    /// worldMatrix: The object to world space matrix for the object being rendered
    /// worldViewProjection: The object to screen space matrix for the object being rendered
    /// detailTex: The detail texture
    /// detailTexRepeat: The distance each repeat of the texture 
    /// detailCenter: The camera position
    /// 
    /// Techniques
    /// 
    /// DetailEffect
    /// </summary>
    public class DetailMaterial : RenderMaterial
    {

        #region Constructors

        public DetailMaterial()
        {
            EffectFilename = "DetailEffect";
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The filename of the detail texture.
        /// </summary>
        public string TextureFilename
        {
            set { _textureFilename = value; _detailTex.Invalidate(); }
            get { return _textureFilename; }
        }



        /// <summary>
        /// The detail texture resource.
        /// </summary>
        [XmlIgnore]
        public Resource<Texture> DetailTexture
        {
            get { return _detailTex; }
            set { _detailTex = value; }
        }



        /// <summary>
        /// The number of times to repeat the texture across the surface.
        /// </summary>
        public float DetailTextureRepeat
        {
            get { return _detailTexRepeat; }
            set { _detailTexRepeat = value; }
        }



        /// <summary>
        /// The distance at which the detail texture is no longer rendered.
        /// </summary>
        public float DetailDistance
        {
            get { return _detailDistance; }
            set { _detailDistance = value; }
        }

        #endregion


        #region Public methods

        public override bool SetupPass()
        {
            if (!base.SetupPass())
                return false;

            if (_detailTex.IsNull || _detailTex.IsInvalid)
                return false;

            return true;
        }


        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (_detailTex.IsNull && !string.IsNullOrEmpty(_textureFilename))
                _detailTex = ResourceManager.Instance.LoadTexture(_textureFilename);

            srs.Gfx.Device.RenderState.SourceBlend = Blend.DestinationColor;
            srs.Gfx.Device.RenderState.DestinationBlend = Blend.SourceColor;

            return base._SetupEffect(srs, materialData);
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            if (!_detailTex.IsNull)
                _detailTexParameter.SetValue(_detailTex.Instance);

            _detailTexRepeatParameter.SetValue(_detailTexRepeat);
            _detailCenterParameter.SetValue(srs.CameraPosition);
            _detailDistanceParameter.SetValue(_detailDistance);
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldMatrixParameter, renderInstance.ObjectTransform);
            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _worldMatrixParameter = EffectManager.GetParameter(Effect, "worldMatrix");
            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
            _detailTexParameter = EffectManager.GetParameter(Effect, "detailTex");
            _detailTexRepeatParameter = EffectManager.GetParameter(Effect, "detailTexRepeat");
            _detailCenterParameter = EffectManager.GetParameter(Effect, "detailCenter");
            _detailDistanceParameter = EffectManager.GetParameter(Effect, "detailDistance");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _worldMatrixParameter = null;
            _worldViewProjectionParameter = null;
            _detailTexParameter = null;
            _detailTexRepeatParameter = null;
            _detailCenterParameter = null;
            _detailDistanceParameter = null;
        }

        #endregion


        #region Private, protected, internal fields

        EffectParameter _worldMatrixParameter;
        EffectParameter _worldViewProjectionParameter;
        protected EffectParameter _detailTexParameter;
        protected EffectParameter _detailTexRepeatParameter;
        protected EffectParameter _detailCenterParameter;
        protected EffectParameter _detailDistanceParameter;

        protected string _textureFilename;
        protected Resource<Texture> _detailTex;
        protected float _detailTexRepeat = 128.0f;
        protected float _detailDistance = 150.0f;

        #endregion
    }
}
