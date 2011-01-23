//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Content;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.GameUtil
{
    /// <summary>
    /// Class that manages loading and unloading of Scene XML files.
    /// </summary>
    public class SceneLoader
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Set/Get the deserializer that the Scene Loader will use.  The same deserializer is used to deserialize all scenes.  This improves
        /// scene load performance.
        /// </summary>
        public TorqueXmlDeserializer Deserializer
        {
            get { return _deserializer; }
            set { _deserializer = value; }
        }


        /// <summary>
        /// This delegate is fired when a scene is loaded.
        /// </summary>
        public OnSceneLoadedDelegate OnSceneLoaded
        {
            get { return _onSceneLoaded; }
            set { _onSceneLoaded = value; }
        }



        /// <summary>
        /// This delegate is fired when a scene is unloaded.
        /// </summary>
        public OnSceneUnloadedDelegate OnSceneUnloaded
        {
            get { return _onSceneUnloaded; }
            set { _onSceneUnloaded = value; }
        }



        /// <summary>
        /// If true, the SceneLoader will cause the runtime to exit if a level load fails.  Default is true.
        /// The exit code returned in this case is GameExitCodes.SceneLoaderError.  Because the compact framework
        /// does not provide System.Environment.Exit, this is not supported on the Xbox 360.  On the Xbox 360,
        /// exceptions are throw to the top level, which will typically cause program termination.
        /// </summary>
        public bool ExitOnFailedLoad
        {
            get { return _exitOnFailedLoad; }
            set { _exitOnFailedLoad = value; }
        }



        public TorqueSceneData LastLoadedScene
        {
            get { return _lastLoadedScene; }
            set { _lastLoadedScene = value; }
        }

        #endregion


        #region Public methods

        public delegate void OnSceneLoadedDelegate(string sceneFileName, TorqueSceneData scene);



        public delegate void OnSceneUnloadedDelegate(string sceneFileName, TorqueSceneData scene);

        /// <summary>
        /// Load a scene.  If the scene is already loaded, the existing instance is returned.  
        /// </summary>
        /// <param name="sceneFileName"></param>
        /// <returns></returns>
        public TorqueSceneData Load(string sceneFileName)
        {
            // already loaded?
            TorqueSceneData scene;

            if (_loadedScenes.TryGetValue(sceneFileName, out scene))
            {
                _lastLoadedScene = scene;
                return scene;
            }

            // no, create new scene
            scene = new TorqueSceneData();
            scene.PreloadMaterials = true;
            scene.Deserializer = Deserializer;

            // we'll intercept content load exceptions, so that we display a (hopefully) helpful message for dealing with them
            try
            {
                // deserialize
                scene.Load(sceneFileName);
            }
            catch (ContentLoadException e)
            {
                string msg = "SceneLoader.Load - Error loading XML file: \'" + sceneFileName + "\', ContentLoadException: \'" + e.Message + "\'  If you are using TXB, make sure that you have saved your project, then rebuild your game in Express.";
                Assert.Fatal(false, msg);
#if !XBOX
                if (_exitOnFailedLoad)
                    System.Environment.Exit(GameExitCodes.SceneLoaderError); // this is here so that TXB knows we failed due to a content load problem
#endif
                throw e; // in case we did not exit
            }

            // add scene to loaded list
            _loadedScenes.Add(sceneFileName, scene);
            _lastLoadedScene = scene;

            // call delegate
            if (_onSceneLoaded != null)
                _onSceneLoaded(sceneFileName, scene);

            return scene;
        }



        /// <summary>
        /// Unload the named scene file.  If the scene is not loaded, nothing happens.
        /// </summary>
        /// <param name="sceneFileName">The scene file to unload.</param>
        public void Unload(string sceneFileName)
        {
            TorqueSceneData scene;

            if (_loadedScenes.TryGetValue(sceneFileName, out scene))
                Unload(scene);
        }



        /// <summary>
        /// Unload the specified scene object.  It is an error if the scene null or not loaded.
        /// </summary>
        /// <param name="scene">The scene object to unload.</param>
        public void Unload(TorqueSceneData scene)
        {
            Assert.Fatal(scene != null, "SceneLoader.Unload - Attempting to unload null scene.");
            Assert.Fatal(scene.Loaded, "SceneLoader.Unload - Specified scene not actually loaded");

            // unload
            scene.Unload();
            // remove from loaded scenes
            string keyToRemove = string.Empty;

            if (_loadedScenes.ContainsValue(scene))
            {
                foreach (string key in _loadedScenes.Keys)
                {
                    if (_loadedScenes[key] == scene)
                    {
                        keyToRemove = key;
                        break;
                    }
                }

                if (keyToRemove != string.Empty)
                    _loadedScenes.Remove(keyToRemove);
            }

            if (_lastLoadedScene == scene)
                _lastLoadedScene = null;

            // call delegate
            if (_onSceneUnloaded != null)
                _onSceneUnloaded(keyToRemove, scene);
        }



        /// <summary>
        /// Unload the last loaded scene.  Useful if you know your game is only going to be loading/unloading one scene file at a time.
        /// </summary>
        public void UnloadLastScene()
        {
            if (_lastLoadedScene != null)
                Unload(_lastLoadedScene);
        }

        #endregion


        #region Private, protected, internal fields

        TorqueXmlDeserializer _deserializer = new TorqueXmlDeserializer();
        Dictionary<string, TorqueSceneData> _loadedScenes = new Dictionary<string, TorqueSceneData>();
        TorqueSceneData _lastLoadedScene = null;
        OnSceneLoadedDelegate _onSceneLoaded;
        OnSceneUnloadedDelegate _onSceneUnloaded;
        bool _exitOnFailedLoad = true;

        #endregion
    }
}
