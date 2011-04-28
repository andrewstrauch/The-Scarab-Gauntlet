//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;



namespace GarageGames.Torque.XNA
{
    /// <summary>
    /// Encapsulates graphics manager settings.
    /// </summary>
    public class GraphicsManagerSettings
    {
        /// <summary>
        /// Whether to start fullscreen, or not. Default false.
        /// </summary>
        public bool IsFullScreen = false;



        /// <summary>
        /// Minimum pixel shader profile. Default PS_1_1.
        /// </summary>
        public ShaderProfile MinimumPixelShaderProfile = ShaderProfile.PS_1_1;



        /// <summary>
        /// Mimimum vertex shader profile. Default VS_1_1.
        /// </summary>
        public ShaderProfile MinimumVertexShaderProfile = ShaderProfile.VS_1_1;



        /// <summary>
        /// Prefer multi sampling. Multi sampling smoothes the edges of lines onscreen, at a cost to performance.
        /// Default false.
        /// </summary>
        public bool PreferMultiSampling = false;



        /// <summary>
        /// Preferred back buffer format. Default BGR32.
        /// </summary>
        public SurfaceFormat PreferredBackBufferFormat = SurfaceFormat.Bgr32;



        /// <summary>
        /// Preferred back buffer width. Default 1024.
        /// </summary>
        public int PreferredBackBufferWidth = 1024;



        /// <summary>
        /// Preferred back buffer height. Default 768.
        /// </summary>
        public int PreferredBackBufferHeight = 768;



        /// <summary>
        /// Use the screen display size for the backbuffer. Overrides PreferredBackBufferWidth and
        /// PreferredBackBufferHeight. This is useful on the Xbox where the resolution is set in the
        /// dashboard. Default false.
        /// </summary>
        public bool UseDisplaySizeForBackbuffer = false;



        /// <summary>
        /// Preferred depth/stencil format. Default 24 bits of depth, 8 bit stencil.
        /// </summary>
        public DepthFormat PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;



        /// <summary>
        /// Whether to synchronize with vertical retrace of display hardware.
        /// </summary>
        public bool SynchronizeWithVerticalRetrace = false;
    }



    /// <summary>
    /// Encapsulates settings which control whether the engine component clears the graphics device and what
    /// values to use when doing so.
    /// </summary>
    public class GraphicsClearSettings
    {
        /// <summary>
        /// Whether to clear the graphics device before rendering. Default true.
        /// </summary>
        public bool ClearBeforeRender = true;



        /// <summary>
        /// Clear color. Default Indigo.
        /// </summary>
        public Color ClearColor = Color.Indigo;



        /// <summary>
        /// Clear depth value. Default 1.0f.
        /// </summary>
        public float ClearDepthValue = 1.0f;



        /// <summary>
        /// Clear stencil value. Default 0.
        /// </summary>
        public int ClearStencilValue = 0;
    }



    /// <summary>
    /// Settings for the engine.  This class is designed to be deserialized from an XML file. The public fields
    /// of this class are the loadable settings which can be done via a call to TorqueEngineComponent.Instance.LoadSettings(filename).
    /// </summary>
    public class TorqueEngineSettings
    {
        /// <summary>
        /// The current graphics manager settings, depending on which platform (xbox or windows) is running. This
        /// property is not deserialized from XML.
        /// </summary>
        [XmlIgnore]
        public GraphicsManagerSettings CurrentGraphicsManagerSettings
        {
            get
            {
#if XBOX
                return XboxGraphicsManagerSettings;
#else
                return WindowsGraphicsManagerSettings;
#endif
            }
        }



        /// <summary>
        /// Graphics settings for windows.  Initialized to a non-null set of defaults on construction. See
        /// GraphicsManagerSettings for information about these defaults.
        /// </summary>
        public GraphicsManagerSettings WindowsGraphicsManagerSettings = new GraphicsManagerSettings();



        /// <summary>
        /// Graphics settings for xbox. Initialized to a non-null set of defaults on construction. See
        /// GraphicsManagerSettings for information about these defaults.
        /// </summary>
        public GraphicsManagerSettings XboxGraphicsManagerSettings = new GraphicsManagerSettings();



        /// <summary>
        /// Clear settings. Initialized to a non-null set of defaults on construction. See
        /// GraphicsClearSettings for information about these defaults.
        /// </summary>
        public GraphicsClearSettings GraphicsClearSettings = new GraphicsClearSettings();



        /// <summary>
        /// How long a tick is in the engine, in milliseconds. Only used when UseFixedTimeStep is true. It is not
        /// advised that you change this unless you know what you are doing. Default 30.
        /// </summary>
        public int TickMS = 30;



        /// <summary>
        /// Whether to enable/disabled fixed time step on the XNA game object. Default true.
        /// </summary>
        public bool UseFixedTimeStep = true;



        /// <summary>
        /// Whether the engine internally calls interpolate or not between ticks. When false, this gives another
        /// level of fixed time step simulation for smoother rendering. Default true.
        /// </summary>
        public bool UseInterpolation = true;



        /// <summary>
        /// Whether the TorqueEngineComponent should average frame times. Default false.
        /// </summary>
        public bool UseAverageFrameTime = false;



        /// <summary>
        /// Whether the TorqueEngineComponent should initialize XACT audio. Default true.
        /// </summary>
        public bool EnableAudio = true;



        /// <summary>
        /// If true, fences will be automatically enabled if the engine detects that the system video card may benefit from them.
        /// If false, fences are only enabled if SimulateFences is set to true. Generally you should not enable this unless
        /// your game is known to need fences on some configurations. Default false.
        /// </summary>
        public bool AutoEnableFences = false;



        /// <summary>
        /// Whether D3D fences should be simulated. Fences can improve performance on lower end video cards that
        /// suffer when the game is rendering at a higher framerate than the card can handle. Default false.
        /// </summary>
        public bool SimulateFences = false;



        /// <summary>
        /// The settings file used to initialize XACT audio. Default empty string.
        /// </summary>
        public string AudioGlobalSettingsFile = string.Empty;



        /// <summary>
        /// This setting enables custom shader effects using multiple render targets.
        /// </summary>
        public bool EnableBackBufferEffects = true;



        /// <summary>
        /// Whether to enable anti-aliasing when rendering. Anti-aliasing smoothes out 3D shapes, but may interfere
        /// with shader effects and significantly slow down the framerate.
        /// </summary>
        public bool EnableAntiAliasing = false;



        /// <summary>
        /// This forces the engine to use the shader profile specified in UserShaderProfile instead of the maximum the
        /// video card can handle. Default false.
        /// </summary>
        public bool ForceUserShaderProfile = false;



        /// <summary>
        /// If ForceUserShaderProfile is true, the shader profile specified here will be used as the maximum shader
        /// version (unless it is higher than what the card can handle). Default PS_1_1.
        /// </summary>
        public ShaderProfile UserShaderProfile = ShaderProfile.PS_1_1;



        /// <summary>
        /// Name of the file to read/write journal to/from if journaling is enabled. Default empty string.
        /// </summary>
        public String JournalFile = string.Empty;



        /// <summary>
        /// Journaling mode (Play, Record, None). Default none.
        /// </summary>
        public TorqueJournal.JournalMode JournalMode = TorqueJournal.JournalMode.None;



        /// <summary>
        /// Load the engine settings from the specified file. Returns the new TorqueEngineSettings instance. If
        /// the file cannot be loaded, null is returned. The settings in the file will override the defaults
        /// that are hardcoded in the class definition.
        /// </summary>
        /// <param name="filename">The filename to load settings from.</param>
        /// <returns>The new settings object.</returns>
        public static TorqueEngineSettings Load(string filename)
        {
            if (filename == null || filename == string.Empty)
            {
                TorqueConsole.Warn("TorqueEngineSettings.Load - Settings file not specified.");
                return null;
            }

            FileInfo f = new FileInfo(filename);
            if (!f.Exists)
            {
                TorqueConsole.Warn("TorqueEngineSettings.Load - Settings file {0} not found.", filename);
                return null;
            }

            TorqueXmlDeserializer d = new TorqueXmlDeserializer();
            TorqueEngineSettings settings = new TorqueEngineSettings();
            d.Process(filename, settings);

            return settings;
        }

    }

}