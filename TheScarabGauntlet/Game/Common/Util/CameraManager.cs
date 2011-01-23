using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;

namespace PlatformerStarter.Common
{
    public class CameraManager
    {
        private T2DSceneCamera camera;
        private Vector2 mountOffset;

        #region Public Properties
        /// <summary>
        /// True if the camera is transitioning, false otherwise.
        /// </summary>
        public bool IsMoving
        {
            get { return camera.IsMoving; }
        }
        /// <summary>
        /// True if the camera is zooming, false otherwise.
        /// </summary>
        public bool IsZooming
        {
            get { return camera.IsZooming; }
        }
        #endregion

        public CameraManager(T2DSceneCamera camera)
        {
            this.camera = camera;
        }

        /// <summary>
        /// Transitions the camera to the given target.
        /// </summary>
        /// <param name="target">The object we want the camera to view.</param>
        /// <param name="transitionTime">The amount of time it will take to transition
        ///  to the target object. </param>
        public void MoveToTarget(T2DSceneObject target, float transitionTime)
        {
            if(camera.IsMounted)
                camera.Dismount();
            camera.AnimatePosition((target.Position + mountOffset), transitionTime);
            camera.Mount(target, String.Empty, camera.AnimatePositionTarget, 0.0f, true);
        }

        /// <summary>
        /// Returns the camera back to the player.  Only to be used after all camera 
        /// transitioning is done
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="transitionTime">The amount of time it will to take to transition
        /// to the player</param>
        public void ReturnToPlayer(T2DSceneObject player, float transitionTime)
        {
            camera.AnimatePosition((player.Position + new Vector2(0, -1)), transitionTime);
            camera.Mount(player, String.Empty, new Vector2(0, -1), 0.0f, true);
            camera.UseMountForce = true;
            camera.MountForce = 15;
        }

        /// <summary>
        /// Zooms the camera in/out depending on the given factor.
        /// </summary>
        /// <param name="scale">The scale at which the camera should zoom to.</param>
        /// <param name="transitionTime">The amount of time it will take to transition to the
        /// given zoom scale.</param>
        public void Zoom(float scale, float transitionTime)
        {
            camera.AnimateZoom(scale, transitionTime);
        }

    }
}
