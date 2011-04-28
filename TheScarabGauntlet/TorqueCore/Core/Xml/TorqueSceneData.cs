//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;
using System.ComponentModel;



namespace GarageGames.Torque.Core.Xml
{
    /// <summary>
    /// Base class for all loadable xml files.  It is recommended that you subclass this class if TorqueSceneData does not meet your needs and 
    /// you want to define your own loaded level.
    /// </summary>
    public class BaseTorqueSceneData
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Version element.  This is for use when we have different versions of the XML scheme in future products.  The XML file can be
        /// deserialized into an instance of this class, which will just read the Version element, and then we can use the version 
        /// element to prepare a TorqueSceneData object specific to that version.  
        /// </summary>
        public string Version;



        public delegate void OnLoadedDelegate();



        public delegate void OnUnloadedDelegate();

        /// <summary>
        /// When a level is loaded, this delegate is fired.
        /// </summary>
        [XmlIgnore]
        public OnLoadedDelegate OnLoaded
        {
            get { return _onLoaded; }
            set { _onLoaded = value; }
        }



        /// <summary>
        /// When a level is unloaded, this delegate is fired.
        /// </summary>
        [XmlIgnore]
        public OnUnloadedDelegate OnUnloaded
        {
            get { return _onUnloaded; }
            set { _onUnloaded = value; }
        }

        #endregion


        #region Private, protected, internal fields

        OnLoadedDelegate _onLoaded;
        OnUnloadedDelegate _onUnloaded;

        #endregion
    }


    /// <summary>
    /// Standard Level Data implementation for Torque.  The name "Level Data" is a bit too specialized, since this class can load 
    /// essentially any type of C# object from XML and install into the engine.  The top level property lists in this object 
    /// (SceneData, Materials, Objects, ...) illustrate the typical kind of objects that are loaded.  Use this class to load ".txscene" 
    /// files and xml files with <TorqueSceneData/> as the root element.
    /// </summary>
    public class TorqueSceneData : BaseTorqueSceneData
    {
        #region Static fields

        /// <summary>
        /// Version of the XML format.  This will be bumped each time the format of the XML changes in an incompatible way.  
        /// Which hopefully won't be often.  Essentially the top level deserialized properties of this class define what is valid in the XML.
        /// </summary>
        public static readonly string RequiredVersion = "1.0";

        #endregion


        #region Constructors

        /// <summary>
        /// Aside from constructing the class, this also sets up the DefaultTypeMap, which you can access with the DefaultTypeMap property.
        /// </summary>
        public TorqueSceneData()
        {
            _defaultTypeMap = new Dictionary<string, Type>(100);

            //find the 2D objects
            if (System.IO.File.Exists(TorqueEngineComponent.Instance.ExecutableDirectory + "\\GarageGames.TorqueX.Framework2D.dll"))
            {
                System.Reflection.Assembly asm = Assembly.LoadFrom(TorqueEngineComponent.Instance.ExecutableDirectory + "\\GarageGames.TorqueX.Framework2D.dll");
                Type typeObject;

                //extract types from the assembly
                Type[] typeArray = asm.GetTypes();

                for (int typeIndex = 0; typeIndex < typeArray.Length; typeIndex++)
                {
                    typeObject = typeArray[typeIndex];

                    if (!_defaultTypeMap.ContainsKey(typeObject.Name))
                    {
                        _defaultTypeMap.Add(typeObject.Name, typeObject);

                        //backwards compatability
                        if (typeObject.Name.CompareTo("T2DScroller") == 0)
                            _defaultTypeMap.Add("Scroller", typeObject);
                        if (typeObject.Name.CompareTo("T2DStaticSprite") == 0)
                            _defaultTypeMap.Add("StaticSprite", typeObject);
                        if (typeObject.Name.CompareTo("T2DAnimationData") == 0)
                            _defaultTypeMap.Add("AnimationData", typeObject);
                        if (typeObject.Name.CompareTo("T2DAnimatedSprite") == 0)
                            _defaultTypeMap.Add("AnimatedSprite", typeObject);
                        if (typeObject.Name.CompareTo("T2DTileObject") == 0)
                            _defaultTypeMap.Add("Tile", typeObject);
                        if (typeObject.Name.CompareTo("T2DTileType") == 0)
                            _defaultTypeMap.Add("TileType", typeObject);
                        if (typeObject.Name.CompareTo("T2DTileLayer") == 0)
                            _defaultTypeMap.Add("TileLayer", typeObject);
                        if (typeObject.Name.CompareTo("T2DSceneObject2D") == 0)
                            _defaultTypeMap.Add("SceneObject2D", typeObject);
                        if (typeObject.Name.CompareTo("T2DSceneCamera") == 0)
                            _defaultTypeMap.Add("Camera2D", typeObject);
                    }
                }
            }

            //find the 3D objects
            if (System.IO.File.Exists(TorqueEngineComponent.Instance.ExecutableDirectory + "\\GarageGames.TorqueX.Framework3D.dll"))
            {
                System.Reflection.Assembly asm = Assembly.LoadFrom(TorqueEngineComponent.Instance.ExecutableDirectory + "\\GarageGames.TorqueX.Framework3D.dll");
                Type typeObject;

                //extract types from the assembly
                Type[] typeArray = asm.GetTypes();

                for (int typeIndex = 0; typeIndex < typeArray.Length; typeIndex++)
                {
                    typeObject = typeArray[typeIndex];

                    //skip over abstract classes, can't instantiate them
                    if (typeObject.IsAbstract)
                        continue;


                    if (!_defaultTypeMap.ContainsKey(typeObject.Name))
                        _defaultTypeMap.Add(typeObject.Name, typeObject);
                }
            }


            // set up some mappings for element names to types.  These are used when the elements do not have a "type" attribute.
            // this is primarily a convenience so that the xml does not have to declare the type for "known" objects.  one can also add 
            // game-specific types to this list prior to deserialization.  It is not necessary to extend this list to add new types,
            // but if you don't put the type here, then you must put a "type" attribute in the xml for all objects of that type.
            _defaultTypeMap.Add("TXNALevelData", typeof(TorqueSceneData));
            _defaultTypeMap.Add("TorqueXLevelData", typeof(TorqueSceneData));
            _defaultTypeMap.Add("TorqueSceneData", typeof(TorqueSceneData));

            // Gui types
            _defaultTypeMap.Add("GUICanvas", typeof(Torque.GUI.GUICanvas));
            _defaultTypeMap.Add("GUISceneview", typeof(Torque.GUI.GUISceneview));
            _defaultTypeMap.Add("GUIControl", typeof(Torque.GUI.GUIControl));
            _defaultTypeMap.Add("GUIStyle", typeof(Torque.GUI.GUIStyle));
            _defaultTypeMap.Add("GUIText", typeof(Torque.GUI.GUIText));
            _defaultTypeMap.Add("GUITextStyle", typeof(Torque.GUI.GUITextStyle));
            _defaultTypeMap.Add("GUIMLText", typeof(Torque.GUI.GUIMLText));
            _defaultTypeMap.Add("GUIMLTextStyle", typeof(Torque.GUI.GUIMLTextStyle));
            _defaultTypeMap.Add("GUIBitmap", typeof(Torque.GUI.GUIBitmap));
            _defaultTypeMap.Add("GUIBitmapStyle", typeof(Torque.GUI.GUIBitmapStyle));
            _defaultTypeMap.Add("GUIButton", typeof(Torque.GUI.GUIButton));
            _defaultTypeMap.Add("GUIButtonStyle", typeof(Torque.GUI.GUIButtonStyle));
            _defaultTypeMap.Add("GUISplash", typeof(Torque.GUI.GUISplash));
            _defaultTypeMap.Add("GUISplashStyle", typeof(Torque.GUI.GUISplashStyle));
            _defaultTypeMap.Add("GUIVideo", typeof(Torque.GUI.GUIVideo));
            _defaultTypeMap.Add("GUIVideoStyle", typeof(Torque.GUI.GUIVideoStyle));

            // Materials
            _defaultTypeMap.Add("SimpleMaterial", typeof(SimpleMaterial));
            _defaultTypeMap.Add("LightingMaterial", typeof(LightingMaterial));
            _defaultTypeMap.Add("RefractionMaterial", typeof(RefractionMaterial));
            _defaultTypeMap.Add("CubemapMaterial", typeof(CubemapMaterial));
            _defaultTypeMap.Add("DetailMaterial", typeof(DetailMaterial));
            _defaultTypeMap.Add("DistanceFog", typeof(DistanceFog));
            _defaultTypeMap.Add("GenericMaterial", typeof(GenericMaterial));
            _defaultTypeMap.Add("GenericMaterialFloatBind", typeof(GenericMaterialFloatBind));
            _defaultTypeMap.Add("GenericMaterialTextureBind", typeof(GenericMaterialTextureBind));
            _defaultTypeMap.Add("TechniqueChainEntry", typeof(TechniqueChainEntry));
            _defaultTypeMap.Add("CellCountDivider", typeof(CellCountDivider));
            _defaultTypeMap.Add("CellSizeDivider", typeof(CellSizeDivider));
            _defaultTypeMap.Add("GenericTextureDivider", typeof(GenericTextureDivider));

            // Other
            _defaultTypeMap.Add("PointLight", typeof(Lighting.PointLight));
            _defaultTypeMap.Add("DirectionalLight", typeof(Lighting.DirectionalLight));
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Load the named level and return a new TorqueSceneData instance.  In order to customize the Level data load process you must 
        /// create the TorqueSceneData object yourself and call Load on it.
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="extraAssemblies">List of assemblies to add to the deserializer's assembly list, for finding new types.  Usually can be null.</param>
        /// <returns>A TorqueSceneData instance</returns>
        public static TorqueSceneData LoadScene(string filename, List<Assembly> extraAssemblies)
        {
            TorqueSceneData ld = new TorqueSceneData();
            return ld.Load(filename, extraAssemblies);
        }



        /// <summary>
        /// Load the specified level data file.  
        /// </summary>
        /// <param name="filename">Filename to load.</param>
        /// <returns>This TorqueSceneData instance.</returns>
        public TorqueSceneData Load(string filename)
        {
            Assert.Fatal(XNA.TorqueEngineComponent.Instance.ExecutableAssembly != null, "TorqueSceneData.Load - ExecutableAssembly not set.");
            return Load(filename, null);
        }



        /// <summary>
        /// Load the named level.  By default, this will also create an new Torque Folder for the level and install it as the current folder.
        /// When you Unload the level, that torque folder, and everything in it, will be unregistered.  Set CreateLevelFolder to false to 
        /// disable this behavior.
        /// </summary>
        /// <param name="filename">The filename to load</param>
        /// <param name="extraAssemblies">List of assemblies to add to the deserializer's assembly list, for finding new types.  Usually can be null.</param>
        public TorqueSceneData Load(string filename, List<Assembly> extraAssemblies)
        {
            Assert.Fatal(_allowReload || !_loaded, "TorqueSceneData.Load - Level already loaded, Unload it first.");

            if (_loaded)
            {
                if (_allowReload)
                    Reset();
                else
                    return null;
            }

            _loaded = true;

            if (filename == null || filename == String.Empty)
                throw new Exception("Invalid level file name: " + filename);


            if (_createLevelFolder) // JMQtodo: move this into scene loader?
            {
                // create a folder for game objects that are created after this level is loaded
                _levelFolder = new TorqueFolder();
                // we'll give it a helpful name for debugging
                _levelFolder.Name = "_autoname_TorqueSceneDataFolder" + this.GetHashCode();

                TorqueObjectDatabase.Instance.Register(_levelFolder);
                TorqueObjectDatabase.Instance.CurrentFolder = _levelFolder;
            }

            // deserialize the xml
            TorqueXmlDeserializer d = Deserializer;

            //d.LoadTypesFromAssemblies = true;
            d.DefaultTypeMap = this.DefaultTypeMap;
            List<Assembly> searchAssemblies = d.Assemblies;

            if (extraAssemblies != null)
                foreach (Assembly a in extraAssemblies)
                    if (!searchAssemblies.Contains(a))
                        searchAssemblies.Add(a);

            // deserialize into this instance
            d.Process(filename, this);

            // check version
            if (this.Version != RequiredVersion)
                throw new Exception("Cannot load this level version:" + this.Version);

            _LoadObjects(this.SceneData);
            _LoadObjects(this.Materials);
            _LoadObjects(this.Objects);

            if (_preloadMaterials)
            {
                // helps to reduce startup hitching
                MaterialManager.PreloadMaterials(null);
            }

            if (this.OnLoaded != null)
                this.OnLoaded();

            return this;
        }



        /// <summary>
        /// Unload the level.  This attempts to fully unload any objects that were loaded by this level.  It also unregister's the 
        /// level data Torque Folder created in the Load() function, if any, which will unregister any objects that were registered since
        /// the level was loaded (unless a different folder was set to be the current folder after the level was loaded).
        /// </summary>
        public void Unload()
        {
            Assert.Fatal(_loaded, "TorqueSceneData.Unload - Level not loaded, Load it first.");

            if (!_loaded)
                return;

            _loaded = false;

            // we fire the callback first so that the recipient can unhook itself from content
            if (this.OnUnloaded != null)
                this.OnUnloaded();

            if (_levelFolder != null && _levelFolder.IsRegistered)
            {
                // unregister the torque folder.  This should unregister everything in it, which will include all objects that were registered
                // since the time the level was created.
                TorqueObjectDatabase.Instance.Unregister(_levelFolder);
                _levelFolder = null; // we null things out for garbage collector
            }

            // now walk our lists and Unload stuff
            _UnloadObjects(this.SceneData);
            _UnloadObjects(this.Materials);
            _UnloadObjects(this.Objects);
            this.SceneData = null;
            this.Materials = null;
            this.Objects = null;
        }



        /// <summary>
        /// Clear this object.  Only used for instances that allow reloading.
        /// </summary>
        public void Reset()
        {
            this.SceneData = null;
            this.Materials = null;
            this.Objects = null;
            _loaded = false;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // deserialize these in place so that the xml can contain multiple hierarchies for each of these types and the results
        // are all added together.

        /// <summary>
        /// Contains list of objects that represent global scene objects.  Also contains some objects that control subsequent
        /// deserialization (such as the ObjectTypeDeclaration).  This should be the first group in an XML file.  This is not strictly enforced,
        /// but you could run into load-order issues with some objects (like the ObjectTypeDeclaration) if you don't do it.  This list is 
        /// deserialized in place (see also TorqueXmlDeserializeInPlace).
        /// </summary>
        [TorqueXmlDeserializeInPlace]
        public List<object> SceneData = new List<object>();

        /// <summary>
        /// Contains list of Materials. This list is 
        /// deserialized in place (see also TorqueXmlDeserializeInPlace).
        /// </summary>
        [TorqueXmlDeserializeInPlace]
        public List<object> Materials = new List<object>();

        /// <summary>
        /// Contains list of Objects.  This includes non-template objects and template objects.  Non-template objects are registered after loading;
        /// templates are not registered, but their names are installed into the Name database so that they can later be cloned/registered.
        /// </summary>
        [TorqueXmlDeserializeInPlace]
        public List<object> Objects = new List<object>();



        /// <summary>
        /// The default type mapping.  
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, Type> DefaultTypeMap
        {
            get { return _defaultTypeMap; }
            set { _defaultTypeMap = value; }
        }



        /// <summary>
        /// The deserializer that the Level Data object uses.  The get accessor will create a new deserializer and return it if none exists already.
        /// </summary>
        [XmlIgnore]
        public TorqueXmlDeserializer Deserializer
        {
            get
            {
                if (_deserializer == null)
                    _deserializer = new TorqueXmlDeserializer();
                return _deserializer;
            }
            set
            {
                _deserializer = value;
            }
        }


        /// <summary>
        /// Whether the level data object should preload material objects after loading.  Preloading is generally desirable because it reduces
        /// startup hitching.
        /// </summary>
        [BrowsableAttribute(false)]
        public bool PreloadMaterials
        {
            get { return _preloadMaterials; }
            set { _preloadMaterials = value; }
        }



        /// <summary>
        /// Indicates whether a level is currently loaded in this object.
        /// </summary>
        [XmlIgnore]
        public bool Loaded
        {
            get { return _loaded; }
            set { _loaded = value; }
        }



        /// <summary>
        /// Indicates whether this level object allows reloading of levels in the same instance.
        /// </summary>
        [XmlIgnore]
        public bool AllowReload
        {
            get { return _allowReload; }
            set { _allowReload = value; }
        }



        /// <summary>
        /// If true, a Torque Folder will be created and installed as the Current Folder just prior to loading a level.  After the level is finished
        /// loading, this folder is left as the CurrentFolder, until Unload is called, at which time the folder is unregistered and destroyed.  
        /// The effect of this is that any objects registered while the level is loaded whill be unregistered and removed when the level is 
        /// unloaded.  This provides a handy way to clean up objects, but you need to be aware that it is happening if you want to register 
        /// an object during a level and have that object persist after the level is unloaded.  To do this you will need to change the Folder
        /// property on the object to a different folder after you have registered it.  The default for this parameter is true.
        /// </summary>
        [XmlIgnore]
        public bool CreateLevelFolder
        {
            set { _createLevelFolder = value; }
            get { return _createLevelFolder; }
        }

        #endregion


        #region Private, protected, internal methods

        private void _LoadObjects(List<object> list)
        {
            if (list == null)
                return;

            foreach (object obj in list)
            {
                // call OnLoad on the object if applicable, before doing anything else with it.
                if (obj is TorqueBase)
                    (obj as TorqueBase).OnLoaded();

                // handle TorqueObjects
                TorqueObject tObj = obj as TorqueObject;

                if (tObj != null)
                {
                    // if the object is not a template, register it now
                    if (!tObj.IsTemplate && !tObj.IsRegistered)
                        TorqueObjectDatabase.Instance.Register(tObj);
                }
            }
        }



        private void _UnloadObjects(List<object> list)
        {
            foreach (object obj in list)
            {
                // do not unload persistent objects
                if (obj is TorqueObject && (obj as TorqueObject).IsPersistent)
                    continue;

                // call OnUnloaded on the object if applicable, before doing anything else with it.
                if (obj is TorqueBase)
                    (obj as TorqueBase).OnUnloaded();

                // handle TorqueObjects
                TorqueObject tObj = obj as TorqueObject;
                if (tObj != null)
                {
                    if (tObj.IsRegistered)
                        TorqueObjectDatabase.Instance.Unregister(tObj);
                }

                // clear any safe pointer refs
                if (obj is TorqueBase)
                    (obj as TorqueBase).Reset();

                // Dispose it
                if (obj is IDisposable)
                    (obj as IDisposable).Dispose();
            }
        }

        #endregion


        #region Private properties, operators, constants, and enums

        Dictionary<string, Type> _defaultTypeMap;
        bool _preloadMaterials = true;
        TorqueXmlDeserializer _deserializer;
        TorqueFolder _levelFolder;
        bool _loaded;
        bool _createLevelFolder = true;
        bool _allowReload;

        #endregion
    }
}


