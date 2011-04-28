using System;
using PlatformerStarter.Common.Traps;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Common.Triggers
{
    [TorqueXmlSchemaType]
    public class TrapTriggerComponent : DirectionalTriggerComponent
    {
        #region Private Members
        private T2DSceneObject trapObject;
        #endregion

        #region Public Properties

        public T2DSceneObject TrapObject
        {
            get { return trapObject; }
            set { trapObject = value; }
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
        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            SceneObject.SetObjectType(PlatformerData.ActorTriggerObjectType, true);

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            TrapComponent trapComponent = trapObject.Components.FindComponent<TrapComponent>();

            trapComponent.Activate();
            
            ourObject.CollisionsEnabled = false;
            ourObject.MarkForDelete = true;
        }

        #endregion
    }
}
