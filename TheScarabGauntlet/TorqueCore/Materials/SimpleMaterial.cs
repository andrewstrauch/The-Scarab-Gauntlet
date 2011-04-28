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
    /// Extremely basic material that simply renders a texture or solid color.
    /// 
    /// Parameters
    /// 
    /// worldViewProjection: the world space to screen space matrix
    /// baseTexture: the texture
    /// opacity: visibility level from 0 (invisible) to 1 (opaque)
    /// 
    /// Techniques
    /// 
    /// CopyTechnique: IsCopyPass true
    /// TexturedTechnique: Texture set and IsColorBlended false
    /// ColorTextureBlendTechnique: Texture set and IsColorBlended true
    /// ColoredTechnique: Texture not set
    /// </summary>
    public class SimpleMaterial : RenderMaterial, ITextureMaterial
    {

        #region Constructors

        public SimpleMaterial()
        {
            EffectFilename = "SimpleEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The filename of the texture to use.
        /// </summary>
        public string TextureFilename
        {
            get { return _textureFilename; }
            set { _textureFilename = value; _textureMissing = false; }
        }



        /// <summary>
        /// The opacity to render at.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }



        /// <summary>
        /// Whether or not to blend the color with the texture. The color is read from
        /// the vertex data.
        /// </summary>
        public bool IsColorBlended
        {
            get { return _isColorBlended; }
            set { _isColorBlended = value; }
        }



        /// <summary>
        /// Whether or not this is a copy pass. A copy pass will not use any filtering
        /// when sampling textures.
        /// </summary>
        public bool IsCopyPass
        {
            get { return _isCopyPass; }
            set { _isCopyPass = value; }
        }



        /// <summary>
        /// The texture object loaded from TextureFilename.
        /// </summary>
        public Resource<Texture> Texture
        {
            get
            {
                if (!_textureMissing && _texture.IsInvalid && !string.IsNullOrEmpty(_textureFilename))
                    _texture = ResourceManager.Instance.LoadTexture(_textureFilename);

                return _texture;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the texture directly on the material, rather than looking it up from TextureFilename.
        /// </summary>
        /// <param name="texture"></param>
        public void SetTexture(Texture texture)
        {
            _texture = ResourceManager.Instance.CreateResource<Texture>(texture);
            _textureFilename = string.Empty;
            _textureMissing = false;
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_texture.IsNull)
            {
                //_texture.Instance.Dispose();
                _texture.Invalidate();
            }
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (!_textureMissing && (_texture.IsNull || _texture.IsInvalid) && !string.IsNullOrEmpty(_textureFilename))
            {
                _texture = ResourceManager.Instance.LoadTexture(_textureFilename);
                if (_texture.IsNull)
                    _textureMissing = true;
            }

            if (_isCopyPass)
                return "CopyTechnique";

            if (!_texture.IsNull)
            {
                if (_isColorBlended)
                    return "ColorTextureBlendTechnique";

                return "TexturedTechnique";
            }

            return "ColoredTechnique";
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            if (_texture.Instance != null && _texture.Instance.IsDisposed)
                _texture.Invalidate();

            if (!_texture.IsNull)
                EffectManager.SetParameter(_baseTextureParameter, Texture.Instance);
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
            EffectManager.SetParameter(_opacityParameter, renderInstance.Opacity * _opacity);
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

        #endregion


        #region Private, protected, internal fields

        float _opacity = 1.0f;
        bool _isColorBlended = true;
        bool _isCopyPass = false;
        string _textureFilename = string.Empty;
        Resource<Texture> _texture;
        bool _textureMissing = false;

        EffectParameter _worldViewProjectionParameter;
        EffectParameter _baseTextureParameter;
        EffectParameter _opacityParameter;

        #endregion
    }
}
