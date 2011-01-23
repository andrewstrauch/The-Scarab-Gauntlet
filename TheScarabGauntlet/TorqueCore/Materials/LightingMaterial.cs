//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Lighting;
using Microsoft.Xna.Framework;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Enumeration of supported lighting modes.
    /// </summary>
    public enum LightingMode
    {
        /// <summary>
        /// No lighting.
        /// </summary>
        None = 0,

        /// <summary>
        /// Light values calculated on each vertex.
        /// </summary>
        PerVertex,

        /// <summary>
        /// Light values calculated on each pixel.
        /// </summary>
        PerPixel
    }



    /// <summary>
    /// Material for objects that receive lighting. The material grabs lights from the
    /// scenegraph based on the maximum number of supported lights and the affect each
    /// light will have on the object. Those with greater affect will be given higher
    /// priority. Lights are usually created through the LightComponent and will be
    /// automatically added to the scenegraph, and thus this material.
    /// 
    /// Normal mapping is enabled if a normal map is set via the NormalMapFilename property.
    /// Specular highlights are enabled if both specular power and intensity are greater than
    /// 0 and the maximum supported shader model is at least 2. Various n
    /// 
    /// This material by default uses the LightingEffect3D effect file. For 2D materials, set
    /// EffectFilename to LightingEffect2D. This is done automatically by TXB.
    /// 
    /// Parameters
    /// 
    /// _worldMatrix: The object to world space matrix for the object being rendered
    /// _worldViewProjection: The object to screen space matrix for the object being rendered
    /// _baseTexture: The base texture
    /// _normalMap: The normal map
    /// _specularPower: Specular power
    /// _specularIntensity: Specular intensity
    /// _specularColor: Specular color
    /// _opacity: Visibility level of the object
    /// _lightPosition: Positions of the lights
    /// _lightDiffuse: Diffuse light colors
    /// _lightAmbient: Ambient light colors
    /// _lightAttenuation: Constant and linear attenuation of each light
    /// _lightCount: The number of lights set. Lights from this number to max lights have all values set to 0
    /// _cameraPosition: Position of the camera
    /// 
    /// Techniques
    /// 
    /// NoLightingTechnique: LightingMode set to None
    /// VertexLightingSpecularNormalMapTechnique: PerVertex lighting, normal map enabled, and specular enabled
    /// VertexLightingNormalMapTechnique: PerVertex lighting, normal map enabled, specular disabled
    /// VertexLightingSpecularTechnique: PerVertex lighting, normal map disabled, specular enabled
    /// VertexLightingTechnique: PerVertex lighting, normal map disabled, specular disabled
    /// PixelLightingSpecularNormalMapTechnique: PerPixel lighting, normal map enabled, and specular enabled, shader model 2
    /// PixelLightingNormalMapTechnique: PerPixel lighting, normal map enabled, specular disabled, shader model 2
    /// PixelLightingSpecularTechnique: PerPixel lighting, normal map disabled, specular enabled, shader model 2
    /// PixelLightingTechnique: PerPixel lighting, normal map disabled, specular disabled, shader model 2
    /// PixelLightingSpecularNormalMapTechnique3_0: PerPixel lighting, normal map enabled, and specular enabled
    /// PixelLightingNormalMapTechnique3_0: PerPixel lighting, normal map enabled, specular disabled
    /// PixelLightingSpecularTechnique3_0: PerPixel lighting, normal map disabled, specular enabled
    /// PixelLightingTechnique3_0: PerPixel lighting, normal map disabled, specular disabled
    /// </summary>
    public class LightingMaterial : RenderMaterial, ITextureMaterial
    {

        #region Static methods, fields, constructors

        // if this is changed, change the values in the effects as well (array subscripts for
        // light parameters and uniform values passed to shader methods from the techniques)
        static int _globalMaxLightCount = 8;

        #endregion


        #region Constructors

        public LightingMaterial()
        {
            EffectFilename = "LightingEffect3D";
            _maxLights = _globalMaxLightCount;

            _positions = new Vector3[_maxLights];
            _colors = new Vector3[_maxLights];
            _ambients = new Vector3[_maxLights];
            _attenuations = new Vector2[_maxLights];
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The filename of the texture to use.
        /// </summary>
        public string TextureFilename
        {
            get { return _textureFilename; }
            set { _textureFilename = value; }
        }



        /// <summary>
        /// The filename of the normal map to use.
        /// </summary>
        public string NormalMapFilename
        {
            get { return _normalMapFilename; }
            set { _normalMapFilename = value; }
        }



        /// <summary>
        /// Whether or not lighting is enabled. If this is set to true, the lighting mode is
        /// set to PerVertex. True will be returned if PerVertex or PerPixel is set.
        /// </summary>
        public bool IsLightingEnabled
        {
            get { return _lightingMode > LightingMode.None; }
            set { _lightingMode = value ? LightingMode.PerVertex : LightingMode.None; }
        }



        /// <summary>
        /// The type of lighting to use. PerVertex isn't as high of quality as PerPixel, but
        /// it is less expensive and can look just as good on meshes with small equal sized gaps
        /// between vertices. Also, if their are no dynamic lights in the scene, just a sunlight
        /// for instance, than PerPixel is probably not necessary.
        /// </summary>
        public LightingMode LightingMode
        {
            get { return _lightingMode; }
            set { _lightingMode = value; }
        }



        /// <summary>
        /// The maximum supported number of lights. By default this is 8, though on cards that
        /// support less than shader model 3, fewer will be used. But, this can be used to limit
        /// the number even more to save performance.
        /// </summary>
        public int MaxSupportedLights
        {
            get { return _maxLights; }
            set
            {
                if (value > _globalMaxLightCount)
                    value = _globalMaxLightCount;

                _maxLights = value;

                _positions = new Vector3[_maxLights];
                _colors = new Vector3[_maxLights];
                _ambients = new Vector3[_maxLights];
                _attenuations = new Vector2[_maxLights];
            }
        }



        /// <summary>
        /// The visibility level of the object.
        /// </summary>
        public float Opacity
        {
            get { return _opacity; }
            set { _opacity = value; }
        }



        /// <summary>
        /// The strength of the specular highlights.
        /// </summary>
        public float SpecularPower
        {
            get { return _specularPower; }
            set { _specularPower = value; }
        }



        /// <summary>
        /// The percentage of the specular highlights to be applied in the effect.
        /// </summary>
        public float SpecularIntensity
        {
            get { return _specularIntensity; }
            set { _specularIntensity = value; }
        }



        /// <summary>
        /// The color of specular highlights.
        /// </summary>
        public Vector3 SpecularColor
        {
            get { return _specularColor; }
            set { _specularColor = value; }
        }



        /// <summary>
        /// The texture resource loaded from TextureFilename.
        /// </summary>
        public Resource<Texture> Texture
        {
            get { return _baseTexture; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the texture on the material, overriding whatever was loaded from TextureFilename.
        /// </summary>
        /// <param name="texture"></param>
        public void SetTexture(Texture texture)
        {
            _baseTexture = ResourceManager.Instance.CreateResource<Texture>(texture);
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            // initialize the arrays
            MaxSupportedLights = _maxLights;
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_baseTexture.IsNull)
            {
                //_baseTexture.Instance.Dispose();
                _baseTexture.Invalidate();
            }
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            if (_baseTexture.IsNull && !string.IsNullOrEmpty(_textureFilename))
                _baseTexture = ResourceManager.Instance.LoadTexture(_textureFilename);

            if (_normalMap.IsNull && !string.IsNullOrEmpty(_normalMapFilename))
                _normalMap = ResourceManager.Instance.LoadTexture(_normalMapFilename);

            bool doNormalMap = !_normalMap.IsNull;
            bool doSpecular = _specularPower > 0.0f && _specularIntensity > 0.0f && srs.Gfx.ShaderProfile >= ShaderProfile.PS_2_0;

            if (_lightingMode == LightingMode.None)
                return "NoLightingTechnique";

            else if (_lightingMode == LightingMode.PerVertex)
            {
                if (doNormalMap)
                {
                    if (doSpecular)
                        return "VertexLightingSpecularNormalMapTechnique";
                    else
                        return "VertexLightingNormalMapTechnique";
                }
                else if (doSpecular)
                    return "VertexLightingSpecularTechnique";
                else
                    return "VertexLightingTechnique";
            }

            else
            {
                if (srs.Gfx.ShaderProfile < ShaderProfile.PS_3_0)
                {
                    if (doNormalMap)
                    {
                        if (doSpecular)
                            return "PixelLightingSpecularNormalMapTechnique";
                        else
                            return "PixelLightingNormalMapTechnique";
                    }
                    else if (doSpecular)
                        return "PixelLightingSpecularTechnique";
                    else
                        return "PixelLightingTechnique";
                }
                else
                {
                    if (doNormalMap)
                    {
                        if (doSpecular)
                            return "PixelLightingSpecularNormalMapTechnique3_0";
                        else
                            return "PixelLightingNormalMapTechnique3_0";
                    }
                    else if (doSpecular)
                        return "PixelLightingSpecularTechnique3_0";
                    else
                        return "PixelLightingTechnique3_0";
                }
            }
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            if (!_baseTexture.IsNull)
                EffectManager.SetParameter(_baseTextureParameter, _baseTexture.Instance);

            if (IsLightingEnabled)
            {
                bool doNormalMap = !_normalMap.IsNull;
                bool doSpecular = _specularPower > 0.0f && _specularIntensity > 0.0f && srs.Gfx.ShaderProfile >= ShaderProfile.PS_2_0;

                if (doNormalMap)
                    EffectManager.SetParameter(_normalMapParameter, _normalMap.Instance);

                if (doSpecular)
                {
                    EffectManager.SetParameter(_cameraPositionParameter, srs.CameraPosition);
                    EffectManager.SetParameter(_specularColorParameter, _specularColor);
                    EffectManager.SetParameter(_specularPowerParameter, _specularPower);
                    EffectManager.SetParameter(_specularIntensityParameter, _specularIntensity);
                }
            }
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionMatrixParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);
            EffectManager.SetParameter(_opacityParameter, renderInstance.Opacity * _opacity);

            if (IsLightingEnabled)
            {
                EffectManager.SetParameter(_worldMatrixParameter, renderInstance.ObjectTransform);

                List<Light> lights = srs.SceneGraph.GetLights(renderInstance.WorldBox, MaxSupportedLights);

                int count = lights.Count;
                EffectManager.SetParameter(_lightCountParameter, count);

                // get light data into the arrays to be passed to the shader:
                // here we have to fill the unused light slots with empty data 
                // to allow the shader to work on certain video cards
                int i = 0;
                for (; i < count; i++)
                {
                    _positions[i] = lights[i].Position;
                    _colors[i] = lights[i].DiffuseColor;
                    _ambients[i] = lights[i].AmbientColor;
                    _attenuations[i].X = lights[i].ConstantAttenuation;
                    _attenuations[i].Y = lights[i].LinearAttenuation;
                }

                if (count > 0)
                {
                    EffectManager.SetParameter(_lightPositionParameter, _positions);
                    EffectManager.SetParameter(_lightDiffuseParameter, _colors);
                    EffectManager.SetParameter(_lightAmbientParameter, _ambients);
                    EffectManager.SetParameter(_lightAttenuationParameter, _attenuations);
                }
            }
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _worldMatrixParameter = EffectManager.GetParameter(Effect, "_worldMatrix");
            _worldViewProjectionMatrixParameter = EffectManager.GetParameter(Effect, "_worldViewProjectionMatrix");
            _baseTextureParameter = EffectManager.GetParameter(Effect, "_baseTexture");
            _normalMapParameter = EffectManager.GetParameter(Effect, "_normalMap");
            _specularPowerParameter = EffectManager.GetParameter(Effect, "_specularPower");
            _specularIntensityParameter = EffectManager.GetParameter(Effect, "_specularIntensity");
            _specularColorParameter = EffectManager.GetParameter(Effect, "_specularColor");
            _opacityParameter = EffectManager.GetParameter(Effect, "_opacity");
            _lightPositionParameter = EffectManager.GetParameter(Effect, "_lightPosition");
            _lightDiffuseParameter = EffectManager.GetParameter(Effect, "_lightDiffuse");
            _lightAmbientParameter = EffectManager.GetParameter(Effect, "_lightAmbient");
            _lightAttenuationParameter = EffectManager.GetParameter(Effect, "_lightAttenuation");
            _lightCountParameter = EffectManager.GetParameter(Effect, "_lightCount");
            _cameraPositionParameter = EffectManager.GetParameter(Effect, "_cameraPosition");
        }

        protected override void _ClearParameters()
        {
            _worldMatrixParameter = null;
            _worldViewProjectionMatrixParameter = null;
            _baseTextureParameter = null;
            _normalMapParameter = null;
            _specularPowerParameter = null;
            _specularIntensityParameter = null;
            _specularColorParameter = null;
            _opacityParameter = null;
            _lightPositionParameter = null;
            _lightDiffuseParameter = null;
            _lightAmbientParameter = null;
            _lightAttenuationParameter = null;
            _lightCountParameter = null;
            _cameraPositionParameter = null;

            base._ClearParameters();
        }

        #endregion


        #region Private, protected, internal fields

        float _opacity = 1.0f;

        string _textureFilename = string.Empty;
        Resource<Texture> _baseTexture;

        string _normalMapFilename = string.Empty;
        Resource<Texture> _normalMap;

        LightingMode _lightingMode = LightingMode.PerVertex;
        float _specularPower = 0.0f;
        float _specularIntensity = 1.0f;
        Vector3 _specularColor = Vector3.One;

        EffectParameter _worldMatrixParameter;
        EffectParameter _worldViewProjectionMatrixParameter;

        EffectParameter _baseTextureParameter;
        EffectParameter _normalMapParameter;

        EffectParameter _opacityParameter;

        EffectParameter _cameraPositionParameter;
        EffectParameter _specularPowerParameter;
        EffectParameter _specularIntensityParameter;
        EffectParameter _specularColorParameter;

        EffectParameter _lightPositionParameter;
        EffectParameter _lightDiffuseParameter;
        EffectParameter _lightAmbientParameter;
        EffectParameter _lightAttenuationParameter;
        EffectParameter _lightCountParameter;

        int _maxLights;
        Vector3[] _positions;
        Vector3[] _colors;
        Vector3[] _ambients;
        Vector2[] _attenuations;

        #endregion
    }
}
