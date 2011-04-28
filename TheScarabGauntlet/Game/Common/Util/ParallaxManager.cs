using System;
using System.Collections.Generic;
using GarageGames.Torque.T2D;
using Microsoft.Xna.Framework;

namespace PlatformerStarter.Common
{
    public struct ParallaxObject
    {
        public T2DSceneObject ObjectPtr;
        public float Magnitude;
    }

    public class ParallaxManager
    {
        #region Private Members
        private List<ParallaxObject> parallaxObjects;
        private static ParallaxManager instance;
        private T2DSceneObject target;
        private Vector2 oldTargetPos;
        #endregion

        #region Public Properties
        
        public static ParallaxManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ParallaxManager();

                return instance;
            }
        }

        public T2DSceneObject Target
        {
            set 
            { 
                target = value;
                oldTargetPos = target.Position;
            }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Updates all parallax objects associated with the parallax manager.
        /// </summary>
        public void Update()
        {
            if (target != null)
            {
                Vector2 currDirection = target.Position - oldTargetPos;

                if(currDirection != Vector2.Zero)
                {
                    //currDirection.Normalize();

                    for (int i = 0; i < parallaxObjects.Count; ++i)
                    {
                        parallaxObjects[i].ObjectPtr.Position = parallaxObjects[i].ObjectPtr.Position
                            + (-currDirection * parallaxObjects[i].Magnitude);
                    }
                }

                oldTargetPos = target.Position;
            }
        }

        /// <summary>
        /// Adds the object and an associated scrolling magnitude the pool
        /// of parallax objects to update.
        /// </summary>
        /// <param name="obj">The scene object to include in parallax scrolling.</param>
        /// <param name="magnitude">The scrolling magnitude of the object.</param>
        public void AddParallaxObj(T2DSceneObject obj, float magnitude)
        {
            ParallaxObject pObj;

            pObj.ObjectPtr = obj;
            pObj.Magnitude = magnitude;

            parallaxObjects.Add(pObj);
        }
        #endregion

        #region Private Routines

        private ParallaxManager()
        {
            parallaxObjects = new List<ParallaxObject>();
        }

        #endregion
    }
}
