//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

using GarageGames.Torque.PlatformerFramework;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A component to be added to a scene object. When an Actor passes over the scene object, the CollectibleComponent is
    /// notified and given the option to confirm the pickup. Derived Collectibles should be used for any type of pickup.
    /// </summary>
    [TorqueXmlSchemaType]
    public class CollectibleComponent : DirectionalTriggerComponent
    {
        #region Private Members

        private SpawnedParticle effect;

        #endregion


        #region Public Properties

        public SpawnedParticle Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            CollectibleComponent obj2 = obj as CollectibleComponent;

            obj2.Effect = Effect;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            base._onEnter(ourObject, theirObject, info);

            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            if (actor == null || ourObject == null)
                return;

            if (_confirmPickup(ourObject, theirObject, actor))
            {
                ourObject.Visible = false;
                ourObject.CollisionsEnabled = false;

                if (actor.Controller != null)
                    (actor.Controller as ActorController).ActorCollectedItem(actor, this);

                if (ourObject.IsRegistered)
                    TorqueObjectDatabase.Instance.Unregister(ourObject);

                ourObject = null;
            }
        }

        /// <summary>
        /// Decides whether or not the Actor should be allowed to pick this collectible up. Override this
        /// in derived classes. Default always returns true.
        /// </summary>
        /// <param name="ourObject">The scene object this CollectibleComponent is on.</param>
        /// <param name="theirObject">The scene object the ActorComponent is on.</param>
        /// <param name="actor">The ActorComponent that's trying to pick up this collectible.</param>
        /// <returns>True if the Actor should be allowed to pick up the collectible.</returns>
        protected virtual bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            if(theirObject.TestObjectType(PlatformerData.PlayerObjectType))
                if(effect != null) 
                    effect.Spawn(SceneObject.Position);

            // this should be overridden by derived classes
            // a return value of false will result in the collectible remaining in the scene
            // a return value of true will result in the collectible being removed from the scene
            return true;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            SceneObject.SetObjectType(PlatformerData.CollectibleObjectType, true);

            return true;
        }

        #endregion
    }
}
