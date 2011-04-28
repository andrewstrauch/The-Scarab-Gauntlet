//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GFX;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// PostProcessMaterial that does a Gaussian blur.
    /// </summary>
    public class GaussianBlurMaterial : PostProcessMaterial
    {

        #region Constructors

        public GaussianBlurMaterial()
        {
            EffectFilename = "GaussianBlurEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The amount to blur the image.
        /// </summary>
        public float BlurAmount
        {
            get { return _blurAmount; }
            set { _blurAmount = value; }
        }



        /// <summary>
        /// The size of the blur effect.
        /// </summary>
        public Vector2 Size
        {
            get { return _size; }
            set { _size = value; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _weightsParameter = EffectManager.GetParameter(Effect, "sampleWeights");
            _offsetsParameter = EffectManager.GetParameter(Effect, "sampleOffsets");

            _sampleCount = _weightsParameter.Elements.Count;
            _sampleWeights = new float[_sampleCount];
            _sampleOffsets = new Vector2[_sampleCount];
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _weightsParameter = null;
            _offsetsParameter = null;
            _sampleWeights = null;
            _sampleOffsets = null;
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            _sampleWeights[0] = _ComputeGaussian(0);
            _sampleOffsets[0].X = 0.0f;
            _sampleOffsets[0].Y = 0.0f;

            float totalWeights = _sampleWeights[0];

            for (int i = 0; i < (_sampleCount >> 1); i++)
            {
                float weight = _ComputeGaussian(i + 1);

                _sampleWeights[i * 2 + 1] = weight;
                _sampleWeights[i * 2 + 2] = weight;

                totalWeights += weight * 2;
                float sampleOffset = i * 2 + 1.5f;

                Vector2 delta = _size * sampleOffset;
                _sampleOffsets[i * 2 + 1] = delta;
                _sampleOffsets[i * 2 + 2] = -delta;
            }

            for (int i = 0; i < _sampleCount; i++)
                _sampleWeights[i] /= totalWeights;

            _weightsParameter.SetValue(_sampleWeights);
            _offsetsParameter.SetValue(_sampleOffsets);
        }



        float _ComputeGaussian(float n)
        {
            return (float)((1.0 / Math.Sqrt(2 * Math.PI * _blurAmount)) * Math.Exp(-(n * n) / (2 * _blurAmount * _blurAmount)));
        }

        #endregion


        #region Private, protected, internal fields

        EffectParameter _weightsParameter;
        EffectParameter _offsetsParameter;

        int _sampleCount;
        float[] _sampleWeights;
        Vector2[] _sampleOffsets;

        float _blurAmount = 4.0f;

        Vector2 _size;

        #endregion
    }



    /// <summary>
    /// PostProcessMaterial that extracts colors from a texture that are above a certain intensity threshold.
    /// </summary>
    public class BloomExtractMaterial : PostProcessMaterial
    {

        #region Constructors

        public BloomExtractMaterial()
        {
            EffectFilename = "BloomExtractEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The intensity value at which pixels will be dropped from the bloom texture.
        /// </summary>
        public float BloomThreshold
        {
            get { return _bloomThreshold; }
            set { _bloomThreshold = value; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _LoadParameters()
        {
            base._LoadParameters();
            _bloomThresholdParameter = EffectManager.GetParameter(Effect, "bloomThreshold");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();
            _bloomThresholdParameter = null;
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);
            _bloomThresholdParameter.SetValue(_bloomThreshold);
        }

        #endregion


        #region Private, protected, internal fields

        float _bloomThreshold = 0.25f;
        EffectParameter _bloomThresholdParameter;

        #endregion
    }



    /// <summary>
    /// PostProcessMaterial that combines two different textures based on bloom settings.
    /// </summary>
    public class BloomCombineMaterial : PostProcessMaterial
    {

        #region Constructors

        public BloomCombineMaterial()
        {
            EffectFilename = "BloomCombineEffect";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene texture after it has passed through the extract and blur passes.
        /// </summary>
        public Texture2D Texture2
        {
            get { return _texture2; }
            set { _texture2 = value; }
        }



        /// <summary>
        /// The intensity of the pixels from the bloom texture (the extracted and blurred view of the scene).
        /// </summary>
        public float BloomIntensity
        {
            get { return _bloomIntensity; }
            set { _bloomIntensity = value; }
        }



        /// <summary>
        /// The intensity of the pixels from the base texture (the rendered view of the scene).
        /// </summary>
        public float BaseIntensity
        {
            get { return _baseIntensity; }
            set { _baseIntensity = value; }
        }



        /// <summary>
        /// The saturation of the pixels from the bloom texture (the extracted and blurred view of the scene).
        /// </summary>
        public float BloomSaturation
        {
            get { return _bloomSaturation; }
            set { _bloomSaturation = value; }
        }



        /// <summary>
        /// The saturation of the pixels from the base texture (the rendered view of the scene).
        /// </summary>
        public float BaseSaturation
        {
            get { return _baseSaturation; }
            set { _baseSaturation = value; }
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _bloomIntensityParameter = EffectManager.GetParameter(Effect, "BloomIntensity");
            _baseIntensityParameter = EffectManager.GetParameter(Effect, "BaseIntensity");
            _bloomSaturationParameter = EffectManager.GetParameter(Effect, "BloomSaturation");
            _baseSaturationParameter = EffectManager.GetParameter(Effect, "BaseSaturation");
            _texture2Parameter = EffectManager.GetParameter(Effect, "baseTexture2");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            _bloomIntensityParameter.SetValue(_bloomIntensity);
            _baseIntensityParameter.SetValue(_baseIntensity);
            _bloomSaturationParameter.SetValue(_bloomSaturation);
            _baseSaturationParameter.SetValue(_baseSaturation);
            _texture2Parameter.SetValue(_texture2);
        }

        #endregion


        #region Private, protected, internal fields

        float _bloomIntensity = 1.25f;
        float _baseIntensity = 1.0f;
        float _bloomSaturation = 1.0f;
        float _baseSaturation = 1.0f;

        Texture2D _texture2;

        EffectParameter _bloomIntensityParameter;
        EffectParameter _baseIntensityParameter;
        EffectParameter _bloomSaturationParameter;
        EffectParameter _baseSaturationParameter;
        EffectParameter _texture2Parameter;

        #endregion
    }



    /// <summary>
    /// Post processor that implements bloom. This is a multi pass effect, so it will affect
    /// performance, particularly on fill rate limited cards.
    /// </summary>
    public class BloomPostProcessor : PostProcessor, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The material to use to extract the bright colors from the scene.
        /// </summary>
        public BloomExtractMaterial ExtractMaterial
        {
            get { return _extractMaterial; }
        }



        /// <summary>
        /// The material to use to combine the blurred extracted texture with the
        /// base texture.
        /// </summary>
        public BloomCombineMaterial CombineMaterial
        {
            get { return _combineMaterial; }
        }



        /// <summary>
        /// The material to use to blur the scene.
        /// </summary>
        public GaussianBlurMaterial BlurMaterial
        {
            get { return _blurMaterial; }
        }

        #endregion


        #region Public methods

        public override void Setup(int width, int height)
        {
            _renderTarget1 = ResourceManager.Instance.CreateRenderTarget2D(width >> 1, height >> 1, 1, SurfaceFormat.Color);
            _renderTarget2 = ResourceManager.Instance.CreateRenderTarget2D(width >> 1, height >> 1, 1, SurfaceFormat.Color);

            base.Setup(width, height);
        }



        public override void Cleanup()
        {
            _renderTarget1.Invalidate();
            _renderTarget2.Invalidate();

            base.Cleanup();
        }



        public override void Run(Texture texture, Vector2 position, Vector2 size)
        {
            GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = false;

            _extractMaterial.Texture = texture;
            GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget1.Instance);
            SceneRenderer.RenderManager.RenderQuad(_extractMaterial, Vector2.Zero, new Vector2(_renderTarget1.Instance.Width, _renderTarget1.Instance.Height));

            GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget2.Instance);
            _blurMaterial.Texture = _renderTarget1.Instance.GetTexture();
            _blurMaterial.Size = new Vector2(1.0f / (float)_renderTarget1.Instance.Width, 0.0f);
            SceneRenderer.RenderManager.RenderQuad(_blurMaterial, Vector2.Zero, new Vector2(_renderTarget2.Instance.Width, _renderTarget2.Instance.Height));

            GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget1.Instance);
            _blurMaterial.Texture = _renderTarget2.Instance.GetTexture();
            _blurMaterial.Size = new Vector2(0.0f, 1.0f / (float)_renderTarget1.Instance.Height);
            SceneRenderer.RenderManager.RenderQuad(_blurMaterial, Vector2.Zero, new Vector2(_renderTarget1.Instance.Width, _renderTarget1.Instance.Height));

            GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget2.Instance);

            _combineMaterial.Texture = texture;
            _combineMaterial.Texture2 = _renderTarget1.Instance.GetTexture();
            TorqueEngineComponent.Instance.ReapplyMainRenderTarget();
            SceneRenderer.RenderManager.RenderQuad(_combineMaterial, position, size);

            GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = true;
        }

        #endregion


        #region Private, protected, internal fields

        Resource<RenderTarget2D> _renderTarget1;
        Resource<RenderTarget2D> _renderTarget2;

        BloomExtractMaterial _extractMaterial = new BloomExtractMaterial();
        BloomCombineMaterial _combineMaterial = new BloomCombineMaterial();
        GaussianBlurMaterial _blurMaterial = new GaussianBlurMaterial();

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Cleanup();
        }

        #endregion
    }
}
