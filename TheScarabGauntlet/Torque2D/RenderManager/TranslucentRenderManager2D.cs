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
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// Render manager for translucent objects. Objects with RenderInstanceType Translucent are
    /// added to this manager. It renders all objects in back to front order so translucency is
    /// properly represented.
    /// </summary>
    public class TranslucentRenderManager2D : BaseRenderManager
    {
        #region Constructors

        public TranslucentRenderManager2D()
        {
            _elementList = new List<RenderInstance>();
            _2DComparer = new TranslucentInstance2DComparison();
        }

        #endregion


        #region Public methods

        public override void Sort(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_sortProfileBlock);
#endif

            if (srs.SceneGraph is T2DSceneGraph)
                _elementList.Sort(_2DComparer);

#if DEBUG
            Profiler.Instance.EndBlock(_sortProfileBlock);
#endif
        }



        public override void RenderZPass(SceneRenderState srs)
        {
        }



        public override void RenderOpaquePass(SceneRenderState srs)
        {
        }



        public override void RenderTranslucentPass(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock(_renderProfileBlock);
#endif

            base.RenderOpaquePass(srs);
            base.RenderTranslucentPass(srs);

#if DEBUG
            Profiler.Instance.EndBlock(_renderProfileBlock);
#endif
        }

        #endregion


        #region Private, protected, internal fields

        Comparer<RenderInstance> _2DComparer;

#if DEBUG
        ProfilerCodeBlock _sortProfileBlock = new ProfilerCodeBlock("TranslucentRenderManager.Sort");
        ProfilerCodeBlock _renderProfileBlock = new ProfilerCodeBlock("TranslucentRenderManager.RenderTranslucentPass");
#endif

        #endregion
    }
}
