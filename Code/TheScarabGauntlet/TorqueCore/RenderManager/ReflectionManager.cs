//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Materials;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// The reflection manager manages dynamically updating the textures on materials based on
    /// the current state of the scene. This is a special render manager that isn't treated like
    /// the others (in fact, it doesn't inherit from BaseRenderManager). Materials that have
    /// IsReflection set will be added to this manager when the render instance they are attached
    /// to is added to the scene renderer.
    /// 
    /// Before the scene is rendered, this manager is updated to render the scene for each material
    /// to a texture. Reflection materials can have priorities set on them to only have them updated
    /// every other frame.
    /// </summary>
    public class ReflectionManager
    {
        /// <summary>
        /// Stores information about a material that reflects.
        /// </summary>
        struct ReflectionData
        {

            #region Private, protected, internal fields

            internal Matrix _transform;
            internal MathUtil.Box3F _bounds;
            internal IReflectionMaterial _material;

            #endregion
        };


        #region Public methods

        /// <summary>
        /// Adds a render instance to have its material updated.
        /// </summary>
        /// <param name="instance">The render instance.</param>
        public void AddElement(RenderInstance instance)
        {
            IReflectionMaterial reflectionMaterial = instance.Material as IReflectionMaterial;
            Assert.Fatal(reflectionMaterial != null, "ReflectionManager.AddElement - Attempting to add non IReflectionMaterial.");

            ReflectionData reflection;
            reflection._transform = instance.ObjectTransform;
            reflection._bounds = instance.WorldBox;
            reflection._material = reflectionMaterial;
            _reflectionList.Add(reflection);
        }



        /// <summary>
        /// Clears the list of materials.
        /// </summary>
        public void Clear()
        {
            _reflectionList.Clear();
        }



        /// <summary>
        /// Updates all the reflections based on their priority.
        /// </summary>
        /// <param name="srs">The current scene render state.</param>
        public void Update(SceneRenderState srs)
        {
            if (_reflectionList.Count == 0)
                return;

#if DEBUG
            Profiler.Instance.StartBlock(_updateProfileBlock);
#endif
            _frameCounter++;

            // is this an odd or even numbered frame?
            evenFrame = _frameCounter % 2 == 0;

            // always update priority 0
            updated = _Update(0, srs);

            // on even frames, update priority 2
            if (evenFrame)
                updated = _Update(2, srs);

            // on odd frames or if no 2s were updated, update priority 1
            if (!evenFrame || !updated)
                updated = _Update(1, srs);

            // not sure why we need to clear depth, but if we don't, messed up things happen
            srs.Gfx.Device.Clear(ClearOptions.DepthBuffer, Color.White, 1.0f, 0);

#if DEBUG
            Profiler.Instance.EndBlock(_updateProfileBlock);
#endif
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Updates all the reflections that have a specific priority.
        /// </summary>
        /// <param name="priority">The priority to update.</param>
        /// <param name="srs">The current scene render state.</param>
        /// <returns>True if something was updated.</returns>
        bool _Update(int priority, SceneRenderState srs)
        {
            int updated = 0;
            float radius;
            ReflectionData reflection;

            for (int i = 0; i < _reflectionList.Count; ++i)
            {
                reflection = _reflectionList[i];

                // only update the requested priority
                if (reflection._material.Priority != priority)
                    continue;

                // not in view so no reason to update
                radius = ((reflection._bounds.Max - reflection._bounds.Min) * 0.5f).Length();
                if (!srs.Frustum.Intersects(reflection._bounds, Matrix.Identity, radius))
                    continue;

                reflection._material.Update(srs, reflection._transform);
                updated++;
            }

            return updated > 0;
        }

        #endregion


        #region Private, protected, internal fields

        int _frameCounter;
        bool evenFrame;
        bool updated;
        List<ReflectionData> _reflectionList = new List<ReflectionData>();

#if DEBUG
        ProfilerCodeBlock _updateProfileBlock = new ProfilerCodeBlock("ReflectionManager.Update");
#endif

        #endregion
    }
}
