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

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A component to be added to a scene object. The trigger will deal a specific amount of damage to any Actors that 
    /// enter the scene object's boundaries. For instantly killing Actors, use a KillTriggerComponent, rathr than a
    /// HazardComponent.
    /// </summary>
    [TorqueXmlSchemaType]
    public class HazardComponent : DirectionalTriggerComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The amount of damage to attempt to deal to Actors that enter the trigger.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "100")]
        public float Damage
        {
            get { return _damage; }
            set { _damage = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            HazardComponent obj2 = obj as HazardComponent;

            obj2.Damage = Damage;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            if (actor == null)
                return;

            if (_confirmDamage(ourObject, theirObject, actor))
                actor.TakeDamage(_damage, ourObject);
        }

        /// <summary>
        /// _confirmDamage callback. Optionally override this in a child class to only deal damage to specific actors. Returns true by default
        /// (i.e. deals damage to any Actor).
        /// </summary>
        /// <param name="ourObject">The scene object this HazardComponent is on.</param>
        /// <param name="theirObject">The scene object the ActorComponent is on.</param>
        /// <param name="actor">The ActorComponent on the scene object that entered the trigger.</param>
        /// <returns>True if the HazardComponent should deal damage to the Actor.</returns>
        protected virtual bool _confirmDamage(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            // this should be overridden by derived classes
            // a return value of false will result in the damage not being applied
            // a return value of true will result in the damage being applied
            return true;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            SceneObject.SetObjectType(PlatformerData.DamageTriggerObjecType, true);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        /// <summary>
        /// The amount of damage to attempt to deal to Actors that enter the trigger.
        /// </summary>
        protected float _damage;

        #endregion
    }
}
