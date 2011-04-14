using System;

using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

using PlatformerStarter.Common.Traps;
using PlatformerStarter.Enemies.ActorComponents.Level1;

namespace PlatformerStarter.Common.Triggers
{
    class BairneBoundaryTrigger : DirectionalTriggerComponent
    {
        #region Private Members
        private bool onLeft;
        #endregion

        #region Public Properties

        public bool OnLeft
        {
            get { return onLeft; }
            set { onLeft = value; }
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

            SceneObject.SetObjectType(PlatformerData.EnemyTriggerObjectType, true);

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            BairneActorComponent actor = theirObject.Components.FindComponent<BairneActorComponent>();

            if (actor != null)
            {
                actor.OnLeft = onLeft;
                actor.ReachedBound = true;
                actor.HorizontalStop();
            }
            else
                ;//log error
        }

        #endregion
    }
}
