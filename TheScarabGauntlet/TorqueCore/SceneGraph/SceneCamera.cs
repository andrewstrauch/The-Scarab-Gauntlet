//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Sim;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.SceneGraph
{
    /// <summary>
    /// Interface for a camera used by ISceneGraph.
    /// </summary>
    public interface ISceneCamera
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scenegraph associated with this camera.
        /// </summary>
        BaseSceneGraph SceneGraph
        {
            get;
        }

        /// <summary>
        /// 3D transform of camera.
        /// </summary>
        Matrix Transform
        {
            get;
        }

        /// <summary>
        /// The world view index of the camera that will be assigned to the scene render state.
        /// </summary>
        int WorldViewIndex
        {
            get;
        }

        /// <summary>
        /// The field of view of the camera.
        /// </summary>
        float FOV
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum distance from the camera at which objects will be rendered.
        /// </summary>
        float FarDistance
        {
            get;
        }

        #endregion
    }
}
