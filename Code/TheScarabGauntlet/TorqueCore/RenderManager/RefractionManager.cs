//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// Manages rendering materials that refract the scene beneath them. In 3D this works pretty much as expected, but
    /// in 2D there are definitely some caveats. By default a 2D scene is not rendered with a depth buffer, so, because
    /// the refraction manager is rendered last, everything in the scene at the same location as the refraction object
    /// will be refracted. You can turn on depth buffering for a 2D scene (T2DSceneGraph.UseDepthBuffer), which generally
    /// works, but requires you to be smart about which objects are rendered as type Mesh2D and which are rendered as type
    /// Translucent (in other words, the IsTranslucent flag on RenderMaterial must be set properly). Still, everything in
    /// the translucent render manager will be refracted if it is in the same location as the refracting object because
    /// translucent objects don't render to the depth buffer.
    /// 
    /// Refraction is handled by resolving the back buffer and copying it to a texture. That texture is set on a material
    /// (RefractionMaterial, unless a custom material is created that refracts) before objects are rendered.
    /// </summary>
    public class RefractionManager : BaseRenderManager, IDisposable
    {

        #region Public methods

        protected override void _RenderGroup(List<RenderInstance> renderInstances, RenderMaterial material, MaterialInstanceData materialData, SceneRenderState srs, GraphicsDevice d3d)
        {
            (material as IRefractionMaterial).SetTexture(_refractionTexture);
            base._RenderGroup(renderInstances, material, materialData, srs, d3d);
        }



        public override void RenderZPass(SceneRenderState srs)
        {
        }



        public override void RenderOpaquePass(SceneRenderState srs)
        {
        }



        public override void RenderTranslucentPass(SceneRenderState srs)
        {
            // Make sure we have something to render.
            if (_elementList.Count == 0 ||
                    srs.IsReflectPass ||
                    GFXDevice.Instance.ShaderProfile < ShaderProfile.PS_1_4)
                return;

            // Get the current render target which contains the state
            // of the scene rendered so far.
            RenderTarget2D sceneTarget = GFXDevice.Instance.Device.GetRenderTarget(0) as RenderTarget2D;
            if (sceneTarget == null)
                return;

#if DEBUG
            Profiler.Instance.StartBlock(_renderProfileBlock);
#endif

            // So here we're making a copy of the current state of the
            // active scene render target for use in the refraction effect.

            // Prepare the copy material if we don't have one.
            if (_copyMaterial == null)
            {
                _copyMaterial = new SimpleMaterial();
                _copyMaterial.IsCopyPass = true;
            }

            // Copy the content of the current scene render target to
            // the refraction texture.
            GFXDevice.Instance.Device.SetRenderTarget(0, null);
            _refractionTexture = sceneTarget.GetTexture();

            // Now set back the scene render target.
            //
            // At this point the act of switching render targets has
            // forced the content to be lost.  To fix that we do a
            // copy to get the original content back.
            //
            _copyMaterial.SetTexture(_refractionTexture);
            TorqueEngineComponent.Instance.ReapplyMainRenderTarget();
            SceneRenderer.RenderManager.RenderQuad(_copyMaterial, Vector2.Zero, new Vector2(sceneTarget.Width, sceneTarget.Height));

            // Now render all the refraction meshes.
            base.RenderOpaquePass(srs);

#if DEBUG
            Profiler.Instance.EndBlock(_renderProfileBlock);
#endif
        }

        #endregion


        #region Private, protected, internal fields

        Texture2D _refractionTexture;
        SimpleMaterial _copyMaterial;

#if DEBUG
        ProfilerCodeBlock _renderProfileBlock = new ProfilerCodeBlock("RefractionManager.RenderTranslucentPass");
#endif

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            if (_copyMaterial != null)
            {
                _copyMaterial.Dispose();
                _copyMaterial = null;
            }

#if DEBUG
            _renderProfileBlock = null;
#endif
            _refractionTexture = null;
            _zPassMaterial = null;

            base.Dispose();
        }

        #endregion
    }
}
