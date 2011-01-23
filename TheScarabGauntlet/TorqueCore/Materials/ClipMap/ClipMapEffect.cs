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
using GarageGames.Torque.Lighting;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.Materials.ClipMap
{
    /// <summary>
    /// A material instance data to be used by objects rendering with a clip map.
    /// This allows an object to specify the range of clip levels it needs to
    /// be accurately represented. Use the static GetaMaterialData method
    /// to get a cached version of the material instance data. This allows
    /// the render manager to sort objects based on the instance of material
    /// instance data they are using for faster rendering.
    /// </summary>
    public class ClipMapMaterialInstanceData : MaterialInstanceData
    {

        #region Static members

        /// <summary>
        /// The maximum number of clip levels for which to cache material data. This should only be set
        /// at load time because it clears the existing cache when set.
        /// </summary>
        public static int MaxSupportedLevels
        {
            get { return _maxSupportedLevels; }
            set
            {
                // if it's the same value do nothing
                if (_maxSupportedLevels == value)
                    return;

                // clear the data array and store the new value
                _dataArray = null;
                _maxSupportedLevels = value;
            }
        }



        /// <summary>
        /// Returns a cached material instance data with the specified start and end level. If none exists with the 
        /// specified data, it will be created and returned. If start and end level are both below MaxSupportedLevels,
        /// the new data will be cached.
        /// </summary>
        /// <param name="startLevel">The most detailed clip level to use.</param>
        /// <param name="endLevel">The least detailed clip level to use.</param>
        /// <returns>A material data object with the specified start and end level.</returns>
        public static ClipMapMaterialInstanceData GetMaterialData(int startLevel, int endLevel)
        {
            if (startLevel >= _maxSupportedLevels || endLevel >= _maxSupportedLevels)
                return new ClipMapMaterialInstanceData(startLevel, endLevel);

            if (_dataArray == null)
                _dataArray = new ClipMapMaterialInstanceData[_maxSupportedLevels][];

            if (_dataArray[startLevel] == null)
                _dataArray[startLevel] = new ClipMapMaterialInstanceData[_maxSupportedLevels];

            if (_dataArray[startLevel][endLevel] == null)
                _dataArray[startLevel][endLevel] = new ClipMapMaterialInstanceData(startLevel, endLevel);

            return _dataArray[startLevel][endLevel];
        }

        private static int _maxSupportedLevels = 16;
        private static ClipMapMaterialInstanceData[][] _dataArray;

        #endregion


        #region Constructors

        private ClipMapMaterialInstanceData(int startLevel, int endLevel)
        {
            _startLevel = startLevel;
            _endLevel = endLevel;
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The lowest level needed to cover the full area of the object.
        /// </summary>
        public int StartLevel
        {
            get { return _startLevel; }
            set { _startLevel = value; }
        }



        /// <summary>
        /// The highest level needed to draw sufficient detail to the back buffer.
        /// </summary>
        public int EndLevel
        {
            get { return _endLevel; }
            set { _endLevel = value; }
        }

        #endregion


        #region Private, protected, internal fields

        int _startLevel;
        int _endLevel;

        #endregion
    }



    /// <summary>
    /// A material that uses a clip map to render an object.
    /// </summary>
    public class ClipMapEffect : RenderMaterial, IDisposable
    {

        #region Static fields

        // if this is changed, change the values in the effects as well (array subscripts for
        // light parameters and uniform values passed to shader methods from the techniques)
        static int _globalMaxLightCount = 8;

        #endregion


        #region Constructors

        public ClipMapEffect()
        {
            // this is a resource effect, so set the filename to the resource name
            EffectFilename = "ClipMap";

            // listen for the device reset event so we can re-initialize our image cache
            TorqueEventManager.ListenEvents<bool>(GFXDevice.Instance.DeviceCreated, OnDeviceCreated);
            TorqueEventManager.ListenEvents<bool>(GFXDevice.Instance.DeviceReset, OnDeviceCreated);

            // make sure our lighting arrays are initialized
            _InitializeLightingArrays();
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The base clip map used by this material.
        /// </summary>
        public ClipMap BaseClipMap
        {
            get { return _baseClipMap; }
            set
            {
                _baseClipMap = value;
                ReInitAllClipMaps(false);
            }
        }



        /// <summary>
        /// A dictionary of all the clip maps used by this material. This is used when multiple views are implemented.
        /// This does not need to be directly accessed for the clip map to function properly - only the base clip map
        /// needs to be set. The other clip maps, if any, will be automatically updated to match the changes in 
        /// the base clip map.
        /// </summary>
        public Dictionary<int, ClipMap> ClipMaps
        {
            get { return _clipMaps; }
        }



        /// <summary>
        /// Clip map per-vertex lighting is only supported in shader model 2 and up.
        /// </summary>
        public bool IsLightingEnabled
        {
            get { return _isLightingEnabled; }
            set { _isLightingEnabled = value; }
        }



        /// <summary>
        /// The max number of lights this effect will consider when lighting the terrain.
        /// </summary>
        public int MaxSupportedLights
        {
            get { return _maxLights; }
            set
            {
                if (value > _globalMaxLightCount)
                    value = _globalMaxLightCount;

                _maxLights = value;

                _InitializeLightingArrays();
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Re-initializes each clip map used by this effect to match the base clip map. Call this after changing
        /// the base clip map via the ClipMap property to assure that each wold view index will reflect the udpdated data.
        /// Only neccesary when specifically using more than one world view index (for split-screen, etc).
        /// </summary>
        public void ReInitAllClipMaps(bool reInitializeBase)
        {
            if (reInitializeBase)
                _baseClipMap.Initialize(_baseClipMap.ImageCache, _baseClipMap.TextureSize, BaseClipMap.ClipMapSize);

            foreach (int i in _clipMaps.Keys)
            {
                if (_clipMaps[i] == _baseClipMap)
                    continue;

                _clipMaps[i].Initialize(_baseClipMap.ImageCache.GetCopyOfInstance(), _baseClipMap.TextureSize, _baseClipMap.ClipMapSize);
            }
        }



        /// <summary>
        /// Attempt to recenter the clip map to the specified position.
        /// </summary>
        /// <param name="position">The new center position for the clip map.</param>
        public void RecenterClipMap(Vector2 position)
        {
            _baseClipMap.Center = position;
        }



        /// <summary>
        /// Attempt to recenter the clip map to the specified position.
        /// </summary>
        /// <param name="position">The new center position for the clip map.</param>
        /// <param name="forceFullUpdate">Specifies whether or not to force a recenter of all levels during the update.</param>
        public void RecenterClipMap(Vector2 position, bool forceFullUpdate)
        {
            _baseClipMap.Recenter(position, forceFullUpdate);
        }



        /// <summary>
        /// Attempt to recenter the clip map to the specified position.
        /// </summary>
        /// <param name="worldViewIndex">The world view index associated with the clip to recenter.</param>
        /// <param name="position">The new center position for the clip map.</param>
        public void RecenterClipMap(int worldViewIndex, Vector2 position)
        {
            // create a clip map for this world view index if none exists
            if (!_clipMaps.ContainsKey(worldViewIndex))
                _GenerateDuplicateClipMap(worldViewIndex);

            _clipMaps[worldViewIndex].Center = position;
        }



        /// <summary>
        /// Attempt to recenter the clip map to the specified position.
        /// </summary>
        /// <param name="worldViewIndex">The world view index associated with the clip to recenter.</param>
        /// <param name="position">The new center position for the clip map.</param>
        /// <param name="forceFullUpdate">Specifies whether or not to force a recenter of all levels during the update.</param>
        public void RecenterClipMap(int worldViewIndex, Vector2 position, bool forceFullUpdate)
        {
            // create a clip map for this world view index if none exists
            if (!_clipMaps.ContainsKey(worldViewIndex))
                _GenerateDuplicateClipMap(worldViewIndex);

            _clipMaps[worldViewIndex].Recenter(position, forceFullUpdate);
        }



        public void OnDeviceCreated(String eventName, bool blah)
        {
            // the graphics device was reset: reinitialize the clip map!
            // make sure the clip map has been filled with texture data
            ReInitAllClipMaps(true);
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            // cast the material instance data
            ClipMapMaterialInstanceData data = materialData as ClipMapMaterialInstanceData;

            // get the world view index from the scene render state
            int worldViewIndex = srs.WorldViewIndex;

            // create a clip map for this world view index if none exists
            if (!_clipMaps.ContainsKey(worldViewIndex))
                _GenerateDuplicateClipMap(worldViewIndex);

            // init start level and end level
            int endLevel;
            int startLevel;

            // check if we have a material instance data
            if (data != null)
            {
                // we have material instance data:
                // grab the start level and end level
                endLevel = data.EndLevel;
                startLevel = data.StartLevel;
            }
            else
            {
                // no data:
                // set default start and end level
                endLevel = _baseClipMap.ClipStackDepth - 1;
                startLevel = (int)MathHelper.Clamp(_baseClipMap.ClipStackDepth - 4, 0, _baseClipMap.ClipStackDepth);
            }

            // get the start level to end level delta
            int delta = endLevel - startLevel;

            // set up the texture fields and map info for the shader
            int i;
            ClipStackEntry entry;

            for (i = 0; i < delta + 1 && i < 4; i++)
            {
                entry = _clipMaps[worldViewIndex].ClipLevels[endLevel - i];

                _levelData[i] = new Quaternion(
                    entry.ClipCenter.X * entry.ScaleFactor,
                    entry.ClipCenter.Y * entry.ScaleFactor,
                    entry.ScaleFactor,
                    0.0f);

                _textures[i] = entry.Texture;
            }

            // null out the end of the array so we don't set old data
            for (; i < 4; i++)
                _textures[i] = null;

            // store the number of textures used
            int texturesUsed = delta + 1;

            // choose a technique.
            if (_isLightingEnabled && srs.Gfx.ShaderProfile >= ShaderProfile.PS_2_0)
            {
                // select a shader model 2 technique with lighting
                if (texturesUsed == 1)
                    return "ClipMap1_2_lit";
                else if (texturesUsed == 2)
                    return "ClipMap2_2_lit";
                else if (texturesUsed == 3)
                    return "ClipMap3_2_lit";
                else
                    return "ClipMap4_2_lit";
            }
            else
            {
                // select a shader model 1 technique (no terrain lighting)
                if (texturesUsed == 1)
                    return "ClipMap1_1";
                else if (texturesUsed == 2)
                    return "ClipMap2_1";
                else if (texturesUsed == 3)
                    return "ClipMap3_1";
                else
                    return "ClipMap4_1";
            }
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            if (_textures[0] != null)
                EffectManager.SetParameter(_diffuseMap1Parameter, _textures[0]);
            if (_textures[1] != null)
                EffectManager.SetParameter(_diffuseMap2Parameter, _textures[1]);
            if (_textures[2] != null)
                EffectManager.SetParameter(_diffuseMap3Parameter, _textures[2]);
            if (_textures[3] != null)
                EffectManager.SetParameter(_diffuseMap4Parameter, _textures[3]);

            _mapInfoParameter.SetValue(_levelData);
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_worldViewProjectionParameter, renderInstance.ObjectTransform * srs.View * srs.Projection);

            if (IsLightingEnabled && srs.Gfx.ShaderProfile >= ShaderProfile.PS_2_0)
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
                for (; i < MaxSupportedLights; i++)
                {
                    _positions[i] = Vector3.Zero;
                    _colors[i] = Vector3.One;
                    _ambients[i] = Vector3.One;
                    _attenuations[i].X = 1.0f;
                    _attenuations[i].Y = 1.0f;
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

            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");
            _mapInfoParameter = EffectManager.GetParameter(Effect, "mapInfo");

            _diffuseMap1Parameter = EffectManager.GetParameter(Effect, "clipLevel1");
            _diffuseMap2Parameter = EffectManager.GetParameter(Effect, "clipLevel2");
            _diffuseMap3Parameter = EffectManager.GetParameter(Effect, "clipLevel3");
            _diffuseMap4Parameter = EffectManager.GetParameter(Effect, "clipLevel4");

            _worldMatrixParameter = EffectManager.GetParameter(Effect, "worldMatrix");
            _lightPositionParameter = EffectManager.GetParameter(Effect, "lightPosition");
            _lightDiffuseParameter = EffectManager.GetParameter(Effect, "lightDiffuse");
            _lightAmbientParameter = EffectManager.GetParameter(Effect, "lightAmbient");
            _lightAttenuationParameter = EffectManager.GetParameter(Effect, "lightAttenuation");
            _lightCountParameter = EffectManager.GetParameter(Effect, "lightCount");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _worldViewProjectionParameter = null;
            _mapInfoParameter = null;

            _diffuseMap1Parameter = null;
            _diffuseMap2Parameter = null;
            _diffuseMap3Parameter = null;
            _diffuseMap4Parameter = null;

            _worldMatrixParameter = null;
            _lightPositionParameter = null;
            _lightDiffuseParameter = null;
            _lightAmbientParameter = null;
            _lightAttenuationParameter = null;
            _lightCountParameter = null;
        }



        protected void _GenerateDuplicateClipMap(int worldViewIndex)
        {
            // if this is the first world view we've been asked to draw, use the base clip map
            if (_clipMaps.Count == 0 && _baseClipMap != null)
            {
                _clipMaps[worldViewIndex] = _baseClipMap;

                return;
            }

            // assert that we have a base clip map
            Assert.Fatal(_baseClipMap != null, "ClipMapEffect - No clip map set!");

            // create a copy of the base clip map
            ClipMap newClipMap = new ClipMap();
            newClipMap.Initialize(_baseClipMap.ImageCache.GetCopyOfInstance(), _baseClipMap.TextureSize, _baseClipMap.ClipMapSize);
            _clipMaps[worldViewIndex] = newClipMap;
        }



        protected void _InitializeLightingArrays()
        {
            _positions = new Vector3[_maxLights];
            _colors = new Vector3[_maxLights];
            _ambients = new Vector3[_maxLights];
            _attenuations = new Vector2[_maxLights];
        }

        #endregion


        #region Private, protected, internal fields

        private ClipMap _baseClipMap;
        private Dictionary<int, ClipMap> _clipMaps = new Dictionary<int, ClipMap>();

        protected EffectParameter _worldViewProjectionParameter;
        protected EffectParameter _mapInfoParameter;
        protected EffectParameter _diffuseMap1Parameter;
        protected EffectParameter _diffuseMap2Parameter;
        protected EffectParameter _diffuseMap3Parameter;
        protected EffectParameter _diffuseMap4Parameter;

        protected Texture2D[] _textures = new Texture2D[4];
        protected Quaternion[] _levelData = new Quaternion[4];

        EffectParameter _worldMatrixParameter;
        EffectParameter _lightPositionParameter;
        EffectParameter _lightDiffuseParameter;
        EffectParameter _lightAmbientParameter;
        EffectParameter _lightAttenuationParameter;
        EffectParameter _lightCountParameter;

        bool _isLightingEnabled = false;
        int _maxLights = _globalMaxLightCount;
        Vector3[] _positions;
        Vector3[] _colors;
        Vector3[] _ambients;
        Vector2[] _attenuations;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            TorqueEventManager.SilenceEvents<bool>(GFXDevice.Instance.DeviceCreated, OnDeviceCreated);
            TorqueEventManager.SilenceEvents<bool>(GFXDevice.Instance.DeviceReset, OnDeviceCreated);
            _textures[0] = null;
            _textures[1] = null;
            _textures[2] = null;
            _textures[3] = null;
            _textures = null;
            _levelData = null;
            _worldMatrixParameter = null;
            _lightPositionParameter = null;
            _lightDiffuseParameter = null;
            _lightAmbientParameter = null;
            _lightAttenuationParameter = null;
            _lightCountParameter = null;
            _worldViewProjectionParameter = null;
            _mapInfoParameter = null;
            _diffuseMap1Parameter = null;
            _diffuseMap2Parameter = null;
            _diffuseMap3Parameter = null;
            _diffuseMap4Parameter = null;
            _baseClipMap = null;
            _clipMaps.Clear();
            _clipMaps = null;
            base.Dispose();
        }

        #endregion
    }
}
