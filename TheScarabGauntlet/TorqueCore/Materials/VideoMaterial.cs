//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
// Author: Jason Cahill
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System.Threading;



namespace GarageGames.Torque.Materials
{
    public class VideoMaterial : RenderMaterial, ITextureMaterial
    {

        #region Constructors

        public VideoMaterial()
        {
            EffectFilename = "SimpleEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The filename of the texture to use.
        /// </summary>
        public string VideoFilename { get; set; }



        /// <summary>
        /// The opacity to render at.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }



        public bool IsLooped
        {
            get { return _isLooped; }
            set { _isLooped = value; }
        }



        public bool IsStopped
        {
            get { return _isStopped; }
            set { _isStopped = value; }
        }



        public MediaState VideoState
        {
            get { return _videoPlayer.State; }
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



        string ITextureMaterial.TextureFilename
        {
            get
            {
                return VideoFilename;
            }
            set
            {
                VideoFilename = value;
            }
        }



        Resource<Texture> ITextureMaterial.Texture
        {
            get
            {
                //return CurrentVideoFrameTexture as Resource<Texture>;
                return ResourceManager.Instance.CreateResource<Texture>((Texture)CurrentVideoFrameTexture.Instance);
            }
        }



        /// <summary>
        /// The texture object loaded from TextureFilename.
        /// </summary>
        public Resource<Texture2D> CurrentVideoFrameTexture
        {
            get
            {
                if (_isInvalid && !string.IsNullOrEmpty(VideoFilename))
                {
                    _video = ResourceManager.Instance.GlobalContentManager.Load<Video>(VideoFilename);
                    _videoPlayer = new VideoPlayer();
                    _videoPlayer.Play(_video);

                    _isStopped = false;
                    while (_videoPlayer.PlayPosition == TimeSpan.Zero)
                        Thread.Sleep(1);

                    _texture = ResourceManager.Instance.CreateResource<Texture2D>(_videoPlayer.GetTexture());
                    _isInvalid = false;
                }

                if (_videoPlayer.State == MediaState.Stopped)
                    _isStopped = true;

                _videoPlayer.IsLooped = _isLooped;
                _videoPlayer.GetTexture();
                return _texture;
            }
        }


        #endregion


        #region Public methods

        /// <summary>
        /// Sets the texture directly on the material, rather than looking it up from TextureFilename.
        /// </summary>
        /// <param name="texture"></param>
        public void SetTexture(Texture2D texture)
        {
            _texture = ResourceManager.Instance.CreateResource<Texture2D>(texture);
            VideoFilename = string.Empty;
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_texture.IsNull)
            {
                //_texture.Instance.Dispose();
                _texture.Invalidate();
            }
            _video = null;
            _videoPlayer = null;
            base.Dispose();
        }



        public void Play()
        {
            if (_videoPlayer != null)
                _videoPlayer.Play(_video);
            else
            {
                if (_isInvalid && !string.IsNullOrEmpty(VideoFilename))
                {
                    _video = ResourceManager.Instance.GlobalContentManager.Load<Video>(VideoFilename);
                    _videoPlayer = new VideoPlayer();
                    _videoPlayer.Play(_video);
                }
            }
            _isStopped = false;
        }



        public void Stop()
        {
            _videoPlayer.Stop();
            _isStopped = true;
        }



        public void Pause()
        {
            _videoPlayer.Pause();
        }



        public void Resume()
        {
            _videoPlayer.Resume();
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if ((_texture.IsNull || _texture.IsInvalid) && !string.IsNullOrEmpty(VideoFilename))
                _texture = ResourceManager.Instance.CreateResource<Texture2D>(_videoPlayer.GetTexture());

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

            if (!_texture.IsNull)
                EffectManager.SetParameter(_baseTextureParameter, CurrentVideoFrameTexture.Instance);
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);

            if (IsTranslucent)
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
        bool _isColorBlended = false;
        bool _isCopyPass = false;
        Resource<Texture2D> _texture;

        bool _isInvalid = true;
        bool _isLooped = true;

        Video _video = null;
        VideoPlayer _videoPlayer = null;

        EffectParameter _worldViewProjectionParameter;
        EffectParameter _baseTextureParameter;
        EffectParameter _opacityParameter;

        bool _isStopped = false;

        #endregion




    }
}