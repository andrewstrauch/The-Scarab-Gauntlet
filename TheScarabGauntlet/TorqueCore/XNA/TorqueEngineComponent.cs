//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GUI;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Platform;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;



namespace GarageGames.Torque.XNA
{
    /// <summary>
    /// This class is the main link between the XNA framework and the Torque X Engine.  All Torque X games require
    /// a TorqueEngineComponent.  The engine component manages low level details such as rendering and time management
    /// as well as initializing the core subsystems of the Torque engine.  Game code can access the engine component at any point with the Instance
    /// static property.  The engine component is a "Component" in the XNA sense, not in the Torque X sense; that is,
    /// it is a component of the Game object rather than a component of a TorqueObject within a game.
    /// </summary>
    public class TorqueEngineComponent : DrawableGameComponent
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// The engine component instance. This is always valid during the lifetime of a Torque Game.
        /// </summary>
        public static TorqueEngineComponent Instance
        {
            get { return _instance; }
        }



        static TorqueEngineComponent _instance;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructs the engine component. Initializes the engine component with default settings, as defined
        /// in TorqueEngineSettings. Settings can be overridden be calling LoadSettings on the component. This 
        /// constructor should be called either from the TorqueGame class or from the main assembly of your game.
        /// Note that only one TorqueEngineComponent is allowed to exist at one time.
        /// </summary>
        /// <param name="game">The XNA game object.</param>
        /// <param name="manager">The XNA graphics manager.</param>
        public TorqueEngineComponent(Game game, GraphicsDeviceManager manager)
            : base(game)
        {
            Assert.Fatal(_instance == null, "TorqueEngineComponent - Only one TorqueEngineComponent is allowed!");
            _instance = this;

            // create default settings object
            _settings = new TorqueEngineSettings();

            _graphicsManager = manager;

            // Set the default executable assembly to the calling assembly. This should be the assembly containing the game's Main function.  
            // Note that if the engine component is created from TorqueGame, the calling assembly is actually Torque, which is wrong, but 
            // TorqueGame corrects for this. The code here is only needed for games that do not use TorqueGame.
            ExecutableAssembly = Assembly.GetCallingAssembly();
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Optional path used to override the asset data base folder.
        /// </summary>
        public string DataPathOverride = "";

        /// <summary>
        /// XNA GameTime from the last update.
        /// </summary>
        public GameTime GameTime
        {
            get { return _gameTime; }
        }



        /// <summary>
        /// Total elapsed time in milliseconds since the engine was initialized.
        /// </summary>
        public float RealTime
        {
            get { return _realTime; }
        }



        /// <summary>
        /// Total elapsed game time in milliseconds since the engine was initialized. This can differ from RealTime if
        /// GameTimeScale is not 1.0 or if frame rate averaging is employed.
        /// </summary>
        public float TorqueTime
        {
            get { return _torqueTime; }
        }



        /// <summary>
        /// GameTimeScale will change the rate that time passes for game entities and scheduled events
        /// on the GameTimeSchedule. Scheduled events on RealTimeSchedule will not be affected by GameTimeScale.
        /// Set GameTimeScale to 0 to pause the game and to 1.0 to resume.
        /// </summary>
        public float GameTimeScale
        {
            get { return _gameTimeScale; }
            set { _gameTimeScale = Math.Max(0.0f, value); }
        }



        /// <summary>
        /// Scheduler for scheduling future events. Events on this queue are affected by GameTimeScale.
        /// </summary>
        public ScheduledEventPool GameTimeSchedule
        {
            get { return _gameTimeSchedule; }
        }



        /// <summary>
        /// Scheduler for scheduling future events. Events on this queue are not affected by GameTimeScale.
        /// </summary>
        public ScheduledEventPool RealTimeSchedule
        {
            get { return _realTimeSchedule; }
        }



        /// <summary>
        /// The executable assembly. Generally, this is the assembly that defines the Main component. Typically
        /// this is set up for you so you don't need to set it.
        /// </summary>
        public Assembly ExecutableAssembly
        {
            get { return _executableAssembly; }
            set { _executableAssembly = value; _executableFileInfo = new FileInfo(_executableAssembly.ManifestModule.FullyQualifiedName); }
        }



        /// <summary>
        /// List of Registered Assemblies known to the engine. The main reason to register an assembly is so that 
        /// the TorqueXMLDeserializer can load types from it. All known frameworks are added to this list by default;
        /// you only need to add a new assembly if you have defined your own class library, separate from your Main 
        /// executable, that defines types that you want deserialized.
        /// </summary>
        public List<Assembly> RegisteredAssemblies
        {
            get { return _registeredAssemblies; }
        }



        /// <summary>
        /// The directory containing the main executable.
        /// </summary>
        public string ExecutableDirectory
        {
            get { return _executableFileInfo.DirectoryName; }
        }



        /// <summary>
        /// The XNA game object.  
        /// </summary>
        public new Game Game
        {
            get { return base.Game; }
        }



        /// <summary>
        /// The name of the engine settings file to use. Can also contain path information. See 
        /// TorqueEngineSettings for more information about settings.
        /// </summary>
        public string SettingsFile
        {
            get { return _settingsFile; }
            set { _settingsFile = value; }
        }



        /// <summary>
        /// A TorqueEvent which is executed each time through the main loop. This is used by low level devices.
        /// It should not be used for game logic since it is not affected by GameTimeScale.
        /// </summary>
        public TorqueEvent<float> PumpEvent
        {
            get { return _pumpEvent; }
        }



        /// <summary>
        /// Returns true if the XNA game is active and the game is not in an exiting state.
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (Game == null)
                    return false;
                else
#if XBOX
                    try 
                    {
                        // Maybe that the Gamer service has not been loaded
                        return Game.IsActive && !Guide.IsVisible && !_exiting;
                    }
                    catch
                    {
                        return Game.IsActive && !_exiting;
                    }
#else
                    return Game.IsActive && !_exiting;
#endif
            }
        }



        /// <summary>
        /// Returns the settings used by this component.  
        /// </summary>
        public TorqueEngineSettings Settings
        {
            get { return _settings; }
        }



        /// <summary>
        /// The post processor that is used for the entire canvas. This includes all GUI elements
        /// as well as the game scene itself. To perform post processing on just a game window,
        /// set the PostProcessor property on GuiSceneview.
        /// </summary>
        public PostProcessor PostProcessor
        {
            get { return _postProcessor; }
            set { _postProcessor = value; }
        }



        /// <summary>
        /// Whether back buffer effects are enabled on this component. See TorqueEngineSettings for more information.
        /// </summary>
        public bool EnableBackBufferEffects
        {
            get { return _settings.EnableBackBufferEffects; }
        }



        /// <summary>
        /// The render target that is currently being rendered to. Setting this DOES NOT
        /// actually set the render target. It is just for the engine to use when
        /// _ReapplyMainRenderTarget is called.
        /// </summary>
        public RenderTarget2D CurrentRenderTarget
        {
            get { return _currentRenderTarget; }
            set { _currentRenderTarget = value; }
        }



        /// <summary>
        /// The XNA AudioEngine object.
        /// </summary>
        public AudioEngine SFXDevice
        {
            get { return _sfxDevice; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Register an Assembly with the engine. See RegisteredAssemblies property for more info. It is 
        /// safe to register an assembly multiple times.
        /// </summary>
        /// <param name="a">The assembly to register</param>
        public void RegisterAssembly(Assembly a)
        {
            if (_registeredAssemblies.Contains(a))
                TorqueConsole.Warn("TorqueEngineComponent::RegisterAssembly - Assembly {0} is already registered.", a.FullName);
            else
                _registeredAssemblies.Add(a);
        }



        /// <summary>
        /// Load the engine settings from the specified file. If the file does not exist, default settings are used.
        /// If the settings file contains graphics settings, they are applied only if the graphics device has 
        /// not been created yet.
        /// </summary>
        /// <param name="settingsFile">The filename of the xml settings file.</param>
        public void LoadSettings(string settingsFile)
        {
            _settingsFile = settingsFile;

            // load the settings file 
            _settings = TorqueEngineSettings.Load(_settingsFile);
            if (_settings == null)
                _settings = new TorqueEngineSettings(); // use default settings

            // if graphics device is not yet initialized, we can apply graphics settings
            if (_graphicsManager.GraphicsDevice == null && _settings.CurrentGraphicsManagerSettings != null)
            {
                _graphicsManager.IsFullScreen = _settings.CurrentGraphicsManagerSettings.IsFullScreen;
                _graphicsManager.MinimumPixelShaderProfile = _settings.CurrentGraphicsManagerSettings.MinimumPixelShaderProfile;
                _graphicsManager.MinimumVertexShaderProfile = _settings.CurrentGraphicsManagerSettings.MinimumVertexShaderProfile;
                _graphicsManager.PreferMultiSampling = _settings.CurrentGraphicsManagerSettings.PreferMultiSampling;
                _graphicsManager.PreferredBackBufferFormat = _settings.CurrentGraphicsManagerSettings.PreferredBackBufferFormat;
                _graphicsManager.PreferredBackBufferHeight = _settings.CurrentGraphicsManagerSettings.PreferredBackBufferHeight;
                _graphicsManager.PreferredBackBufferWidth = _settings.CurrentGraphicsManagerSettings.PreferredBackBufferWidth;
                _graphicsManager.PreferredDepthStencilFormat = _settings.CurrentGraphicsManagerSettings.PreferredDepthStencilFormat;
                _graphicsManager.SynchronizeWithVerticalRetrace = _settings.CurrentGraphicsManagerSettings.SynchronizeWithVerticalRetrace;

                _graphicsManager.PreparingDeviceSettings += this._PreparingDeviceSettings;
            }

            // check for command line arguments.  because command line args are not supported on xbox, this is only
            // used for certain PC-only features (such as schema export).

#if !XBOX
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].ToLowerInvariant().Trim() == "-exportschema")
                {
                    // schema export requires use of TorqueGame
                    Torque.GameUtil.TorqueGame.ExportSchema = true;

                    // if next arg is present and not -, it is the filename to export to
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        Torque.GameUtil.TorqueGame.ExportSchemaFileName = args[i + 1];
                }
            }
#endif
        }



        /// <summary>
        /// Initialize the engine component. This is called by XNA. It loads settings (if they haven't been 
        /// initialized yet) and calls _SetupEngine to initialize the engine.
        /// </summary>
        public override void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            TorqueConsole.Echo("\nInitializing Torque X engine component...");

            base.Initialize();

            if (_settings == null)
            {
                TorqueConsole.Echo("Attempting to load settings file...");
                LoadSettings(_settingsFile);
            }

            // set up the engine
            _SetupEngine();

            TorqueConsole.Echo("\nTorque X engine component initialized.");
        }



        /// <summary>
        /// Tell the game object to exit.
        /// </summary>
        public void Exit()
        {
            _exiting = true;
            Game.Exit();
        }



        /// <summary>
        /// Shutdown the audio engine. Generally you do not need to call this directly, since TorqueGame does it when
        /// necessary. It is safe to call it multiple times or when audio hasn't been initialized. The purposes of this function is to 
        /// prevent XACT-related audio crashes when the game exits. The engine's SFXDevice will be null after calling this.
        /// </summary>
        public void ShutdownAudio()
        {
            if (_sfxDevice != null)
            {
                if (!_sfxDevice.IsDisposed)
                    _sfxDevice.Dispose();

                _sfxDevice = null;
            }
        }



        /// <summary>
        /// Updates the engine component. Called by XNA. This updates game and real time, processes torque events,
        /// and updates the audio engine.
        /// </summary>
        /// <param name="gameTime">The time that has passed since the last call to Update.</param>
        public override void Update(GameTime gameTime)
        {
            // do nothing if we not active
            if (!IsActive)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("TorqueEngineComponent.Update");
#endif

            // The time since Update was called last
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

#if DEBUG
            // update the event loop, depending on whether or not we are reading/writing a journal
            if (TorqueEventManager.Instance.IsReadingJournal)
            {
                _realTime = TorqueEventManager.Instance.Journal.Reader.ReadSingle();
                _torqueTime = TorqueEventManager.Instance.Journal.Reader.ReadSingle();

                while (TorqueEventManager.Instance.PostJournaledEvent())
                    ;

                // Process any non-journal events that were queued.
                while (TorqueEventManager.ProcessEvents())
                    ;
            }
            else
#endif
            {
                // update gameTime and realTime
                _gameTime = gameTime;
                _realTime += (float)_gameTime.ElapsedRealTime.TotalMilliseconds;

                // call GetElapsedTime to do any processing on the elapsed time (such as averaging)
                elapsedMs = _GetElapsedTime(elapsedMs);

                // update torque time
                _torqueTime += _gameTimeScale * elapsedMs;

#if DEBUG
                if (TorqueEventManager.Instance.IsWritingJournal)
                {
                    TorqueEventManager.Instance.Journal.Writer.Write(_realTime);
                    TorqueEventManager.Instance.Journal.Writer.Write(_torqueTime);
                }
#endif

                // Trigger a pump event per cycle.  Trigger processes the
                // event immediately without putting it on the event queue.
                TorqueEventManager.TriggerEvent(_pumpEvent, elapsedMs);

                // post a time event per cycle and process all events
                TorqueEventManager.PostEvent(_timeEvent, elapsedMs);

                while (TorqueEventManager.ProcessEvents())
                    ;

#if DEBUG
                // mark the end of this block of events
                if (TorqueEventManager.Instance.IsWritingJournal)
                    TorqueEventManager.Instance.MarkJournalEventBlockEnd();
#endif
            }

            // update the audio engine
            if (_sfxDevice != null)
                _sfxDevice.Update();

#if DEBUG
            Profiler.Instance.EndBlock("TorqueEngineComponent.Update");
#endif
        }



        /// <summary>
        /// Prepares render targets and clears them if that function is enabled in the torque settings. This
        /// should be called before drawing begins. Typically this is called from an override of the Game's
        /// Draw function. TorqueGame does this automatically.  
        /// </summary>
        public void OnStartDraw()
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueEngineComponent.OnStartDraw");
#endif

            if (EnableBackBufferEffects && _postProcessor == null)
            {
                // Default action when postprocessor is null is to create a copy postprocessor
                // this is done to allow refraction, which needs to render to a texture, to work 
                // out-of-box. Set EnableBackBufferEffects to false to disable this.
                _postProcessor = new PostProcessor();
                _postProcessor.Material = new CopyPostProcessMaterial();
            }

            if ((_mainRenderTarget.IsNull || _mainRenderTarget.IsInvalid) && EnableBackBufferEffects && _postProcessor != null)
            {
                DepthStencilBufferWidth = _gfxDevice.Device.DepthStencilBuffer.Width;
                DepthStencilBufferHeight = _gfxDevice.Device.DepthStencilBuffer.Height;
                _mainRenderTarget = ResourceManager.Instance.CreateRenderTarget2D(DepthStencilBufferWidth, DepthStencilBufferHeight, 1, SurfaceFormat.Color);
                _postProcessor.Setup(DepthStencilBufferWidth, DepthStencilBufferHeight);
            }

            ClearRenderTarget();

#if DEBUG
            Profiler.Instance.EndBlock("TorqueEngineComponent.OnStartDraw");
#endif
        }



        /// <summary>
        /// Renders the Torque Scene. Called by XNA.
        /// </summary>
        /// <param name="gameTime">The amount of time that has passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueEngineComponent.Draw");
#endif

            // set up post processing
            bool postProcess = !(_mainRenderTarget.IsNull || _mainRenderTarget.IsInvalid) && EnableBackBufferEffects && (_postProcessor != null);

            if (postProcess)
            {
                _currentRenderTarget = _mainRenderTarget.Instance;
                GFXDevice.Instance.Device.SetRenderTarget(0, _mainRenderTarget.Instance);

                ClearRenderTarget();
            }

            // render
            GUICanvas.Instance.RenderFrame();

            // clear post processing
            if (postProcess)
            {
                // resolve the texture and reset the back buffer
                GFXDevice.Instance.Device.SetRenderTarget(0, null);
                Texture2D texture = _mainRenderTarget.Instance.GetTexture();
                _currentRenderTarget = null;

                // run the post processor
                Vector2 size = new Vector2((float)GFXDevice.Instance.CurrentVideoMode.BackbufferWidth, (float)GFXDevice.Instance.CurrentVideoMode.BackbufferHeight);
                _postProcessor.Run(texture, Vector2.Zero, size);
            }

            // sanity check to avoid memory leaks.
            GFXDevice.Instance.FlushAndLockVolatileBuffers();
            GFXDevice.Instance.UnlockVolatileBuffers();

#if DEBUG
            Profiler.Instance.EndBlock("TorqueEngineComponent.Draw");
#endif
        }



        /// <summary>
        /// Performs draw post-processing. Should be called at the end of the game's EndDraw function. TorqueGame
        /// does this automatically.  If fence simulation is enabled (see TorqueEngineSettings), it is applied by
        /// this function.
        /// </summary>
        public void OnEndDraw()
        {
            // do nothing if we not active
            if (!IsActive)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("TorqueEngineComponent.OnEndDraw");
#endif

#if !XBOX
            if (_settings.SimulateFences)
            {
                GraphicsDevice d = GFXDevice.Instance.Device;

                if (_fenceTexture == null || _fenceTexture.IsDisposed)
                    _fenceTexture = new ResolveTexture2D(d, d.DepthStencilBuffer.Width, d.DepthStencilBuffer.Height, 1, SurfaceFormat.Bgr32);

                if (_fenceData == null)
                    _fenceData = new uint[d.DepthStencilBuffer.Width * d.DepthStencilBuffer.Height];

                d.ResolveBackBuffer(_fenceTexture);

                _fenceTexture.GetData<uint>(_fenceData);
            }
#endif

#if DEBUG
            Profiler.Instance.EndBlock("TorqueEngineComponent.OnEndDraw");
#endif
        }



        /// <summary>
        /// Clears the current render target and depth buffer.
        /// </summary>
        public void ClearRenderTarget()
        {
            ClearRenderTarget(false);
        }



        /// <summary>
        /// Clears the current render target if ClearBeforeRender is enabled in the engine settings and always
        /// clears the depth buffer.
        /// </summary>
        /// <param name="depthOnly">If this is true, only the depth buffer will be cleared.</param>
        public void ClearRenderTarget(bool depthOnly)
        {
            if (_settings.GraphicsClearSettings.ClearBeforeRender && !depthOnly)
                GFXDevice.Instance.Clear(_settings.GraphicsClearSettings.ClearColor, _settings.GraphicsClearSettings.ClearDepthValue, _settings.GraphicsClearSettings.ClearStencilValue);
            else
                GFXDevice.Instance.ClearDepth(_settings.GraphicsClearSettings.ClearDepthValue, _settings.GraphicsClearSettings.ClearStencilValue);
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// This function applies video settings based on what kind of video card we are running on.
        /// </summary>
        void _ApplyVideoProfile()
        {
            // profile based on adapter description
            string adapterDescription = this.GraphicsDevice.CreationParameters.Adapter.Description;

            if (adapterDescription == null)
                return;

            adapterDescription = adapterDescription.Trim().ToLower();
            if (adapterDescription == string.Empty)
                return;

        }



        /// <summary>
        /// Sets up the engine.  
        /// </summary>
        void _SetupEngine()
        {
            // if we have a d3d device, init video now
            if (_graphicsManager.GraphicsDevice != null)
            {
                // create gfx device
                _gfxDevice = new GFXDevice();

                // override shader profile...
                _gfxDevice.ForceUserShaderProfile = _settings.ForceUserShaderProfile;
                _gfxDevice.UserShaderProfile = _settings.UserShaderProfile;

                // initialize the device 
                _gfxDevice.Init(_graphicsManager);

                _ApplyVideoProfile();
            }

            // create audio device
            if (_settings.EnableAudio && _sfxDevice == null && _settings.AudioGlobalSettingsFile != string.Empty)
            {
                TorqueConsole.Echo("Initializing audio...");

                try
                {
                    _sfxDevice = new AudioEngine(_settings.AudioGlobalSettingsFile);
                }
                catch (ArgumentException e) // catch this one so that we can continue running without needing update all of our xact files.
                {
                    _sfxDevice = null;
                    TorqueConsole.Error("TorqueEngineComponent::_SetupEngine - Failed to initialize XACT: " + e.ToString());
                }
            }

            // create gui canvas
            _canvas = new GUICanvas();
            TorqueObjectDatabase.Instance.Register(_canvas);

            TorqueConsole.Echo("Initializing GUICanvas...");

            // put a default gui on the canvas
            GUISceneview defaultContentControl = new GUISceneview();
            defaultContentControl.Name = "DefaultSceneView";
            defaultContentControl.Style = new GUIStyle();
            _canvas.SetContentControl(defaultContentControl);

            // configure time handling on game object.
            if (_settings.UseFixedTimeStep)
            {
                ProcessList.Instance.TickMS = _settings.TickMS; // use for a constant tick (with interpolation)

                if (_settings.UseInterpolation)
                {
                    // We want to interpolate between ticks, have xna give use updates whenever it has time
                    ProcessList.Instance.UseInterpolation = true;
                    Game.IsFixedTimeStep = false;
                }
                else
                {
                    // we only want to render on tick boundaries (no interpolation) so tell xna
                    // to only give us tick divisible updates.
                    ProcessList.Instance.UseInterpolation = false;
                    Game.IsFixedTimeStep = true;
                    Game.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, _settings.TickMS);
                }
            }
            else
            {
                // Variable ticks and no interpolation so tell xna to give us updates whenever it has time
                ProcessList.Instance.TickMS = 0; // use for a variable tick (no interpolation)
                ProcessList.Instance.UseInterpolation = false; // not needed, but for clarity
                Game.IsFixedTimeStep = false;
            }

            TorqueConsole.Echo("Initializing event manager...");

            TorqueEventManager.ListenEvents(_timeEvent, _UpdateSim);
            TorqueEventManager.ListenEvents(PumpEvent, InputManager.Instance.Pump);

            // set up inputs
            Torque.Platform.XGamePadDevice.EnumerateGamepads();
#if !XBOX
            Torque.Platform.XKeyboardDevice.EnumerateKeyboards();
            Torque.Platform.XMouseDevice.EnumerateMouses();
            Microsoft.Xna.Framework.Input.Mouse.WindowHandle = Game.Window.Handle;
#endif

            TorqueConsole.Echo("Initializing content manager...");
            // Create global content manager
            ContentManager cm = new ContentManager(this.Game.Services);
            ResourceManager.Instance.GlobalContentManager = cm;

            // make it the current CM
            ResourceManager.Instance.PushContentManager(cm);


#if DEBUG
            // setup journaling
            if (_settings.JournalMode != TorqueJournal.JournalMode.None)
            {
                TorqueConsole.Echo("Initializing journaling...");
                bool ok;
                string err;
                TorqueJournal journal = new TorqueJournal();

                if (Settings.JournalMode == TorqueJournal.JournalMode.Play)
                {
                    err = " for reading.";
                    ok = journal.OpenForRead(_settings.JournalFile);
                }
                else
                {
                    err = " for writing.";
                    ok = journal.OpenForWrite(_settings.JournalFile);
                }

                Assert.Fatal(ok, "Not able to open journal file " + _settings.JournalFile + err);

                if (ok)
                    TorqueEventManager.Instance.Journal = journal;
            }
#endif
        }



        /// <summary>
        /// If the main render target is not null, set it as render target zero. Otherwise set null as render
        /// target zero (typically this restores the backbuffer).
        /// </summary>
        public void ReapplyMainRenderTarget()
        {
            if (_currentRenderTarget != null)
                GFXDevice.Instance.Device.SetRenderTarget(0, _currentRenderTarget);
            else
                GFXDevice.Instance.Device.SetRenderTarget(0, null);
        }



        /// <summary>
        /// Process the input elapsed time and return a new elased time. If UseAverageFrameTime is enabled, it is
        /// applied here. Otherwise, this function doesn't do anything except cap the elapsed time to 100ms.
        /// </summary>
        /// <param name="elapsed">Elapsed time.</param>
        /// <returns>Averages frame time if UseAverageFrameTime is set in the engine.</returns>
        private float _GetElapsedTime(float elapsed)
        {
            // cap elapsed time to 100ms
            if (elapsed > 100.0f)
                elapsed = 100.0f;

            if (_settings.UseAverageFrameTime)
            {
                int maxSamples = 30;

                // Average update rate over last few frames to get a more consistent
                // update rate.  The reason for this is that most of the variance in
                // the update interval is consumed by and caused by D3D.Present.
                float avgTime = 0.0f;
                float numSamples = 0.0f;

                for (int i = 1; i < _timeList.Count; i++)
                {
                    _timeList[i - 1] = _timeList[i];
                    avgTime += _timeList[i];
                    numSamples += 1.0f;
                }

                if (_timeList.Count < maxSamples)
                    _timeList.Add(elapsed);
                else
                    _timeList[maxSamples - 1] = elapsed;

                if (numSamples > 0)
                    elapsed = avgTime / numSamples;
            }

            return elapsed;
        }



        /// <summary>
        /// Called by Update. Advances time, ticks the engine, interpolates, and updates animation.
        /// </summary>
        /// <param name="eventName">Torque event name</param>
        /// <param name="elapsed">Amount of elapsed time that has passed in milliseconds</param>
        private void _UpdateSim(String eventName, float elapsed)
        {
            _gameTimeSchedule.AdvanceTime(elapsed * _gameTimeScale);
            _realTimeSchedule.AdvanceTime(elapsed);
            TorqueObjectDatabase.Instance.DeleteMarkedObjects();

            ProcessList.Instance.AdvanceTick(elapsed * _gameTimeScale);
            TorqueObjectDatabase.Instance.DeleteMarkedObjects();

            ProcessList.Instance.InterpolateTick();
            ProcessList.Instance.UpdateAnimation(elapsed * _gameTimeScale);
            TorqueObjectDatabase.Instance.DeleteMarkedObjects();
        }



        /// <summary>
        /// Apply custom graphics settings to the device before it is created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            if (_settings.CurrentGraphicsManagerSettings.UseDisplaySizeForBackbuffer)
            {
                GraphicsDeviceInformation gdi = args.GraphicsDeviceInformation;
                gdi.PresentationParameters.BackBufferWidth = gdi.Adapter.CurrentDisplayMode.Width;
                gdi.PresentationParameters.BackBufferHeight = gdi.Adapter.CurrentDisplayMode.Height;
            }
        }

        #endregion


        #region Private, protected, internal fields

        string _settingsFile = String.Empty;
        TorqueEngineSettings _settings;

        List<float> _timeList = new List<float>();
        GameTime _gameTime = new GameTime();
        float _realTime;
        float _torqueTime;
        float _gameTimeScale = 1.0f;

        int DepthStencilBufferWidth;
        int DepthStencilBufferHeight;

        TorqueEvent<float> _timeEvent = new TorqueEvent<float>("TimeEvent");
        TorqueEvent<float> _pumpEvent = new TorqueEvent<float>("PumpEvent");

        ScheduledEventPool _gameTimeSchedule = new ScheduledEventPool();
        ScheduledEventPool _realTimeSchedule = new ScheduledEventPool();

        bool _initialized;
        bool _exiting;

        GraphicsDeviceManager _graphicsManager;
        AudioEngine _sfxDevice;
        GFXDevice _gfxDevice;
        GUICanvas _canvas;

        ResolveTexture2D _fenceTexture;
        uint[] _fenceData;

        Resource<RenderTarget2D> _mainRenderTarget;
        RenderTarget2D _currentRenderTarget;
        PostProcessor _postProcessor;

        Assembly _executableAssembly;
        FileInfo _executableFileInfo;
        List<Assembly> _registeredAssemblies = new List<Assembly>();

        #endregion
    }
}

