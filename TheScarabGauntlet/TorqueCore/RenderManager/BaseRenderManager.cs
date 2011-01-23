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
using GarageGames.Torque.GFX;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Materials;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// Compares render instances and sorts them so that instances with the same
    /// materials are next to each other. And within that, instances with the same
    /// vertex buffer are next to each other.
    /// </summary>
    class RenderInstanceComparison : Comparer<RenderInstance>
    {

        #region Public methods

        public override int Compare(RenderInstance x, RenderInstance y)
        {
            int test = x.MaterialSortKey - y.MaterialSortKey;

            return (test == 0 ? x.GeometrySortKey - y.GeometrySortKey : test);
        }

        #endregion
    }



    /// <summary>
    /// Base render manager class. This provides basic functionality useful for derived render
    /// managers that implement rendering for specific render instance types.
    /// </summary>
    public class BaseRenderManager : IDisposable
    {

        #region Constructors

        public BaseRenderManager()
        {
            _elementList = new List<RenderInstance>();
            _comparer = new RenderInstanceComparison();
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Adds a render instance to the render manager.
        /// </summary>
        /// <param name="instance">The instance to add.</param>
        public virtual void AddElement(RenderInstance instance)
        {
            Assert.Fatal(instance.Material != null, "Cannot render objects without materials");
            Assert.Fatal(instance.VertexCount > 0, "Render instance must have > 0 vertices");
            Assert.Fatal(instance.PrimitiveCount > 0, "Render instance must have > 0 primitives");

            if (instance.Material == null)
                return;

            if (instance.VertexCount == 0)
                return;

            if (instance.PrimitiveCount == 0)
                return;

            if (instance.Material != null)
            {
                int sortKey = 0;

                int matTypeCode = instance.Material.GetType().GetHashCode();
                long hi10 = matTypeCode & 0xFFC00000;
                hi10 = hi10 >> 22;
                long mid10 = matTypeCode & 0x3FF000;
                mid10 = mid10 >> 12;
                long lo10 = matTypeCode & 0xFFC;
                lo10 = lo10 >> 2;
                long lo2 = matTypeCode & 0x3;
                sortKey = (int)(hi10 ^ mid10 ^ lo10 ^ lo2) << 20;

                int matInstCode = instance.Material.GetHashCode();
                hi10 = matInstCode & 0xFFC00000;
                hi10 = hi10 >> 22;
                mid10 = matTypeCode & 0x3FF000;
                mid10 = mid10 >> 12;
                lo10 = matTypeCode & 0xFFC;
                lo10 = lo10 >> 2;
                lo2 = matTypeCode & 0x3;
                sortKey = (int)(hi10 ^ mid10 ^ lo10 ^ lo2) << 10;

                if (instance.MaterialInstanceData != null)
                {
                    int matDataCode = instance.MaterialInstanceData.GetHashCode();
                    hi10 = matDataCode & 0xFFC00000;
                    hi10 = hi10 >> 22;
                    mid10 = matTypeCode & 0x3FF000;
                    mid10 = mid10 >> 12;
                    lo10 = matTypeCode & 0xFFC;
                    lo10 = lo10 >> 2;
                    lo2 = matTypeCode & 0x3;
                    sortKey = (int)(hi10 ^ mid10 ^ lo10 ^ lo2);
                }

                instance.MaterialSortKey = sortKey;
            }

            if (instance.VertexBuffer != null)
                instance.GeometrySortKey = instance.VertexBuffer.GetHashCode();

            _elementList.Add(instance);
        }



        /// <summary>
        /// Sorts the render instances that have been added to this manager..
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public virtual void Sort(SceneRenderState srs)
        {
            _elementList.Sort(_comparer);
        }



        /// <summary>
        /// Renders a z pass (depth buffer only, no color)
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public virtual void RenderZPass(SceneRenderState srs)
        {
            if (_elementList.Count == 0)
                return;

            _RenderGroup(_elementList, _zPassMaterial, null, srs, srs.Gfx.Device);
        }



        /// <summary>
        /// Renders opaque render instances.
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public virtual void RenderOpaquePass(SceneRenderState srs)
        {
            if (_elementList.Count == 0)
                return;

            _RenderDiffuse(srs, srs.Gfx.Device);
        }



        /// <summary>
        /// Renders translucent render instances and additional passes that require blending with
        /// the first pass (fog, for instance).
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        public virtual void RenderTranslucentPass(SceneRenderState srs)
        {
            if (_elementList.Count == 0)
                return;

            _RenderFog(srs, srs.Gfx.Device);
        }



        /// <summary>
        /// Clears the list of render instances added to the manager.
        /// </summary>
        public virtual void Clear()
        {
            _elementList.Clear();
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Gets a group of render instances that all share the same material. These can be rendered together without
        /// performing several render state changes. The instances are stored in _currentInstances.
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        /// <param name="i">The index to start looking for instances. This is update to the index after the last
        /// instance that was added.</param>
        protected virtual void _GetRenderGroup(SceneRenderState srs, ref int i)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_getRenderGroupProfileBlock);
#endif

            // Find a group of render instances that we can render all at once, if possible.
            // Current scheme for finding such groups is pretty simple: any group of instances that share the same
            // material and are in world space will work.  That could break down at a later date or be expanded to
            // be more general, but for now this works well.
            RenderMaterial startMat = null;
            MaterialInstanceData startMatData = null;
            bool gotone = false;

            RenderInstance ri;
            RenderMaterial currentMat;
            MaterialInstanceData currentMatData;

            for (; i < _elementList.Count; i++)
            {
                ri = _elementList[i];
                currentMat = ri.Material;
                currentMatData = ri.MaterialInstanceData;

                // This is the check to group this instance with the preceding instances.
                if (gotone && (currentMat != startMat || currentMatData != startMatData))
                {
                    // don't group this one with the last one(s)
                    i--;

#if DEBUG
                    Profiler.Instance.EndBlock(_getRenderGroupProfileBlock);
#endif

                    return;
                }

                if (_isFogPass)
                {
                    if (!(_fogMaterial as IFogMaterial).IsObjectFogged(srs.CameraPosition, ri.WorldBox))
                        continue;
                }
                else if (_fogMaterial != null)
                {
                    if ((_fogMaterial as IFogMaterial).IsObjectObscured(srs.CameraPosition, ri.WorldBox))
                        continue;
                }

                _currentInstances.Add(ri);
                startMat = currentMat;
                startMatData = currentMatData;
                gotone = true;
            }

#if DEBUG
            Profiler.Instance.EndBlock(_getRenderGroupProfileBlock);
#endif
        }



        /// <summary>
        /// Renders the render instances as normal. This is done by grabbing each group, and calling _RenderGroup on it.
        /// </summary>
        /// <param name="srs"></param>
        /// <param name="d3d"></param>
        protected virtual void _RenderDiffuse(SceneRenderState srs, GraphicsDevice d3d)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_renderDiffuseProfileBlock);
#endif

            RenderInstance ri;
            RenderMaterial material;
            MaterialInstanceData materialData;

            for (int i = 0; i < _elementList.Count; i++)
            {
                _GetRenderGroup(srs, ref i);

                if (_currentInstances.Count != 0)
                {
                    ri = _currentInstances[0];
                    material = ri.Material;
                    materialData = _currentInstances[0].MaterialInstanceData;

                    _RenderGroup(_currentInstances, material, materialData, srs, d3d);
                }

                _currentInstances.Clear();
            }

#if DEBUG
            Profiler.Instance.EndBlock(_renderDiffuseProfileBlock);
#endif
        }



        /// <summary>
        /// Renders all the render instances, forcing the material they are rendered with to the FogMaterial set on the scenegraph.
        /// </summary>
        /// <param name="srs"></param>
        /// <param name="d3d"></param>
        protected virtual void _RenderFog(SceneRenderState srs, GraphicsDevice d3d)
        {
            if (srs.SceneGraph == null)
                return;

            _fogMaterial = srs.SceneGraph.FogMaterial;

            if (_fogMaterial == null)
                return;

#if DEBUG
            Profiler.Instance.StartBlock(_renderFogProfileBlock);
#endif

            _isFogPass = true;

            if (_elementList.Count != 0)
                _RenderGroup(_elementList, _fogMaterial, null, srs, d3d);

            _isFogPass = false;

#if DEBUG
            Profiler.Instance.EndBlock(_renderFogProfileBlock);
#endif
        }



        /// <summary>
        /// Renders a group of instances with a common material. Usually this group is obtained by calling _GetRenderGroup but
        /// that isn't strictly necessary.
        /// </summary>
        /// <param name="renderInstances">The group of render instances to render.</param>
        /// <param name="material">The material to render with.</param>
        /// <param name="materialData">Additional material data to render with.</param>
        /// <param name="srs">The scene render state.</param>
        /// <param name="d3d">The graphics device.</param>
        protected virtual void _RenderGroup(List<RenderInstance> renderInstances, RenderMaterial material, MaterialInstanceData materialData, SceneRenderState srs, GraphicsDevice d3d)
        {
            if (srs.IsReflectPass && (material is IReflectionMaterial))
                return;

#if DEBUG
            Profiler.Instance.StartBlock(_renderGroupProfileBlock);
#endif

            material.SetupEffect(srs, materialData);

            VertexBuffer lastVB = null;
            RenderInstance lastRI = null;
            int instanceCount = renderInstances.Count;
            RenderInstance ri;
            float radius;

            while (material.SetupPass())
            {
                for (int i = 0; i < instanceCount; i++)
                {
                    ri = renderInstances[i];

                    if (ri.PrimitiveCount == 0)
                        continue;

                    if (srs.HasReflections)
                    {
                        GarageGames.Torque.MathUtil.Box3F box = ri.WorldBox;
                        radius = 0.5f * (box.Max - box.Min).Length();

                        if (!srs.Frustum.Intersects(box, Matrix.Identity, radius))
                            continue;
                    }

                    material.SetupObject(ri, srs);

                    // only change the vertex buffer if it is different from the previous one
                    if (lastVB != ri.VertexBuffer)
                    {
                        d3d.Vertices[0].SetSource(ri.VertexBuffer, 0, ri.VertexSize);
                        d3d.Indices = ri.IndexBuffer;
                        d3d.VertexDeclaration = ri.VertexDeclaration;
                        lastVB = ri.VertexBuffer;
                    }

                    if (ri.IndexBuffer == null)
                        d3d.DrawPrimitives(ri.PrimitiveType, ri.BaseVertex, ri.PrimitiveCount);
                    else
                        d3d.DrawIndexedPrimitives(ri.PrimitiveType, ri.BaseVertex, 0, ri.VertexCount, ri.StartIndex, ri.PrimitiveCount);

                    // store the last render instance so it can be used for checking parameter differences
                    lastRI = ri;
                }
            }

            material.CleanupEffect();

#if DEBUG
            Profiler.Instance.EndBlock(_renderGroupProfileBlock);
#endif
        }



        /// <summary>
        /// Renders a single render instance.
        /// </summary>
        /// <param name="ri">The render instance to render.</param>
        /// <param name="material">The material to render with.</param>
        /// <param name="materialData">Additional material data to render with.</param>
        /// <param name="srs">The scene render state.</param>
        /// <param name="d3d">The graphics device.</param>
        protected virtual void _RenderObject(RenderInstance ri, RenderMaterial material, MaterialInstanceData materialData, SceneRenderState srs, GraphicsDevice d3d)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_renderObjectProfileBlock);
#endif

            material.SetupEffect(srs, materialData);

            while (material.SetupPass())
            {
                material.SetupObject(ri, srs);

                d3d.Vertices[0].SetSource(ri.VertexBuffer, 0, ri.VertexSize);
                d3d.Indices = ri.IndexBuffer;
                d3d.VertexDeclaration = ri.VertexDeclaration;

                if (ri.IndexBuffer == null)
                    d3d.DrawPrimitives(ri.PrimitiveType, ri.BaseVertex, ri.PrimitiveCount);
                else
                    d3d.DrawIndexedPrimitives(ri.PrimitiveType, ri.BaseVertex, 0, ri.VertexCount, ri.StartIndex, ri.PrimitiveCount);
            }

            material.CleanupEffect();

#if DEBUG
            Profiler.Instance.EndBlock(_renderObjectProfileBlock);
#endif
        }

        #endregion


        #region Private, protected, internal fields

        protected List<RenderInstance> _elementList;
        protected Comparer<RenderInstance> _comparer;

        protected bool _isFogPass;
        protected RenderMaterial _fogMaterial;
        protected RenderMaterial _zPassMaterial = new ZPassMaterial();

        static protected List<RenderInstance> _currentInstances = new List<RenderInstance>();

#if DEBUG
        ProfilerCodeBlock _getRenderGroupProfileBlock = new ProfilerCodeBlock("BaseRenderManager._GetRenderGroup");
        ProfilerCodeBlock _renderDiffuseProfileBlock = new ProfilerCodeBlock("BaseRenderManager._RenderDiffuse");
        ProfilerCodeBlock _renderFogProfileBlock = new ProfilerCodeBlock("BaseRenderManager._RenderFog");
        ProfilerCodeBlock _renderGroupProfileBlock = new ProfilerCodeBlock("BaseRenderManager._RenderGroup");
        ProfilerCodeBlock _renderObjectProfileBlock = new ProfilerCodeBlock("BaseRenderManager._RenderObject");
#endif

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_elementList != null)
                _elementList.Clear();
            _elementList = null;
            _comparer = null;
            _fogMaterial = null;
#if DEBUG
            _getRenderGroupProfileBlock = null;
            _renderDiffuseProfileBlock = null;
            _renderFogProfileBlock = null;
            _renderGroupProfileBlock = null;
            _renderObjectProfileBlock = null;
#endif
            _zPassMaterial = null;
        }

        #endregion
    }
}
