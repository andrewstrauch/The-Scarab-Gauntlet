using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    public class CameraManager: ITickObject
    {
        private T2DSceneCamera camera;
        private T2DSceneObject mountObject;
        private Vector2 mountOffset;
        private Vector2 leashRange;
        private List<CameraTarget> targets;
        private bool mountToObj;
        private static CameraManager instance = null;
        private float speed;

        #region Public Properties
        
        /// <summary>
        /// Singleton instance of the camera manager. 
        /// </summary>
        public static CameraManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CameraManager();

                return instance;
            }
        }

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

        public Vector2 LeashRange
        {
            get { return leashRange; }
            set { leashRange = value; }
        }

        #endregion

        private CameraManager()
        {
            // camera mounting
            camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>();
            
            if (camera != null)
            {
                if (camera.IsMounted)
                    camera.Dismount();

                camera.CameraWorldLimitMin = new Vector2(-1000, -1000);
                camera.CameraWorldLimitMax = new Vector2(1000, 1000);
                leashRange = new Vector2(20.0f, 20.0f);

                ParallaxManager.Instance.ParallaxTarget = camera;
                ProcessList.Instance.AddTickCallback(camera, this);
            }

            mountToObj = false;
            speed = 0.0f;
        }

        public void Mount(T2DSceneObject mountObject)
        {
            this.mountObject = mountObject;
            mountToObj = true;
        }

        public void Mount(T2DSceneObject mountObject, Vector2 offset)
        {
            this.mountObject = mountObject;
            this.mountOffset = offset;
            mountToObj = true;
        }

        public void Mount(Vector2 mountPt)
        {
            camera.Position = mountPt;
            mountToObj = false;
        }

        private void Update(float dt)
        {
            if (mountToObj)
            {
                float camPos = camera.Position.X;

                if (mountObject.Position.X > camera.Position.X + leashRange.X ||
                    mountObject.Position.X < camera.Position.X - leashRange.X)
                {
                    speed = 0.25f;
                    camPos = MathHelper.Lerp(camPos, mountObject.Position.X + mountOffset.X, speed);
                }

                camera.Position = new Vector2(camPos, camera.Position.Y);
            }// : camera.Position;
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
            camera.AnimatePosition((target.Position), transitionTime);
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

        public void ProcessTick(Move move, float dt)
        {
            Update(dt);
        }

        public void InterpolateTick(float dt)
        { }
        
    }
}
