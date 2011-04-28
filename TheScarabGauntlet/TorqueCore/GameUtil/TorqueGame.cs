//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Materials;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.GameUtil
{
    /// <summary>
    /// Base class for Torque X Games.  Subclasses the XNA Framework Game class.  It is not strictly required to use this, but it is recommended,
    /// since it does a lot of engine setup and other work for you.  This class is designed to be subclassed in your game.
    /// </summary>
    public class TorqueGame : Microsoft.Xna.Framework.Game
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// If true, the game will export the torque schema for any TorqueXmlSchemaType attributed classes in all
        /// registered assemblies when BeginRun is called.  After that it will exit.  Schema is exported to 
        /// file named by ExportSchemaFileName property.  Default is myschema.txschema.  
        /// </summary>
        public static bool ExportSchema
        {
            get { return _exportSchema; }
            set { _exportSchema = value; }
        }



        /// <summary>
        /// The filename to use when creating the exported schema file.
        /// </summary>
        public static string ExportSchemaFileName
        {
            get { return _exportSchemaFileName; }
            set { _exportSchemaFileName = value; }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Constructs the Game.  Creates the graphics manager and engine component.  Then calls SetupEngineComponent, SetupGraphics, and SetupGame,
        /// in that order.  This constructor should be called from your game's Main function or from a function in the same Assembly as Main.
        /// </summary>
        public TorqueGame()
        {
            // locate the calling assembly.  The torque engine component will need it.
            Assembly executingAssembly = Assembly.GetCallingAssembly();

            // make sure it is not Torque
            bool isTorque = executingAssembly == typeof(TorqueGame).Assembly;
            Assert.Fatal(!isTorque, "TorqueGame Constructor - Found Torque as calling assembly, this is not expected.  Construct the TorqueGame class from your game's Main function or from a function in the same Assembly as Main");

            // create graphics and engine component
            _graphicsManager = new Microsoft.Xna.Framework.GraphicsDeviceManager(this);

            this._engineComponent = new GarageGames.Torque.XNA.TorqueEngineComponent(this, this._graphicsManager);
            this._engineComponent.ExecutableAssembly = executingAssembly;

            // add engine component
            this.Components.Add(this._engineComponent);

            // setup engine component
            SetupEngineComponent();

            if (TorqueEngineComponent.Instance.Settings.EnableAntiAliasing)
            {
                _graphicsManager.PreferMultiSampling = true;
                _graphicsManager.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(OnPrepDeviceSettings);
            }

            // setup graphics - do this after engine component so that code can override engine component settings
            SetupGraphics();

            // setup game
            SetupGame();

            // We need to allow the schema export to happen before we start running the game
            if (ExportSchema)
                LoadEngineFrameworks();
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Set/Get a SceneLoader that the game can used to load scene XML files.  Get accessor creates the SceneLoader if none exists already (i.e.
        /// it won't return null)
        /// </summary>
        public SceneLoader SceneLoader
        {
            get
            {
                if (_sceneLoader == null)
                    _sceneLoader = new SceneLoader();

                return _sceneLoader;
            }
            set { _sceneLoader = value; }
        }



        /// <summary>
        /// Gets the TorqueEngineComponent associated with this game.
        /// </summary>
        public TorqueEngineComponent Engine
        {
            get { return _engineComponent; }
        }



        /// <summary>
        /// Gets the GraphicsDeviceManager associated with this game.
        /// </summary>
        public GraphicsDeviceManager GraphicsDeviceManager
        {
            get { return _graphicsManager; }
        }

        #endregion


        #region Private, protected, internal methods

        void OnPrepDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            // Xbox 360 and most PCs support FourSamples(4x) and TwoSamples(2x) antialiasing.
            PresentationParameters pp = e.GraphicsDeviceInformation.PresentationParameters;

#if XBOX
            pp.MultiSampleQuality = 0;
            pp.MultiSampleType = MultiSampleType.FourSamples;
            return;
#else
            int quality = 0;
            GraphicsAdapter adapter = e.GraphicsDeviceInformation.Adapter;
            SurfaceFormat format = adapter.CurrentDisplayMode.Format;

            // Check for 4xAA
            if (adapter.CheckDeviceMultiSampleType(DeviceType.Hardware, format, false, MultiSampleType.FourSamples, out quality))
            {
                // even if a greater quality is returned, we only want quality 0
                pp.MultiSampleQuality = 0;
                pp.MultiSampleType = MultiSampleType.FourSamples;
            }

            // Check for 2xAA
            else if (adapter.CheckDeviceMultiSampleType(DeviceType.Hardware, format, false, MultiSampleType.TwoSamples, out quality))
            {
                // even if a greater quality is returned, we only want quality 0
                pp.MultiSampleQuality = 0;
                pp.MultiSampleType = MultiSampleType.TwoSamples;
            }
            return;
#endif
        }



        protected virtual void LoadEngineFrameworks()
        {
            //load the 2D and 3D assemblies
            System.Reflection.Assembly assembly2D = null;
            System.Reflection.Assembly assembly3D = null;

#if !XBOX
            //avoid double-loading assemblies and get a reference to the assembly from the AppDomain
            Assembly[] assemblyList = AppDomain.CurrentDomain.GetAssemblies();

            for (int index = 0; index < assemblyList.Length; index++)
            {
                if (assemblyList[index].FullName.Contains("GarageGames.TorqueX.Framework2D"))
                {
                    assembly2D = assemblyList[index];
                    _engineComponent.RegisterAssembly(assembly2D);
                }
                else if (assemblyList[index].FullName.Contains("GarageGames.TorqueX.Framework3D"))
                {
                    assembly3D = assemblyList[index];
                    _engineComponent.RegisterAssembly(assembly3D);
                }
            }
#endif

            //if needed, manually load assemblies for the command-line export process
            if (assembly2D == null && assembly3D == null)
            {
                //create the scene renderer
                if (System.IO.File.Exists(_engineComponent.ExecutableDirectory + "\\GarageGames.TorqueX.Framework2D.dll"))
                {
                    //load the 2D assembly
                    assembly2D = System.Reflection.Assembly.LoadFrom(_engineComponent.ExecutableDirectory + "\\GarageGames.TorqueX.Framework2D.dll");
                    _engineComponent.RegisterAssembly(assembly2D);
                }


                //create the scene renderer
                if (System.IO.File.Exists(_engineComponent.ExecutableDirectory + "\\GarageGames.TorqueX.Framework3D.dll"))
                {
                    //load the 2D assembly
                    assembly3D = System.Reflection.Assembly.LoadFrom(_engineComponent.ExecutableDirectory + "\\GarageGames.TorqueX.Framework3D.dll");
                    _engineComponent.RegisterAssembly(assembly3D);
                }
            }

#if !XBOX
            // export schema if requested.
            if (ExportSchema)
            {
                int start = System.Environment.TickCount;

                TorqueConsole.Echo("\nExporting XML Schema to file: {0}", ExportSchemaFileName);
                new TorqueXmlSchemaExporter().Process(ExportSchemaFileName);


                System.Environment.Exit(0); // make sure we exit
            }
#endif

            //RenderInstanceType.Sky
            if (assembly3D != null)
            {
                object renderSky3D = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.SkyRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderSky3D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Mesh2D
            if (assembly2D != null)
            {
                object render2D = assembly2D.CreateInstance("GarageGames.Torque.RenderManager.T2DRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(render2D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Terrain
            if (assembly3D != null)
            {
                object renderTerrain3D = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.TerrainRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderTerrain3D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Mesh3D
            if (assembly3D != null)
            {
                object renderMesh3D = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.T3DRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderMesh3D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Avatar
            if (assembly3D != null)
            {
                object renderAvatar = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.AvatarRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderAvatar);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Shadow
            if (assembly3D != null)
            {
                object renderShadow3D = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.ShadowRenderManager");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderShadow3D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Translucent2D
            if (assembly2D != null)
            {
                object renderTranslucent2D = assembly2D.CreateInstance("GarageGames.Torque.RenderManager.TranslucentRenderManager2D");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderTranslucent2D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Translucent3D
            if (assembly3D != null)
            {
                object renderTranslucent3D = assembly3D.CreateInstance("GarageGames.Torque.RenderManager.TranslucentRenderManager3D");
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderTranslucent3D);
            }
            else
                RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Billboard
            RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(null);


            //RenderInstanceType.Refraction
            object renderRefraction3D = new RenderManager.RefractionManager();
            RenderManager.SceneRenderer.RenderManager.AddRenderBinToSceneRenderer(renderRefraction3D);
        }



        /// <summary>
        /// Setup the engine component.  By default this function attempts to load "torqueSettings.xml".  Override to do your own initialization.
        /// </summary>
        protected virtual void SetupEngineComponent()
        {
            // load settings
            TorqueConsole.Echo("Loading settings file...");
            this._engineComponent.LoadSettings("torqueSettings.xml");
        }



        /// <summary>
        /// Setup the Graphics Manager.  You can override this to change settings chosen by the engine in this function, if you so desire.
        /// </summary>
        protected virtual void SetupGraphics() { }



        /// <summary>
        /// Setup this Game Object.  Use this function if you need to initialize some kind of game state before BeginRun is called.  Otherwise, it 
        /// is best to setup the game in BeginRun().
        /// </summary>
        protected virtual void SetupGame() { }



        /// <summary>
        /// Override to include code that runs when the game is exiting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            // tell the engine component to shutdown audio.  This is needed because the component itself does not appear to get an exit callback.
            TorqueEngineComponent.Instance.ShutdownAudio();
        }



        /// <summary>
        /// One time game initialization.
        /// </summary>
        protected override void BeginRun()
        {
            base.BeginRun();

            Assert.Fatal(TorqueEngineComponent.Instance != null, "TorqueGame.BeginRun - This game requires a Torque Engine Component.");

            LoadEngineFrameworks();

        }



        /// <summary>
        /// Draw.  Also calls into the engine component so that it can do any pre-draw processing.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueGame.Draw");
#endif

            TorqueEngineComponent.Instance.OnStartDraw();

            base.Draw(gameTime);

#if DEBUG
            Profiler.Instance.EndBlock("TorqueGame.Draw");
#endif
        }



        /// <summary>
        /// End drawing.  Also calls into the engine component to do end draw processing.
        /// </summary>
        protected override void EndDraw()
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueGame.EndDraw");
#endif

            base.EndDraw();

            TorqueEngineComponent.Instance.OnEndDraw();

#if DEBUG
            Profiler.Instance.EndBlock("TorqueGame.EndDraw");
#endif
        }


        #endregion


        #region Private, protected, internal fields

        protected Microsoft.Xna.Framework.GraphicsDeviceManager _graphicsManager;
        protected GarageGames.Torque.XNA.TorqueEngineComponent _engineComponent;
        protected SceneLoader _sceneLoader;
        protected static bool _exportSchema;
        protected static string _exportSchemaFileName = "myschema.txschema";

        #endregion
    }
}
