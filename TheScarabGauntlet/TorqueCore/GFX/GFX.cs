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
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GFX
{
    /// <summary>
    /// Class that contains information about the video mode.
    /// </summary>
    public class GFXVideoMode
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Width of the depth/stencil buffer.
        /// </summary>
        public int BackbufferWidth
        {
            get { return _frameWidth; }
            set { _frameWidth = value; }
        }



        /// <summary>
        /// Height of the depth/stencil buffer.
        /// </summary>
        public int BackbufferHeight
        {
            get { return _frameHeight; }
            set { _frameHeight = value; }
        }



        /// <summary>
        /// Width of the display.
        /// </summary>
        public int DisplayWidth
        {
            get { return _displayWidth; }
            set { _displayWidth = value; }
        }



        /// <summary>
        /// Height of the display.
        /// </summary>
        public int DisplayHeight
        {
            get { return _displayHeight; }
            set { _displayHeight = value; }
        }



        /// <summary>
        /// The PC and XBOX differ on what is the actual display. On the PC, a game
        /// can be windowed or fullscreen. This property takes this into account.
        /// </summary>
        public int VirtualWidth
        {
            get
            {
#if XBOX
                return _displayWidth;
#else
                return _frameWidth;
#endif
            }
        }



        /// <summary>
        /// The PC and XBOX differ on what is the actual display. On the PC, a game
        /// can be windowed or fullscreen. This property takes this into account.
        /// </summary>
        public int VirtualHeight
        {
            get
            {
#if XBOX
                return _displayHeight;
#else
                return _frameHeight;
#endif
            }
        }



        /// <summary>
        /// Whether this mode is fullscreen.
        /// </summary>
        public bool Fullscreen
        {
            get { return _fullscreen; }
            set { _fullscreen = value; }
        }

        #endregion


        #region Private, protected, internal fields

        int _frameWidth;
        int _frameHeight;
        int _displayHeight;
        int _displayWidth;
        bool _fullscreen;

        #endregion
    }



    /// <summary>
    /// GFXDevice is the Torque X graphics interface layer. Contains the reference to the XNA GraphicsDevice.
    /// </summary>
    public class GFXDevice
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Returns the global GFXDevice instance.  Currently there is only one GFXDevice active at any time.
        /// </summary>
        public static GFXDevice Instance
        {
            get { return _instance; }
        }



        /// <summary>
        /// Overrides the GFX shader profile (usually the device profile) with the user defined profile.
        /// </summary>
        public bool ForceUserShaderProfile
        {
            get { return _forceUserShaderProfile; }
            set { _forceUserShaderProfile = value; }
        }



        /// <summary>
        /// User defined shader profile.  Useful for testing against older shader profiles.
        /// </summary>
        public ShaderProfile UserShaderProfile
        {
            get { return _userShaderProfile; }
            set { _userShaderProfile = value; }
        }



        /// <summary>
        /// Returns the GFX shader profile used for selecting rendering features.  This value may be the device or user defined profile.
        /// </summary>
        public ShaderProfile ShaderProfile
        {
            get
            {
                if (_deviceShaderProfile == ShaderProfile.Unknown)
                {
                    GraphicsDeviceCapabilities caps = _device.GraphicsDeviceCapabilities;

                    // must get the *lowest* shader version, in case these are different.
                    // don't compare ShaderProfile directly - those are different.
                    if (caps.PixelShaderVersion <= caps.VertexShaderVersion)
                    {
                        // safe to use the pixel shader version...
                        _deviceShaderProfile = caps.MaxPixelShaderProfile;
                    }
                    else
                    {
                        // looks like we need the vertex shader version - but ShaderProfile is not correct (it's VS not PS)!
                        // so build our own.
                        String str = String.Concat("PS_", caps.VertexShaderVersion.Major, "_", caps.VertexShaderVersion.Minor);
                        // and convert to ShaderProfile.
                        _deviceShaderProfile = (ShaderProfile)Enum.Parse(typeof(ShaderProfile), str, false); ;
                    }
                }

                if (!_forceUserShaderProfile)
                    return _deviceShaderProfile;

                if (_userShaderProfile > _deviceShaderProfile)
                    return _deviceShaderProfile;

                return _userShaderProfile;
            }
        }



        /// <summary>
        /// Returns true if the specifed width and height defines a widescreen aspect ratio display, false otherwise.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool IsWideScreen(int width, int height)
        {
            if (height == 0)
                return false;

            float aspect = (float)width / (float)height;

            return IsWideScreen(aspect);
        }



        /// <summary>
        /// Returns true if the specified width to height aspect ratio is widescreen.
        /// </summary>
        /// <param name="widthToHeightAspectRatio"></param>
        /// <returns></returns>
        public static bool IsWideScreen(float widthToHeightAspectRatio)
        {
            return widthToHeightAspectRatio >= 1.34f; // 4:3 == 1.33 repeating
        }

        static GFXDevice _instance;

        static readonly Color DefaultClearColor = Color.Green;
        static readonly float DefaultDepthClear = 1.0f;
        static readonly int DefaultStencilClear = 0;

        #endregion


        #region Constructors

        /// <summary>
        /// Create the GFX device.  Also assigns the global GFX singleton (and asserts if that singleton has 
        /// already been assigned).
        /// </summary>
        public GFXDevice()
        {
            Assert.Fatal(GFXDevice._instance == null, "GFXDevice Constructor - Error: GFXDevice singleton already exists!");

            GFXDevice._instance = this;
            _volatileBufferStorage = new GFXVolatileVertexBufferSourceManager<GFXVertexFormat.PCTTBN>(1024);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The GraphicsDevice contained in this instance.
        /// </summary>
        public GraphicsDevice Device
        {
            get { return _device; }
        }



        /// <summary>
        /// The current video mode.
        /// </summary>
        public GFXVideoMode CurrentVideoMode
        {
            get { return _currentMode; }
        }



        /// <summary>
        /// Whether the current device has a z buffer.
        /// </summary>
        public bool HasZBuffer
        {
            get { return _hasZBuffer; }
        }



        /// <summary>
        /// Whether the current device has a stencil buffer.
        /// </summary>
        public bool HasStencilBuffer
        {
            get { return _hasStencilBuffer; }
        }



        /// <summary>
        /// TorqueEvent that is fired when device is disposing.  Bool parameter is ignored.
        /// </summary>
        public TorqueEvent<bool> DeviceDisposing
        {
            get { return _deviceDisposing; }
        }



        /// <summary>
        /// TorqueEvent that is fired when device is created.  Bool parameter is ignored.
        /// </summary>
        public TorqueEvent<bool> DeviceCreated
        {
            get { return _deviceCreated; }
        }



        /// <summary>
        /// TorqueEvent that is fired when device is resetting.  Bool parameter is ignored.
        /// </summary>
        public TorqueEvent<bool> DeviceResetting
        {
            get { return _deviceResetting; }
        }



        /// <summary>
        /// TorqueEvent that is fired when device is Reset.  Bool parameter is ignored.
        /// </summary>
        public TorqueEvent<bool> DeviceReset
        {
            get { return _deviceReset; }
        }

        #endregion


        #region Public Methods


        #region Initialization & Device Events

        /// <summary>
        /// Initialize the GFXDevice
        /// </summary>
        /// <param name="manager">a GraphicsDeviceManager</param>
        public void Init(GraphicsDeviceManager manager)
        {
            Assert.Fatal(manager != null, "GFXDevice.Init - Graphics component is null!");

            _graphicsManager = manager;

            manager.DeviceCreated += this.OnDeviceCreated;
            manager.DeviceResetting += this.OnDeviceResetting;
            manager.DeviceReset += this.OnDeviceReset;
            manager.DeviceDisposing += this.OnDeviceDisposing;

            // if device is already active, call OnDeviceCreated
            if (manager.GraphicsDevice != null)
                OnDeviceCreated(this, null);
        }



        /// <summary>
        /// Invoked by graphics manager when device is disposing.  Triggers related torque event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnDeviceDisposing(object sender, EventArgs args)
        {
            TorqueConsole.Echo("\nGraphics device disposing...");
            ResourceManager.Instance.OnDeviceResetting(true);
            TorqueEventManager.TriggerEvent(DeviceDisposing, true);
        }



        /// <summary>
        /// Invoked by graphics manager when device is resetting.  Triggers related torque event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnDeviceResetting(object sender, EventArgs args)
        {
            TorqueConsole.Echo("\nGraphics device resetting...");
            ResourceManager.Instance.OnDeviceResetting(false); // clear the resource manager before the we get the reset event
            TorqueEventManager.TriggerEvent(DeviceResetting, true);
        }



        /// <summary>
        /// Invoked by graphics manager when device is reset.  Sets up the device according to engine specifications.
        /// Triggers related torque event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnDeviceReset(object sender, EventArgs args)
        {
            TorqueConsole.Echo("\nGraphics device reset.");
            _SetupDevice();
            TorqueEventManager.TriggerEvent(DeviceReset, true);
        }



        /// <summary>
        /// Invoked by graphics manager when device is created.  Sets up the device according to engine specifications.
        /// Triggers related torque event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnDeviceCreated(object sender, EventArgs args)
        {
            TorqueConsole.Echo("\nGraphics device created.");

            _device = _graphicsManager.GraphicsDevice;
            _SetupDevice();

            TorqueEventManager.TriggerEvent(DeviceCreated, true);
        }



        /// <summary>
        /// Set up the graphics device.
        /// </summary>
        internal void _SetupDevice()
        {
            Assert.Fatal(_device == _graphicsManager.GraphicsDevice, "GFXDevice._SetupDevice - Did the device change and we fail to notice?");

            switch (_device.DepthStencilBuffer.Format)
            {
                case DepthFormat.Depth16:
                case DepthFormat.Depth24:
                case DepthFormat.Depth32:
                    _hasZBuffer = true;
                    break;
                case DepthFormat.Depth15Stencil1:
                case DepthFormat.Depth24Stencil4:
                case DepthFormat.Depth24Stencil8:
                case DepthFormat.Depth24Stencil8Single:
                    _hasZBuffer = true;
                    _hasStencilBuffer = true;
                    break;
            }

            _currentMode = new GFXVideoMode();
            _currentMode.Fullscreen = _graphicsManager.IsFullScreen;
            _currentMode.BackbufferWidth = _device.DepthStencilBuffer.Width;
            _currentMode.BackbufferHeight = _device.DepthStencilBuffer.Height;
            _currentMode.DisplayHeight = _device.DisplayMode.Height;
            _currentMode.DisplayWidth = _device.DisplayMode.Width;

            FontRenderer.Instance.Reset();
        }

        #endregion


        #region Utility Functions

        /// <summary>
        /// Clear the graphics device using the default clear settings.
        /// </summary>
        public void Clear()
        {
            // default color is opaque green
            Clear(DefaultClearColor, DefaultDepthClear, DefaultStencilClear);
        }



        /// <summary>
        /// Clear the graphics device using the specified clear settings.
        /// </summary>
        /// <param name="clearColor"></param>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        public void Clear(Color clearColor, float depth, int stencil)
        {
            ClearOptions flags = ClearOptions.Target;

            if (HasZBuffer)
                flags |= ClearOptions.DepthBuffer;

            if (HasStencilBuffer)
                flags |= ClearOptions.Stencil;

            Device.Clear(flags, clearColor, depth, stencil);
        }



        /// <summary>
        /// Clear the depth and stencil buffers, but not the render target.
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        public void ClearDepth(float depth, int stencil)
        {
            if (HasZBuffer)
            {
                ClearOptions flags = ClearOptions.DepthBuffer;

                if (HasStencilBuffer)
                    flags |= ClearOptions.Stencil;

                Device.Clear(flags, Color.White, depth, stencil);
            }
            else if (HasStencilBuffer)
            {
                Device.Clear(ClearOptions.Stencil, Color.White, depth, stencil);
            }
        }



        /// <summary>
        /// Create a matrix using the specified frustum parameters.  Rotate the matrix so that it uses the Torque 3D 
        /// convention (Z up, Y in, X right).  Note, this 3D convention may change before the initial release of the
        /// 3D version of this engine.  
        /// </summary>
        /// <param name="fov">FOV In radians</param>
        /// <param name="aspect">Aspect ratio</param>
        /// <param name="near">Distance of near plane</param>
        /// <param name="far">Distance of far plane</param>
        /// <returns>The new matrix</returns>
        public Matrix SetFrustum(float fov, float aspect, float near, float far)
        {
            // use the perspectivefovRH function to get a RH coordinate system
            //  positive x points right
            //  positive y points up
            //  positive z points back (i.e, not in to screen)      
            Matrix mat = Matrix.CreatePerspectiveFieldOfView(fov / aspect, aspect, near, far); // this is right handed

            // now rotate the coordinate system 90 degress about x so that we get the torque coordinate system
            //  positive x points right
            //  positive z points up
            //  positive y points in to screen
            Matrix rot = Matrix.CreateRotationX(-(float)Math.PI / 2);

            mat = Matrix.Multiply(rot, mat);

            return mat;
        }



        /// <summary>
        /// Create a matrix using the specified orthographic parameters.  
        /// </summary>
        /// <param name="useZForHeight">If true, the matrix will be oriented with Z up, like the Torque3D coordinate system</param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="top"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns>The new matrix</returns>
        public Matrix SetOrtho(bool useZForHeight, float left, float right, float bottom, float top, float near, float far)
        {
            Matrix mat = Matrix.CreateOrthographicOffCenter(left, right, bottom, top, near, far);

            if (useZForHeight)
            {
                Matrix rot = Matrix.CreateRotationX(-(float)Math.PI / 2);
                mat = Matrix.Multiply(rot, mat);
            }

            return mat;
        }



        /// <summary>
        /// Allocates space in the shared volatile pool for the passed in buffer.
        /// </summary>
        /// <param name="elementcount"></param>
        /// <param name="buffer"></param>
        public void ReserveVolatileBufferPCTTBN(int elementcount, GFXVolatileSharedVertexBufferPCTTBN buffer)
        {
            _volatileBufferStorage.ReserveBuffer(elementcount, buffer);
        }



        /// <summary>
        /// Flushes any cached volatile data to the hardware and locks the buffer pool for rendering.
        /// </summary>
        public void FlushAndLockVolatileBuffers()
        {
            _volatileBufferStorage.FlushAndLock();
        }



        /// <summary>
        /// Unlocks the buffer pool after rendering.
        /// </summary>
        public void UnlockVolatileBuffers()
        {
            _volatileBufferStorage.Unlock();
        }



        /// <summary>
        /// Checks to see if the buffer pool was flushed.
        /// </summary>
        /// <returns>True if the buffer contents are flushed.</returns>
        public bool AreVolatileBuffersFlushed()
        {
            return _volatileBufferStorage.AreBufferContentsFlushed();
        }

        /// <summary>
        /// Returns the ratio of source texel per screen pixel for the current viewport.
        /// </summary>
        /// <param name="distance">The distance to test.</param>
        /// <param name="radius">The radius of the area to test.</param>
        /// <returns>Returns the ratio of source texel per screen pixel for the current viewport.</returns>
        public float ProjectRadius(float distance, float radius)
        {
            return (radius / distance) * _device.Viewport.Width;
        }



        /// <summary>
        /// Get a vertex declaration for the VertexPositionColor vertex format.
        /// </summary>
        /// <returns>A vertex declaration for the VertexPositionColor vertex format.</returns>
        public VertexDeclaration GetVertexDeclarationVPC()
        {
            if (_vdVPC == null || _vdVPC.IsDisposed || _vdVPC.GraphicsDevice != _device)
                _vdVPC = new VertexDeclaration(_device, VertexPositionColor.VertexElements);

            return _vdVPC;
        }



        /// <summary>
        /// Get a vertex declaration for the VertexPositionTexture vertex format.
        /// </summary>
        /// <returns>A vertex declaration for the VertexPositionTexture vertex format.</returns>
        public VertexDeclaration GetVertexDeclarationVPT()
        {
            if (_vdVPT == null || _vdVPT.IsDisposed || _vdVPT.GraphicsDevice != _device)
                _vdVPT = new VertexDeclaration(_device, VertexPositionTexture.VertexElements);

            return _vdVPT;
        }



        /// <summary>
        /// Get a vertex declaration for the VertexPositionColorTexture vertex format.
        /// </summary>
        /// <returns>A vertex declaration for the VertexPositionColorTexture vertex format.</returns>
        public VertexDeclaration GetVertexDeclarationVPCT()
        {
            if (_vdVPCT == null || _vdVPCT.IsDisposed || _vdVPCT.GraphicsDevice != _device)
                _vdVPCT = new VertexDeclaration(_device, VertexPositionColorTexture.VertexElements);

            return _vdVPCT;
        }



        /// <summary>
        /// Get a vertex declaration for the VertexPositionNormalTexture vertex format.
        /// </summary>
        /// <returns>A vertex declaration for the VertexPositionNormalTexture vertex format.</returns>
        public VertexDeclaration GetVertexDeclarationVPNT()
        {
            if (_vdVPNT == null || _vdVPNT.IsDisposed || _vdVPNT.GraphicsDevice != _device)
                _vdVPNT = new VertexDeclaration(_device, VertexPositionNormalTexture.VertexElements);

            return _vdVPNT;
        }

        #endregion

        #endregion


        #region Private, protected, internal fields

        GraphicsDevice _device = null;
        GraphicsDeviceManager _graphicsManager = null;
        GFXVideoMode _currentMode;
        bool _hasZBuffer;
        bool _hasStencilBuffer;
        bool _forceUserShaderProfile = false;
        ShaderProfile _deviceShaderProfile = ShaderProfile.Unknown;
        ShaderProfile _userShaderProfile = ShaderProfile.PS_1_1;

        GFXVolatileVertexBufferSourceManager<GFXVertexFormat.PCTTBN> _volatileBufferStorage;

        TorqueEvent<bool> _deviceReset = new TorqueEvent<bool>("DeviceReset", false);
        TorqueEvent<bool> _deviceResetting = new TorqueEvent<bool>("DeviceResetting", false);
        TorqueEvent<bool> _deviceDisposing = new TorqueEvent<bool>("DeviceDisposing", false);
        TorqueEvent<bool> _deviceCreated = new TorqueEvent<bool>("DeviceCreated", false);

        // vertex declarations
        private VertexDeclaration _vdVPC;
        private VertexDeclaration _vdVPT;
        private VertexDeclaration _vdVPCT;
        private VertexDeclaration _vdVPNT;

        #endregion
    }
}

