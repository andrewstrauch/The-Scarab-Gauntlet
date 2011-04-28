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

namespace PlatformerStarter.Common.Traps
{
    [TorqueXmlSchemaType]
    public class SwingingTrapComponent : TrapComponent
    {
        #region Private Members
        private BoundedRotationComponent motor;
        #endregion

        #region Public Properties
        
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        public override void  Activate()
        {
            if(motor != null)
                motor.BeginRotation();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            motor = SceneObject.Components.FindComponent<BoundedRotationComponent>();

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }

        #endregion
    }
}
