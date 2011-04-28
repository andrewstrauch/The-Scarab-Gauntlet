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
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// A data structure to be set on a render instance to specify information about how the
    /// material should behave on that object. An instance of this structure can be set on a
    /// RenderInstance, to be passed to the Init method of a RenderMaterial or accessed from
    /// the RenderInstance in SetupObjectData.
    /// </summary>
    abstract public class MaterialInstanceData { }



    /// <summary>
    /// Materials that provide the ability to set and get texture should implement this. It is
    /// not strictly necessary, but it allows other subsystems, like the TextureDivider, to
    /// access the texture data on a material.
    /// </summary>
    public interface ITextureMaterial
    {
        #region Interface members

        /// <summary>
        /// The filename of the texture.
        /// </summary>
        string TextureFilename
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the texture resource for the material.
        /// </summary>
        Resource<Texture> Texture
        {
            get;
        }

        #endregion
    }



    /// <summary>
    /// Materials that need to have textures updated frequently, like for reflections, should
    /// implement this interface. Any material with this interface will be added to the reflection
    /// render manager and will have update called every frame or every other frame, depending on
    /// the priority.
    /// </summary>
    public interface IReflectionMaterial
    {
        #region Inteface members

        /// <summary>
        /// Called by the reflection render manager to allow the material to update itself.
        /// </summary>
        /// <param name="srs">The current render state of the scene. Do not change this. Materials
        /// that need to do their own render pass should create their own scene render state.</param>
        /// <param name="objectTransform">The transform of the object this material is on. This
        /// obviously won't work correctly if the material is applied to multiple objects (actually,
        /// this method will be called once for each object it is on). But, materials of this type
        /// will almost certainly need to be unique.</param>
        void Update(SceneRenderState srs, Matrix objectTransform);

        /// <summary>
        /// The priority of the reflection. Since doing an entire extra pass over the scene (which
        /// most reflections will need to do) is expensive, this can be set to only update a reflection
        /// at certain intervals. Priority 0 will always be updated. Priority 1 will be updated every
        /// odd frame, or if no priority 2 was updated. Priority 2 will be updated every even frame.
        /// </summary>
        int Priority
        {
            get;
        }

        #endregion
    }



    /// <summary>
    /// Materials that do refraction, or more generally, use the existing contents of the back buffer
    /// as a texture, should implement this interface. Objects using this material will always be
    /// rendered last and will have the back buffer texture passed to the SetTexture method before
    /// they are rendered.
    /// </summary>
    public interface IRefractionMaterial
    {
        /// <summary>
        /// Called by the refraction render manager to set the back buffer texture on the material.
        /// </summary>
        /// <param name="texture">The back buffer texture.</param>
        void SetTexture(Texture2D texture);
    }



    /// <summary>
    /// An interface for fog materials that allows them to notify the render manager how an object
    /// will be affected by fog.
    /// </summary>
    public interface IFogMaterial
    {
        #region Interface members

        /// <summary>
        /// This is called by the render manager during the normal render pass. If this method returns false, 
        /// the render instance passed will be drawn. Otherwise, the render instance will be skipped. A return 
        /// value of true means the object would be completely obscured by fog and should be skipped.
        /// </summary>
        /// <param name="camPos">The current position of the camera.</param>
        /// <param name="bounds">Bounding box to check.</param>
        /// <returns>True if the render instance is completely obscured by fog and should not be rendered.</returns>
        bool IsObjectObscured(Vector3 camPos, Box3F bounds);

        /// <summary>
        /// This method is called by the render manager during a fog pass. If this method returns true, the render 
        /// instance passed will be drawn. Otherwise, the render instance will be skipped. A return value of false 
        /// means the object is not fogged at all and should be skipped by the fog pass.
        /// </summary>
        /// <param name="camPos">The current position of the camera.</param>
        /// <param name="bounds">Bounding box to check.</param>
        /// <returns>True if the render instance will be fogged at all.</returns>
        bool IsObjectFogged(Vector3 camPos, Box3F bounds);

        #endregion
    }



    /// <summary>
    /// Manages materials in the engine. Maps texture names to materials, if applicable, and also handles
    /// material preloading. Materials don't have to be added to this manager if the aforementioned benefits
    /// aren't required.
    /// </summary>
    public class MaterialManager
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Lookup a material with the specified mapped name.  
        /// </summary>
        /// <param name="name">Mapped name of the material.</param>
        /// <returns>The mapped material, or null if it wasn't found.</returns>
        public static RenderMaterial Lookup(String name)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            RenderMaterial material;
            name = _MapName(name);
            if (_mappedMaterials.TryGetValue(name, out material))
                return material;

            return null;
        }

        /// <summary>
        /// Lookup a material with the specified object name.  
        /// </summary>
        /// <param name="name">The object name of the material.</param>
        /// <returns>The material or null if it wasn't found.</returns>
        public static RenderMaterial LookupByName(String name)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            var material = TorqueObjectDatabase.Instance.FindObject(name) as RenderMaterial;
            if (_materials.Contains(material))
                return material;

            return null;
        }

        /// <summary>
        /// Remove the specified material from the render manager.  Also removes any mappings to that material.
        /// </summary>
        /// <param name="material">The material to remove.</param>
        public static void Remove(RenderMaterial material)
        {
            Assert.Fatal(material != null, "Null material passed to Remove");
            if (material == null)
                return;

            _materials.Remove(material);
            string keyToRemove = null;
            foreach (KeyValuePair<string, RenderMaterial> pair in _mappedMaterials)
            {
                if (pair.Value == material)
                {
                    keyToRemove = pair.Key;
                    break;
                }
            }

            if (keyToRemove != null)
                _mappedMaterials.Remove(keyToRemove);
        }

        /// <summary>
        /// Adds the specified material to the material manager without a name. The material can be remapped
        /// with a name later.
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static RenderMaterial Add(RenderMaterial material)
        {
            return Add(string.Empty, material);
        }

        /// <summary>
        /// Add the specified material to the manager with the specified name. If the material is already in the 
        /// manager, it is remapped to the specified name.
        /// </summary>
        /// <param name="name">Mapped name for material</param>
        /// <param name="material">The material</param>
        /// <returns>The material</returns>
        public static RenderMaterial Add(String name, RenderMaterial material)
        {
            Assert.Fatal(material != null, "Cannot add mapping to null material");

            if (!string.IsNullOrEmpty(name))
            {
                name = _MapName(name);
                RenderMaterial tempMat;
                if (_mappedMaterials.TryGetValue(name, out tempMat))
                    return null;
                _mappedMaterials[name] = material;
            }

            // the material might already have been added to the material list
            // don't re-add it
            if (!_materials.Contains(material))
                _materials.Add(material);

            return material;
        }

        /// <summary>
        /// Preload all materials.  This works be calling Init, SetupPass, and Cleanup on each loaded material.
        /// </summary>
        /// <param name="srs">SceneRenderState to use for preload.  If null, the function will create its own
        /// scene render state.</param>
        public static void PreloadMaterials(SceneRenderState srs)
        {
            if (srs == null)
            {
                srs = new SceneRenderState();
                srs.SceneGraph = null;
                srs.Gfx = GFXDevice.Instance;
            }

            bool wasPreload = srs.IsPreloadPass;
            srs.IsPreloadPass = true;
            foreach (RenderMaterial m in _materials)
            {
                m.SetupEffect(srs, null);
                while (m.SetupPass()) ;
                m.CleanupEffect();
            }
            srs.IsPreloadPass = wasPreload;
        }

        /// <summary>
        /// Clears all materials from the manager leaving it empty.
        /// </summary>
        public static void RemoveAll()
        {
            _mappedMaterials.Clear();
            _materials.Clear();
        }

        /// <summary>
        /// Read only access to the material list.
        /// </summary>
        public static List<RenderMaterial> Materials
        {
            get { return _materials; }
        }

        //------------------------------------------------------
        #region Private, protected, internal static methods

        static String _MapName(String rawName)
        {
            String prefix = string.Empty; // System.IO.Directory.GetCurrentDirectory();
#if !XBOX
            if (!rawName.StartsWith(prefix, true, System.Globalization.CultureInfo.CurrentCulture))
#else
            if (!rawName.StartsWith(prefix))
#endif
                prefix = String.Empty;

            StringBuilder name = new StringBuilder(rawName, prefix.Length, rawName.Length - prefix.Length, 0);
            for (int i = 0; i < name.Length; i++)
                name[i] = System.Char.ToLower(name[i]);
            int lastFour = name.Length - 4;
            if (lastFour >= 0 && name[lastFour] == '.')
            {
                // Check suffix, if it's a gfx suffix then trim it.
                // Do this the hard way because not sure how else to do it
                // without copying to a string prematurely.
                char a = name[lastFour + 1];
                char b = name[lastFour + 2];
                char c = name[lastFour + 3];
                bool doTrim = false;
                if (a == 'j' && b == 'p' && c == 'g')
                    doTrim = true;
                else if (a == 'p' && b == 'n' && c == 'g')
                    doTrim = true;
                else if (a == 'b' && b == 'm' && c == 'p')
                    doTrim = true;
                else if (a == 'g' && b == 'i' && c == 'f')
                    doTrim = true;

                if (doTrim)
                    name.Remove(lastFour, 4);
            }
            if (name.Length > 0 && name[0] == '\\')
                name.Remove(0, 1);
            return name.ToString();
        }

        #endregion

        //------------------------------------------------------
        #region Private, protected, internal static fields

        // internal for unit test access
        // name mapped materials
        static internal Dictionary<String, RenderMaterial> _mappedMaterials = new Dictionary<string, RenderMaterial>();

        // all materials
        static internal List<RenderMaterial> _materials = new List<RenderMaterial>();

        #endregion

        #endregion
    }
}
