//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Materials;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// Class which a SceneGraph must attach to be able to render.
    /// </summary>
    public class GUISceneview : GUIControl
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Changes the attached SceneGraph's view to the specified camera.
        /// </summary>
        public ISceneCamera Camera
        {
            get { return _sceneCamera; }
            set { _sceneCamera = value; }
        }



        /// <summary>
        /// Optionally provide a mask in which none but those TorqueObjectTypes
        /// passing the mask will render.
        /// </summary>
        public TorqueObjectType RenderMask
        {
            get { return _renderMask; }
            set { _renderMask = value; }
        }



        /// <summary>
        /// Optionally provide a mask in which all but those TorqueObjectTypes
        /// passing the mask will render.
        /// </summary>
        public TorqueObjectType NoRenderMask
        {
            get { return _noRenderMask; }
            set { _noRenderMask = value; }
        }



        public PostProcessor PostProcessor
        {
            get { return _postProcessor; }
            set
            {
                if (!TorqueEngineComponent.Instance.EnableBackBufferEffects)
                {
                    TorqueConsole.Warn("\nGUISceneview.PostProcessor_set - Back buffer effects are disabled.");
                    return;
                }

                if (_postProcessor != null)
                    _DestroyPostProcessing();

                _postProcessor = value;
            }
        }

        #endregion


        #region Public methods

        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
            if (_sceneCamera == null)
                _sceneCamera = TorqueObjectDatabase.Instance.FindObject<ISceneCamera>();

            // Stop rendering if we still don't have a camera or the guide is open.
            if (_sceneCamera == null ||
                    _sceneCamera.SceneGraph == null ||
                    !TorqueEngineComponent.Instance.IsActive)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("GUISceneview.OnRender");
#endif

            if (GFXDevice.Instance != null)
            {
                postProcess = _postProcessor != null && TorqueEngineComponent.Instance.EnableBackBufferEffects;

                mainRenderTarget = TorqueEngineComponent.Instance.CurrentRenderTarget;

                if (postProcess)
                {
                    if (_renderTarget == null || _renderTarget.IsDisposed)
                        _InitPostProcessing();

                    Assert.Fatal(_renderTarget != null && !_renderTarget.IsDisposed, "GUISceneView.OnRender - Invalid render target!");
                    TorqueEngineComponent.Instance.CurrentRenderTarget = _renderTarget;
                    GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget);

                    TorqueEngineComponent.Instance.ClearRenderTarget();
                }

                // make sure the scenegraph knows it's rendering from this camera
                _sceneCamera.SceneGraph.Camera = _sceneCamera;
                _sceneCamera.SceneGraph.PreRender(GFXDevice.Instance, _renderMask, _noRenderMask, updateRect.Width / updateRect.Height);
                _sceneCamera.SceneGraph.Render(GFXDevice.Instance, _renderMask, _noRenderMask);

                if (postProcess)
                {
                    GFXDevice.Instance.Device.SetRenderTarget(0, mainRenderTarget);
                    TorqueEngineComponent.Instance.CurrentRenderTarget = mainRenderTarget;
                    texture = _renderTarget.GetTexture();
                    _postProcessor.Run(texture, Position, Size);
                }
            }

            _RenderChildControls(offset, updateRect);

#if DEBUG
            Profiler.Instance.EndBlock("GUISceneview.OnRender");
#endif
        }



        public override void SetBounds(Microsoft.Xna.Framework.Vector2 newPosition, Vector2 newSize)
        {
            base.SetBounds(newPosition, newSize);

            if (_postProcessor != null)
                _InitPostProcessing();
        }



        public override void OnUnregister()
        {
            _DestroyPostProcessing();
            base.OnUnregister();
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUISceneview obj2 = (GUISceneview)obj;

            obj2.Camera = Camera;
            obj2.RenderMask = RenderMask;
            obj2.NoRenderMask = NoRenderMask;
            obj2.PostProcessor = PostProcessor;
        }

        #endregion


        #region Private, protected, internal fields

        protected void _InitPostProcessing()
        {
            if (_renderTarget != null)
                _DestroyPostProcessing();

            _renderTarget = new RenderTarget2D(GFXDevice.Instance.Device, (int)Size.X, (int)Size.Y, 1, SurfaceFormat.Color, RenderTargetUsage.DiscardContents);
            _postProcessor.Setup((int)Size.X, (int)Size.Y);
        }



        protected void _DestroyPostProcessing()
        {
            if (_renderTarget == null)
                return;

            _renderTarget.Dispose();
            _renderTarget = null;
            _postProcessor.Cleanup();
        }

        #endregion


        #region Private, protected, internal fields

        ISceneCamera _sceneCamera = null;

        TorqueObjectType _renderMask = TorqueObjectType.AllObjects;
        TorqueObjectType _noRenderMask;

        bool postProcess;
        RenderTarget2D mainRenderTarget;
        Texture2D texture;

        PostProcessor _postProcessor = null;
        RenderTarget2D _renderTarget = null;

        #endregion
    }
}
