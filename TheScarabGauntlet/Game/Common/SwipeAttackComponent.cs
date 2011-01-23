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

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class SwipeAttackComponent : TorqueComponent
    {
        #region Private members
        private T2DSceneObject swipeObject;
        private string linkPointName;
        private BoundedRotationComponent rotationComponent;
        private Vector2 mountOffset;
        private int damage;
        #endregion

        #region Properties
        public T2DSceneObject SwipeObject
        {
            get { return swipeObject; }
            set { swipeObject = value; }
        }

        public string LinkPointName
        {
            get { return linkPointName; }
            set { linkPointName = value; }
        }

        public Vector2 MountOffset
        {
            get { return mountOffset; }
            set { mountOffset = value; }
        }

        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        [TorqueXmlSchemaType]
        public static T2DOnCollisionDelegate SwipeCollision
        {
            get { return MeleeCollision; }
        }
        #endregion

        #region Public routines
        public void StartAttack()
        {
            swipeObject.CollisionsEnabled = true;
            rotationComponent.BeginRotation();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
        }

        /// <summary>
        /// T2DOnCollision delegate to handle damage between the swipe object and player/enemies
        /// </summary>
        /// <param name="myObject">The melee scene object mounted on the player</param>
        /// <param name="theirObject">The enemy scene object</param>
        /// <param name="info">Collision information</param>
        /// <param name="resolve">How the collision will be resolved</param>
        /// <param name="physicsMaterial">The type of material that would affect the physics</param>
        public static void MeleeCollision(T2DSceneObject myObject, T2DSceneObject theirObject,
            T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            int damage = myObject.MountedTo.Components.FindComponent<SwipeAttackComponent>().Damage;

            if (theirObject.TestObjectType(PlatformerData.ActorObjectType))
            {
                PlayerActorComponent actor = theirObject.Components.FindComponent<PlayerActorComponent>();

                // Deal damage to the enemy
                if (actor != null)
                    actor.TakeDamage(damage, myObject.MountedTo, true, true);
            }

            myObject.CollisionsEnabled = false;
        }
        #endregion

        #region Private routines
        private void TurnOffCollision(string eventName, T2DSceneObject mountObject)
        {
            swipeObject.CollisionsEnabled = false;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            rotationComponent = SceneObject.Components.FindComponent<BoundedRotationComponent>();
            rotationComponent.OnRotationFinished = TurnOffCollision;
            //if(!swipeObject.IsRegistered)
              //  TorqueObjectDatabase.Instance.Register(swipeObject);
            
            swipeObject.SetObjectType(ExtPlatformerData.EnemyDamageObjectType, true);
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
