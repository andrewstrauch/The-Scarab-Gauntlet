//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using GarageGames.Torque.GUI;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.Materials.ClipMap
{
    public class ClipMapBlenderImageCache : IClipMapImageCache, IDisposable
    {

        #region Static members

        public static readonly int MaxBlendedTextures = 4;
        private static int _nvidiaVendorID = 0x10DE;

        #endregion


        #region Public properties

        /// <summary>
        /// The filenames of the base textures to use.
        /// </summary>
        public string[] BaseTextureNames
        {
            get { return _baseTexNames; }
            set { _baseTexNames = value; }
        }



        /// <summary>
        /// The opacity map to use when blending textures.
        /// </summary>
        public Resource<Texture> OpacityMap
        {
            get { return _blender.OpacityMap; }
            set { _blender.OpacityMap = value; }
        }



        /// <summary>
        /// The light map to use.
        /// </summary>
        public Resource<Texture> LightMap
        {
            get { return _blender.LightMap; }
            set { _blender.LightMap = value; }
        }



        /// <summary>
        /// The texture coordinate offset at which the opacity map will be mapped to the
        /// geometry. This defaults to zero and in most cases should not be changed. This
        /// can be used to shift essentially the entire clip map texture across the face
        /// of a terrain to correct any potential alignment issues.
        /// </summary>
        public Vector2 OpacityMapOffset
        {
            get { return _blender.OpacityMapOffset; }
            set { _blender.OpacityMapOffset = value; }
        }



        /// <summary>
        /// The number of times the base textures will repeat across the geometry.
        /// </summary>
        public float TextureScale
        {
            get { return _blender.TextureScale; }
            set { _blender.TextureScale = value; }
        }



        /// <summary>
        /// A base scale factor that will be multiplied with TextureScale for the actual repeat count.
        /// This should be the clip map's size (512 by defailt) divided by TextureScale. It's left public
        /// to allow for customization.
        /// </summary>
        public float BaseTexScaleFactor
        {
            get { return _blender.BaseTexScaleFactor; }
            set { _blender.BaseTexScaleFactor = value; }
        }

        #endregion


        #region Public methods

        public void Initialize(int clipMapSize, int clipStackDepth)
        {
            // load all the base textures
            _blender.BaseTextures = new Resource<Texture>[_baseTexNames.Length];

            for (int i = 0; i < _blender.BaseTextures.Length; i++)
            {
                if (!string.IsNullOrEmpty(_baseTexNames[i]))
                {
                    if (!_useDebugTexture)
                    {
                        _blender.BaseTextures[i] = ResourceManager.Instance.LoadTexture(_baseTexNames[i]);
                    }
                    else
                    {
                        Resource<Texture> tex = ResourceManager.Instance.LoadTexture(_baseTexNames[i]);
                        _blender.BaseTextures[i] = _GenerateDebugTexture((tex.Instance as Texture2D).Width);
                    }
                }
            }

            // reload the opacity map if neccesary
            if (_blender.OpacityMap.IsNull || _blender.OpacityMap.IsInvalid)
                _blender.ReloadOpacityMap();

            // reload the light map if neccesary
            if (_blender.LightMap.IsNull || _blender.LightMap.IsInvalid)
                _blender.ReloadLightMap();

            // dispose any existing render targets
            for (int i = 0; i < _renderTargets.Count; i++)
                _renderTargets[i].Dispose();

            // clear the render targets list
            _renderTargets.Clear();

            // create the scratch render targets: one for each clip stack entry
            for (int i = 0; i < clipStackDepth; i++)
            {
                // check for NVIDIA:
                // XNA currently has issues generating mipmaps for certain NVIDIA drivers (apparently due 
                // to a stretchrect call failing and not reporting failure correctly or something).
                // to avoid problems, disable all blender image cache terrain mipping on all NVIDIA cards!
                // TODO: remove this check when Microsoft fixes the problem in XNA
#if XBOX
                _renderTargets.Add(new RenderTarget2D(GFXDevice.Instance.Device, clipMapSize, clipMapSize, 1, SurfaceFormat.Color, RenderTargetUsage.PreserveContents));
#else
                if (GFXDevice.Instance.Device.CreationParameters.Adapter.VendorId == _nvidiaVendorID)
                {
                    // if NVIDIA, use only 1 mip level
                    _renderTargets.Add(new RenderTarget2D(GFXDevice.Instance.Device, clipMapSize, clipMapSize, 1, SurfaceFormat.Color, RenderTargetUsage.PreserveContents));
                }
                else
                {
                    // not NVIDIA, use full mipmap
                    _renderTargets.Add(new RenderTarget2D(GFXDevice.Instance.Device, clipMapSize, clipMapSize, 0, SurfaceFormat.Color, RenderTargetUsage.PreserveContents));
                }
#endif
            }

            // create the quad to render when blending base textures
            _CreateRenderQuadVBIB();

            // create the viewport to use when blending
            _viewSlice.X = 0;
            _viewSlice.Y = 0;
            _viewSlice.Width = clipMapSize;
            _viewSlice.Height = clipMapSize;

            // create the render state with the proper world transform for rendering our quad
            if (_toTexRenderState == null)
            {
                _toTexRenderState = new SceneRenderState();
                _toTexRenderState.World.LoadIdentity();
                _toTexRenderState.View = Matrix.Identity;
                _toTexRenderState.Projection = GFXDevice.Instance.SetOrtho(false, 0f, 1f, 1f, 0f, 0f, 1f);
                _toTexRenderState.Gfx = GFXDevice.Instance;
            }
        }



        public void BeginRectUpdates(int mipLevel, ClipStackEntry stackEntry)
        {
            // grab a local reference to the graphics device
            GraphicsDevice device = GFXDevice.Instance.Device;

            // record the viewport (if PC)
            _sceneViewport = device.Viewport;
            _sceneDepthBuffer = device.DepthStencilBuffer;

            // replace it with our scratch render target for this mip level
            device.Viewport = _viewSlice;
            device.SetRenderTarget(0, _renderTargets[mipLevel]);
            device.DepthStencilBuffer = null;

            // set up the device to render our quad to a section of the texture
            device.RenderState.AlphaBlendEnable = false;
            device.RenderState.DepthBufferEnable = false;
            device.RenderState.CullMode = CullMode.None;
            //device.Indices = _renderQuadIB.Instance;
            device.VertexDeclaration = GFXDevice.Instance.GetVertexDeclarationVPT();
        }



        public void DoRectUpdate(int mipLevel, ClipStackEntry stackEntry, RectangleI srcRegion, RectangleI dstRegion)
        {
            // get a local reference to the GFXDevice's graphics device
            GraphicsDevice device = GFXDevice.Instance.Device;

            if (device.IsDisposed)
                return;

            // calculate the new destination texture coordinates
            // (custom viewports currently not fully supported on the XBox, so we use this method instead.
            // this is just as fast, so probably keep using this.)
            RectangleF dstCoords = new RectangleF(
                (float)dstRegion.X / (float)stackEntry.Texture.Width,
                (float)dstRegion.Y / (float)stackEntry.Texture.Height,
                (float)dstRegion.Width / (float)stackEntry.Texture.Width,
                (float)dstRegion.Height / (float)stackEntry.Texture.Height
                );

            // calculate the new texture coordinates
            RectangleF texCoords = new RectangleF(
                (float)srcRegion.X / ((float)stackEntry.Texture.Width * stackEntry.ScaleFactor),
                (float)srcRegion.Y / ((float)stackEntry.Texture.Height * stackEntry.ScaleFactor),
                (float)srcRegion.Width / ((float)stackEntry.Texture.Width * stackEntry.ScaleFactor),
                (float)srcRegion.Height / ((float)stackEntry.Texture.Height * stackEntry.ScaleFactor)
                );

            // custom viewports currently bugged on XBOX
            _UpdateRenderQuad(texCoords, dstCoords);

            // init the effect for this render
            _blender.SetupEffect(_toTexRenderState, null);

            // draw the quad
            while (_blender.SetupPass())
            {
                try
                {
                    device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, _renderQuadVB, 0, 2);
                }
                catch (Exception x)
                {
                    break;
                }
            }
            // cleanup the effect after render
            _blender.CleanupEffect();
        }



        public void FinishRectUpdates(int mipLevel, ClipStackEntry stackEntry)
        {
            // grab a local reference to the graphics device
            GraphicsDevice device = GFXDevice.Instance.Device;

            // resolve the render target and refresh the clip stack texture reference
            device.SetRenderTarget(0, null);

            // get the render target texture from the current render target
            stackEntry.Texture = _renderTargets[mipLevel].GetTexture();

            // drop the normal render target and viewport back into place
            TorqueEngineComponent.Instance.ReapplyMainRenderTarget();
            device.Viewport = _sceneViewport;
            device.DepthStencilBuffer = _sceneDepthBuffer;
        }



        public IClipMapImageCache GetCopyOfInstance()
        {
            ClipMapBlenderImageCache copy = new ClipMapBlenderImageCache();
            copy.BaseTextureNames = BaseTextureNames;
            copy.TextureScale = TextureScale;
            copy.OpacityMapOffset = OpacityMapOffset;
            copy.OpacityMap = OpacityMap;
            copy.LightMap = LightMap;
            copy.BaseTexScaleFactor = BaseTexScaleFactor;
            return copy;
        }

        #endregion


        #region Private, protected, internal methods

        private void _CreateRenderQuadVBIB()
        {
            // create a new vertex buffer
            D3DVertexBufferProfile profile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
            _renderQuadVB = new VertexPositionTexture[4];

            VertexPositionTexture[] v = TorqueUtil.GetScratchArray<VertexPositionTexture>(4);
            {
                Int32 idx = 0;
                for (Int32 ix = 0; ix < 2; ix++)
                    for (Int32 iy = 0; iy < 2; iy++)
                        v[idx++] = new VertexPositionTexture(new Vector3((float)ix, (float)iy, 0), new Vector2((float)ix, 1f - (float)iy));

                v.CopyTo(_renderQuadVB, 0);
            }
        }



        private void _UpdateRenderQuad(RectangleF texCoords, RectangleF verts)
        {
            VertexPositionTexture[] v = TorqueUtil.GetScratchArray<VertexPositionTexture>(4);
            Int32 idx = 0;

            for (Int32 ix = 0; ix < 2; ix++)
                for (Int32 iy = 0; iy < 2; iy++)
                    v[idx++] = new VertexPositionTexture(new Vector3(((float)ix * verts.Width) + verts.X, ((float)iy * verts.Height) + verts.Y, 0),
                                                        new Vector2(((float)ix * texCoords.Width) + texCoords.X, ((float)iy * texCoords.Height) + texCoords.Y));

            GFXDevice.Instance.Device.Vertices[0].SetSource(null, 0, 0);
            v.CopyTo(_renderQuadVB, 0);
        }



        protected Resource<Texture> _GenerateDebugTexture(int width)
        {
            uint[] data = new uint[width * width];

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint color = 0xFF000000;

                    int oneX = x & 1;
                    int oneY = y & 1;
                    int twoX = x & 4;
                    int twoY = y & 4;
                    int fourX = x & 16;
                    int fourY = y & 16;

                    // 1x1 red checker pattern
                    if ((oneX ^ oneY) != 0)
                        color += 0xFF;

                    // 2x2 green checker pattern
                    if ((twoX ^ twoY) != 0)
                        color += 0xFF00;

                    // 4x4 blue checker pattern
                    if ((fourX ^ fourY) != 0)
                        color += 0xFF0000;

                    data[(width * y) + x] = color;
                }
            }

            // create and return the texture
            Texture2D tex = new Texture2D(GFXDevice.Instance.Device, width, width, 1, TextureUsage.None, SurfaceFormat.Color);
            tex.SetData<uint>(data);
            return ResourceManager.Instance.CreateResource<Texture>(tex);
        }

        #endregion


        #region Private, protected, internal fields


        private ClipMapBlenderEffect _blender = new ClipMapBlenderEffect();

        protected string[] _baseTexNames;
        protected bool _useDebugTexture = false;

        private VertexPositionTexture[] _renderQuadVB;

        private Viewport _viewSlice;
        private Viewport _sceneViewport;
        private DepthStencilBuffer _sceneDepthBuffer;
        private SceneRenderState _toTexRenderState;

        private List<RenderTarget2D> _renderTargets = new List<RenderTarget2D>();

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_renderTargets != null)
            {
                foreach (RenderTarget2D x in _renderTargets)
                    x.Dispose();
                _renderTargets.Clear();
                _renderTargets = null;
            }
            if (!_blender.OpacityMap.IsNull)
                _blender.OpacityMap.Invalidate();
            _blender = null;
            _renderQuadVB = null;
            _sceneDepthBuffer = null;
            _toTexRenderState = null;
        }

        #endregion
    }
}
