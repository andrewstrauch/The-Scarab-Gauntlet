//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Materials;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// Render manager for 2D objects or 3D objects in a 2D scene. This should only be used for objects in a
    /// T2DSceneGraph. To add objects to this render manager set the render instance type to Mesh2D. To add
    /// 3D objects, set the render instance type to Mesh3D and set the BinOverride on the SceneRenderer to
    /// Mesh2D.
    /// </summary>
    public class T2DRenderManager : BaseRenderManager
    {
        public override void Sort(SceneRenderState srs)
        {
            if (_elementList.Count == 0 || srs.IsReflectPass)
                return;

            T2DSceneGraph sceneGraph = srs.SceneGraph as T2DSceneGraph;
            Assert.Fatal(sceneGraph != null, "T2DRenderManager.Sort - 2D elements should not be added to a 3D scenegraph!");

            // The base sort sorts by material for batching, which will only work if the depth buffer is enabled.
            if (sceneGraph.UseDepthBuffer)
                base.Sort(srs);
        }



        public override void RenderOpaquePass(SceneRenderState srs)
        {
            if (_elementList.Count == 0 || srs.IsReflectPass)
                return;

            T2DSceneGraph sceneGraph = srs.SceneGraph as T2DSceneGraph;
            Assert.Fatal(sceneGraph != null, "T2DRenderManager.RenderOpaquePass - 2D elements should not be added to a 3D scenegraph!");

            // always use alpha blending
            srs.Gfx.Device.RenderState.AlphaBlendEnable = true;

            // if the depth buffer isn't being used, disable it
            if (!sceneGraph.UseDepthBuffer)
                srs.Gfx.Device.RenderState.DepthBufferEnable = false;

            // The depth buffer is enabled, but not in a z pass, so we need alpha testing to keep transparent
            // parts of textures out of the z buffer.
            else if (!sceneGraph.DoZPass)
            {
                srs.Gfx.Device.RenderState.AlphaTestEnable = true;
                srs.Gfx.Device.RenderState.ReferenceAlpha = 255;
            }

            // render objects as normal
            _RenderDiffuse(srs, srs.Gfx.Device);

            // in case a 3D object was the last thing rendered
            if (_rendering3D)
                _Cleanup3DState(srs, srs.Gfx.Device);

            // cleanup render states
            if (!sceneGraph.UseDepthBuffer)
                srs.Gfx.Device.RenderState.DepthBufferEnable = true;
            else if (!sceneGraph.DoZPass)
                srs.Gfx.Device.RenderState.AlphaTestEnable = false;

            srs.Gfx.Device.RenderState.AlphaBlendEnable = false;
        }



        public override void RenderTranslucentPass(SceneRenderState srs)
        {
        }



        protected override void _RenderGroup(List<RenderInstance> renderInstances, RenderMaterial material, MaterialInstanceData materialData, SceneRenderState srs, GraphicsDevice d3d)
        {
            // If the first instance is a 3D shape then make sure the
            // state is set for rendering them.
            if (renderInstances[0].Type == RenderInstance.RenderInstanceType.Mesh3D)
            {
                if (!_rendering3D)
                    _Setup3DState(srs, d3d);
            }
            else
            {
                // Restore the 2D rendering state.
                if (_rendering3D)
                    _Cleanup3DState(srs, d3d);
            }

            // Render as normal.
            base._RenderGroup(renderInstances, material, materialData, srs, d3d);
        }



        /// <summary>
        /// Sets up the render state for rendering 3D objects in the 2D scene.
        /// </summary>
        /// <param name="d3d">The current graphics device.</param>
        protected virtual void _Setup3DState(SceneRenderState srs, GraphicsDevice d3d)
        {
            _rendering3D = true;

            d3d.RenderState.DepthBufferEnable = true;
            d3d.RenderState.DepthBufferWriteEnable = true;

            T2DSceneCamera camera = srs.SceneGraph.Camera as T2DSceneCamera;

            // We need a better zrange than the normal 2d projection
            // matrix provides... so store it and apply a new one.
            _2dProjection = srs.Projection;
            srs.Projection = GFXDevice.Instance.SetOrtho(false, -0.5f * camera.Extent.X, 0.5f * camera.Extent.X,
                                                         0.5f * camera.Extent.Y, -0.5f * camera.Extent.Y, -1000.0f, 1000.0f);

            TorqueEngineComponent.Instance.ClearRenderTarget(true);
        }



        /// <summary>
        /// Cleans up the render state after rendering 3D objects in the 2D scene.
        /// </summary>
        /// <param name="d3d">The current graphics device.</param>
        protected virtual void _Cleanup3DState(SceneRenderState srs, GraphicsDevice d3d)
        {
            d3d.RenderState.DepthBufferEnable = false;
            d3d.RenderState.DepthBufferWriteEnable = false;

            // Restore the previous 2d projection matrix.
            srs.Projection = _2dProjection;

            _rendering3D = false;
        }



        protected bool _rendering3D;
        protected Matrix _2dProjection;
    }
}
