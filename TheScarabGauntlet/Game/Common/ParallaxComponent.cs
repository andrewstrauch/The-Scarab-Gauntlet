using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

namespace PlatformerStarter.Common
{
    [TorqueXmlSchemaType]
    public class ParallaxComponent : TorqueComponent
    {
        #region Private Members
        private float magnitude;
        #endregion

        #region Public Properties

        public float ScrollMagnitude
        {
            get { return magnitude; }
            set { magnitude = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
           
            ParallaxComponent obj2 = obj as ParallaxComponent;
            obj2.ScrollMagnitude = ScrollMagnitude;        
        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            ParallaxManager.Instance.AddParallaxObj(SceneObject, magnitude);

            return true;
        }

        protected override void _OnUnregister()
        {
            // todo: perform de-initialization for the component

            base._OnUnregister();
        }
        #endregion
    }
}
