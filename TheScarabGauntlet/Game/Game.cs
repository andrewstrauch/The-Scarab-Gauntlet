using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.GUI;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    class Game : TorqueGame
    {

        static TorqueGame _game;
        Random _random;
        float totalTime;
        float _gameStart;
        bool paused = false;
        List<TorqueObject> _players = new List<TorqueObject>();
        Pause_GUI pauseGUI;

        #region Properties
        public static Game Instance
        {
            get { return _game as Game; }
        }
        public Random Random
        {
            get { return _random; }
        }

        public float Time
        {
            get { return totalTime; }
        }

        public float GameStart
        {
            get { return _gameStart; }
        }

        public List<TorqueObject> Players
        {
            get { return _players; }
        }
        #endregion

        public Game()
        {
            Assert.Fatal(_game == null, "doh");
            _game = this;
            _random = new Random();
        }

        protected override void SetupEngineComponent()
        {
            base.SetupEngineComponent();

            // register the platformer framework with the engine component, so that we can load its types from XML
            base._engineComponent.RegisterAssembly(typeof(PlatformerData).Assembly);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            totalTime += gameTime.ElapsedGameTime.Milliseconds;
        }

        protected override void BeginRun()
        {
            base.BeginRun();
            pauseGUI = new Pause_GUI();
//#if !DEBUG
            StartMenu_GUI openingMenu = new StartMenu_GUI();
            GUICanvas.Instance.SetContentControl(openingMenu);
//#else
            // load the test level        
  //          SceneLoader.Load(@"data\levels\Level1.txscene");
//#endif
            
            //1_new.txscene");
            //SceneLoader.Load(@"data\levels\sample_level.txscene");
           //SceneLoader.Load(@"data\levels\traps_test.txscene");
           //SceneLoader.Load(@"data\levels\test.txscene");

            //SceneLoader.Load(@"data\levels\hulk_test.txscene");


            // create game gui
           /* GUIStyle playStyle = new GUIStyle();
            GUISceneview play = new GUISceneview();
            play.Name = "PlayScreen";
            play.Style = playStyle;
            T2DSceneCamera camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>("Camera");
            play.Camera = camera;
            GUICanvas.Instance.SetContentControl(play);
            */
            // game start
            _gameStart = Time;

        }

        public void TogglePause()
        {
            // Toggle pause on and off
            paused = !paused;

            if (paused)
            {
                GUICanvas.Instance.PushDialogControl(pauseGUI, 1);
                Game.Instance.Engine.GameTimeScale = 0.0f;
            }
            else
            {
                GUICanvas.Instance.PopDialogControl(pauseGUI);
                Game.Instance.Engine.GameTimeScale = 1.0f;
            }
        }

        public void Reset()
        {
            SceneLoader.UnloadLastScene();
            
            if(paused)
                TogglePause();
            
            SceneLoader.Load(@"data\levels\Level1.txscene");//SceneLoader.Load(@"data\levels\Level1.txscene");
        }
    }
}