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
using GarageGames.Torque.Util;
using GarageGames.Torque.T2D;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// Comparer for 2D render instances.
    /// </summary>
    class TranslucentInstance2DComparison : Comparer<RenderInstance>
    {
        #region Public methods

        public override int Compare(RenderInstance x, RenderInstance y)
        {
            return (int)(x.SortPoint.Z - y.SortPoint.Z);
        }

        #endregion
    }

}
