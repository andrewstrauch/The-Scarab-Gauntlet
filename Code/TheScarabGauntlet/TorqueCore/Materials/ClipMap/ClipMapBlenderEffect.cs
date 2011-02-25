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
using GarageGames.Torque.GFX;



namespace GarageGames.Torque.Materials.ClipMap
{
    public class ManagedTexture2D : Texture2D, IDisposable
    {
        // Summary:
        //     Creates an uninitialized Texture2D resource of the specified dimensions.
        //     To initialize a Texture2D from an existing file, see the static method Framework.Content.ContentManager.Load``1(System.String)
        //     or Texture2D.FromFile.
        //
        // Parameters:
        //   graphicsDevice:
        //     The GraphicsDevice used to display the texture.
        //
        //   width:
        //     The width of the texture, in pixels. This value must be a power of two if
        //     the GraphicsDeviceCapabilities.TextureCaps.RequiresPower2 property of graphicsDevice
        //     is true. If this value is 0, a value of 1 is used.
        //
        //   height:
        //     The height of the texture, in pixels. This value must be a power of two if
        //     the GraphicsDeviceCapabilities.TextureCaps.RequiresPower2 property of graphicsDevice
        //     is true. If this value is 0, a value of 1 is used.
        //
        //   numberLevels:
        //     The number of downsampled surfaces to create when preprocessing the texture.
        //     These smaller versions of the texture, known as mipmap levels, are used when
        //     the texture is minified to fit a smaller area than the original texture size.
        //     The chain of downsampled surfaces associated with a texture is sometimes
        //     called a mipmap chain.If numberLevels is zero, all texture sublevels down
        //     to 1×1 pixels will be generated for hardware that supports mipmapped textures.
        //     Use Texture.LevelCount to see the number of levels generated.
        //
        //   usage:
        //     Options identifying the behaviors of this texture resource.
        //
        //   format:
        //     A SurfaceFormat value specifying the requested pixel format for the texture.
        //     The returned texture may be of a different format if the device does not
        //     support the requested format. Applications should check the format of the
        //     returned texture to ensure that it matches the requested format.
        public ManagedTexture2D(GraphicsDevice graphicsDevice, int width, int height, uint[] data)
            : base(graphicsDevice, width, height, 1, TextureUsage.None, SurfaceFormat.Color)
        {
            SetData<uint>(data);
            ManagedData = data;
        }



        /// <summary>Gets data managed by this instance.  Modifications to this data do not affect the base texture.</summary>
        public readonly uint[] ManagedData;



        /// <summary>
        /// Creates a new ManagedTexture2D from the given Texture2D.
        /// </summary>
        public static ManagedTexture2D FromTexture2D(Texture2D instance)
        {
            int w = instance.Width;
            int h = instance.Height;
            uint[] data = new uint[w * h];
            instance.GetData<uint>(data);
            ManagedTexture2D result = new ManagedTexture2D(instance.GraphicsDevice, w, h, data);
            return result;
        }

        #region IDisposable Members

        // Added just as helper, not really needed
        public virtual new void Dispose()
        {
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// An image cache that uses an opacity map to blend between up to four base textures with lightmap support.
    /// </summary>
    public class ClipMapBlenderEffect : RenderMaterial, IDisposable
    {

        #region Constructors

        public ClipMapBlenderEffect()
        {
            // this is a resource effect, so set the filename to the resource name
            EffectFilename = "ClipMapBlender";
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The opacity map to use when blending base textures. The RGBA channels of the 
        /// opacity map correspond directly to base textures 0-4. A color value of (0, 0, 0, 1)
        /// will result in BaseTextures[3] being fully visible and 0-2 being invisible. A color
        /// value of (0.5, 0.5, 0, 0) will result in a 50/50 fade between base textures 0 and 1.
        /// Each color in the opacity map should be be normalized for correct results.
        /// </summary>
        public Resource<Texture> OpacityMap
        {
            get { return _opacityMap; }
            set
            {
                _opacityMap = value;
                _ExtractOpacityMapData();
            }
        }



        /// <summary>
        /// The light map to use when rendering to the clip map. This will be blended over
        /// the base textures once they're blended.
        /// </summary>
        public Resource<Texture> LightMap
        {
            get { return _lightMap; }
            set
            {
                _lightMap = value;
                _ExtractLightMapData();
            }
        }



        /// <summary>
        /// The array of base textures to blend between when rendering
        /// to the clip map. Torque X currently supports a max of 4
        /// base textures.
        /// </summary>
        public Resource<Texture>[] BaseTextures
        {
            get { return _baseTextures; }
            set { _baseTextures = value; }
        }



        /// <summary>
        /// The texture coordinate offset at which the opacity map will be mapped to the
        /// geometry. This defaults to zero and in most cases should not be changed. This
        /// can be used to shift essentially the entire clip map texture across the face
        /// of a terrain to correct any potential alignment issues.
        /// </summary>
        public Vector2 OpacityMapOffset
        {
            get { return _opacityMapOffset; }
            set { _opacityMapOffset = value; }
        }



        /// <summary>
        /// The number of times the base textures will repeat across the geometry.
        /// </summary>
        public float TextureScale
        {
            get { return _textureScale; }
            set { _textureScale = value; }
        }



        /// <summary>
        /// A base scale factor that will be multiplied with TextureScale for the actual repeat count.
        /// This should be the clip map's size (512 by defailt) divided by TextureScale. It's left public
        /// to allow for customization.
        /// </summary>
        public float BaseTexScaleFactor
        {
            get { return _baseTexScaleFactor; }
            set { _baseTexScaleFactor = value; }
        }

        #endregion


        #region Public methods

        public override bool SetupPass()
        {
            if (!base.SetupPass())
                return false;

            // if we're running on a shader model 1 card we're going to be using a multi-pass
            // technique and blending the colors, so set the blend based on which pass we're on.
            // note that pass is already incremented when it gets here!
            if (_isShaderModel1)
            {
                if (_nextPass == 1)
                {
                    // first pass: don't blend
                    GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = false;
                }
                else if (_nextPass == 2)
                {
                    // second pass: additive blend
                    GFXDevice.Instance.Device.RenderState.AlphaBlendEnable = true;
                    GFXDevice.Instance.Device.RenderState.SourceBlend = Blend.SourceAlpha;
                    GFXDevice.Instance.Device.RenderState.DestinationBlend = Blend.One;
                }
            }

            return true;
        }



        public void ReloadOpacityMap()
        {
            if (_opacityMapData == null)
                return;

            //jk 9-14            ManagedTexture2D newOpacityTex = new ManagedTexture2D(GFXDevice.Instance.Device, _opacityMapWidth, _opacityMapWidth, _opacityMapData);
            Texture2D newOpacityTex = new Texture2D(GFXDevice.Instance.Device, _opacityMapWidth, _opacityMapWidth);
            newOpacityTex.SetData<uint>(_opacityMapData);
            //jk

            _opacityMap = ResourceManager.Instance.CreateResource<Texture>(newOpacityTex);
        }



        public void ReloadLightMap()
        {
            if (_lightMapData == null)
                return;

            //jk 9-14            ManagedTexture2D newLightTex = new ManagedTexture2D(GFXDevice.Instance.Device, _lightMapWidth, _lightMapWidth, _lightMapData);
            Texture2D newLightTex = new Texture2D(GFXDevice.Instance.Device, _lightMapWidth, _lightMapWidth);
            newLightTex.SetData<uint>(_lightMapData);
            _lightMap = ResourceManager.Instance.CreateResource<Texture>(newLightTex);
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            // technique switching
            if (GFXDevice.Instance.ShaderProfile < ShaderProfile.PS_2_0)
            {
                // shader profile is 1.1 to 1.4
                // set this flag true for SetupPass so we know to toggle blending for the second pass
                _isShaderModel1 = true;

                // use a 2 pass shader model 1.1 technique
                if (_lightMap.IsNull)
                    return "BlendWithoutLightMap_1_1";
                else
                    return "BlendWithLightMap_1_1";
            }
            else
            {
                // shader profile is 2.0 or higher
                // set this flag false for SetupPass so we know not to toggle blending (this version only needs one pass)
                _isShaderModel1 = false;

                // use a 1 pass shader model 2.0 technique
                if (_lightMap.IsNull)
                    return "BlendWithoutLightMap_2_0";
                else
                    return "BlendWithLightMap_2_0";
            }
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            EffectManager.SetParameter(_worldViewProjectionParameter, srs.World.Top * srs.View * srs.Projection);

            EffectManager.SetParameter(_textureScaleParameter, _textureScale * _baseTexScaleFactor);
            EffectManager.SetParameter(_opacityMapOffsetParameter, _opacityMapOffset);

            if (!_opacityMap.IsNull && !_opacityMap.IsInvalid)
                EffectManager.SetParameter(_opacityMapParameter, _opacityMap.Instance);

            if (!_lightMap.IsNull && !_lightMap.IsInvalid)
                EffectManager.SetParameter(_lightMapParameter, _lightMap.Instance);

            if (!_baseTextures[0].IsNull)
                EffectManager.SetParameter(_baseTexture1Parameter, _baseTextures[0].Instance);
            if (!_baseTextures[1].IsNull)
                EffectManager.SetParameter(_baseTexture2Parameter, _baseTextures[1].Instance);
            if (!_baseTextures[2].IsNull)
                EffectManager.SetParameter(_baseTexture3Parameter, _baseTextures[2].Instance);
            if (!_baseTextures[3].IsNull)
                EffectManager.SetParameter(_baseTexture4Parameter, _baseTextures[3].Instance);
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _worldViewProjectionParameter = EffectManager.GetParameter(Effect, "worldViewProjection");

            _textureScaleParameter = EffectManager.GetParameter(Effect, "textureScale");
            _opacityMapOffsetParameter = EffectManager.GetParameter(Effect, "opacityMapOffset");

            _opacityMapParameter = EffectManager.GetParameter(Effect, "opacityMap");
            _lightMapParameter = EffectManager.GetParameter(Effect, "lightMap");

            _baseTexture1Parameter = EffectManager.GetParameter(Effect, "baseTex1");
            _baseTexture2Parameter = EffectManager.GetParameter(Effect, "baseTex2");
            _baseTexture3Parameter = EffectManager.GetParameter(Effect, "baseTex3");
            _baseTexture4Parameter = EffectManager.GetParameter(Effect, "baseTex4");
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            _worldViewProjectionParameter = null;
            _textureScaleParameter = null;
            _opacityMapParameter = null;
            _opacityMapOffsetParameter = null;
            _lightMapParameter = null;
            _baseTexture1Parameter = null;
            _baseTexture2Parameter = null;
            _baseTexture3Parameter = null;
            _baseTexture4Parameter = null;
        }



        protected void _ExtractOpacityMapData()
        {
            if (_opacityMap.IsNull)
                return;

            /* //jk 9-14
                        ManagedTexture2D om = _opacityMap.Instance as ManagedTexture2D;
                        if (om != null)
                        {
                            _opacityMapWidth = om.Width;
                            _opacityMapData = (uint[])om.ManagedData.Clone();
                        }
                        else
                        {
                            Texture2D opacityTex = _opacityMap.Instance as Texture2D;
                            _opacityMapWidth = opacityTex.Width;
                            _opacityMapData = new uint[opacityTex.Width * opacityTex.Height];
                            opacityTex.GetData<uint>(_opacityMapData);
                        }
            */
            Texture2D opacityTex = _opacityMap.Instance as Texture2D;
            _opacityMapWidth = opacityTex.Width;
            _opacityMapData = new uint[opacityTex.Width * opacityTex.Height];
            opacityTex.GetData<uint>(_opacityMapData);
            //jk
        }



        protected void _ExtractLightMapData()
        {
            if (_lightMap.IsNull)
                return;

            /* //jk 9-14
                        ManagedTexture2D lm = _lightMap.Instance as ManagedTexture2D;
                        if (lm != null)
                        {
                            _lightMapWidth = lm.Width;
                            _lightMapData = (uint[])lm.ManagedData.Clone();
                        }
                        else
                        {
                            Texture2D lightTex = _lightMap.Instance as Texture2D;
                            _lightMapWidth = lightTex.Width;
                            _lightMapData = new uint[lightTex.Width * lightTex.Height];
                            lightTex.GetData<uint>(_lightMapData);
                        }
             */
            Texture2D lightTex = _lightMap.Instance as Texture2D;
            _lightMapWidth = lightTex.Width;
            _lightMapData = new uint[lightTex.Width * lightTex.Height];
            lightTex.GetData<uint>(_lightMapData);
            //jk
        }

        #endregion


        #region Private, protected, internal fields

        private bool _isShaderModel1;

        protected float _textureScale;
        protected float _baseTexScaleFactor;
        protected Vector2 _opacityMapOffset = Vector2.Zero;
        protected Resource<Texture> _opacityMap;
        protected Resource<Texture> _lightMap;
        protected Resource<Texture>[] _baseTextures;

        protected int _opacityMapWidth;
        protected uint[] _opacityMapData;
        protected int _lightMapWidth;
        protected uint[] _lightMapData;

        protected EffectParameter _worldViewProjectionParameter;
        protected EffectParameter _textureScaleParameter;
        protected EffectParameter _opacityMapParameter;
        protected EffectParameter _opacityMapOffsetParameter;
        protected EffectParameter _lightMapParameter;
        protected EffectParameter _baseTexture1Parameter;
        protected EffectParameter _baseTexture2Parameter;
        protected EffectParameter _baseTexture3Parameter;
        protected EffectParameter _baseTexture4Parameter;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_opacityMap.IsNull)
            {
                //_opacityMap.Instance.Dispose();
                _opacityMap.Invalidate();
            }
            if (!_lightMap.IsNull)
            {
                //_lightMap.Instance.Dispose();
                _lightMap.Invalidate();
            }
            _baseTexture1Parameter = null;
            _baseTexture2Parameter = null;
            _baseTexture3Parameter = null;
            _baseTexture4Parameter = null;
            if (_baseTextures != null)
            {
                for (int i = 0; i < _baseTextures.GetLength(0); i++)
                {
                    if (!_baseTextures[i].IsNull)
                    {
                        //_baseTextures[i].Instance.Dispose();
                        _baseTextures[i].Invalidate();
                    }
                }
            }
            if (!_effect.IsNull)
            {
                //_effect.Instance.Dispose();
                _effect.Invalidate();
            }
            _lightMapData = null;
            _lightMapParameter = null;
            _opacityMapData = null;
            _opacityMapOffsetParameter = null;
            _opacityMapParameter = null;
            _ResetRefs();
            _textureScaleParameter = null;
            _worldViewProjectionParameter = null;
            base.Dispose();
        }

        #endregion
    }
}

