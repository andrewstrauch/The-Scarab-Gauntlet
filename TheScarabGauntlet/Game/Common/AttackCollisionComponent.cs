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
    public class AttackCollisionComponent : TorqueComponent
    {
        protected T2DSceneObject targetObject;
        protected string linkPointName;
        protected TorqueEvent<bool> mountEvent;
        protected int mountFrame;
        protected int dismountFrame;
        protected int damage;
        private bool mounted;
        TorqueObjectType collisionTypes;

        #region Properties
        public T2DSceneObject TargetObject
        {
            get { return targetObject; }
            set { targetObject = value; }
        }
        public string MountLPName
        {
            get { return linkPointName; }
            set { linkPointName = value; }
        }
        public int MountFrame
        {
            get { return mountFrame; }
            set { mountFrame = value; }
        }
        public int DismountFrame
        {
            get { return dismountFrame; }
            set { dismountFrame = value; }
        }
        public bool Mounted
        {
            get { return mounted; }
        }
        [TorqueXmlSchemaType(DefaultValue = "10")]
        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }
        public TorqueObjectType CollisionTypes
        {
            get { return collisionTypes; }
            set { collisionTypes = value; }
        }
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        [TorqueXmlSchemaType]
        public static T2DOnCollisionDelegate PunchCollision
        {
            get { return MeleeCollision; }
        }
        #endregion


        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            
            AttackCollisionComponent obj2 = obj as AttackCollisionComponent;

            obj2.TargetObject = TargetObject;
            obj2.MountFrame = MountFrame;
            obj2.DismountFrame = DismountFrame;
            obj2.MountLPName = MountLPName;
            obj2.Damage = Damage;
        }

        /// <summary>
        /// Mounts the attack object.
        /// </summary>
        public void MountAttack()
        {
            TorqueEventManager.TriggerEvent<bool>(mountEvent, true);
            mounted = true;
        }
        
        /// <summary>
        /// Dismounts the attack object.
        /// </summary>
        public void DismountAttack()
        {
            TorqueEventManager.TriggerEvent<bool>(mountEvent, false);
            mounted = false;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            mountEvent = new TorqueEvent<bool>("mountAction");

            TorqueEventManager.Instance.MgrListenEvents<bool>(mountEvent, ProcessMount, null);

            SetupCollision();
            mounted = false;

            return true;
        }

        protected virtual void SetupCollision()
        {
            // Set the type of the melee object to a damage trigger type so it collides with enemies
            SceneObject.SetObjectType(ExtPlatformerData.MeleeDamageObjectType, true);//PlatformerData.DamageTriggerObjecType, true);//PlatformerData.DamageTriggerObjecType, true);
            //
            // Make sure we tell the melee object what to collide with,  in this case
            // it will only collide with enemies (non-player actors)
            //SceneObject.Collision.CollidesWith += ExtPlatformerData.DamageRegionObjectType;
        }

        protected override void _OnUnregister()
        {
            // todo: perform de-initialization for the component

            base._OnUnregister();
        }

        /// <summary>
        /// Chooses whether to mount or dismount the melee scene object from the given
        /// linkpoint on the player
        /// </summary>
        /// <param name="eventName">The name of the event being triggered</param>
        /// <param name="mountObject">True to mount onto the player, false to dismount from
        ///                           the player.</param>
        protected void ProcessMount(string eventName, bool mountToObject)
        {
            if (SceneObject != null)
            {
                if (mountToObject)
                    SceneObject.Mount(targetObject, linkPointName, false);
                else
                {
                    SceneObject.Dismount();
                    SceneObject.MarkForDelete = true;
                }
            }
        }

        /// <summary>
        /// T2DOnCollision delegate to handle damage between the melee scene object and enemies
        /// </summary>
        /// <param name="myObject">The melee scene object mounted on the player</param>
        /// <param name="theirObject">The enemy scene object</param>
        /// <param name="info">Collision information</param>
        /// <param name="resolve">How the collision will be resolved</param>
        /// <param name="physicsMaterial">The type of material that would affect the physics</param>
        public static void MeleeCollision(T2DSceneObject myObject, T2DSceneObject theirObject,
            T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            //theirObject.Collision.Images[0].
            int damage = myObject.Components.FindComponent<AttackCollisionComponent>().Damage;

            
            if(theirObject.TestObjectType(PlatformerData.ActorObjectType))
            {
                ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

                // Deal damage to the enemy
                if (actor != null)
                    actor.TakeDamage(damage, theirObject, true, true);
            }

            else
            {
                WeakSpotComponent weakSpot = theirObject.Components.FindComponent<WeakSpotComponent>();

                if(weakSpot != null)
                    weakSpot.TakeDamage(damage, true);
            }

            myObject.MarkForDelete = true;
        }
    }
}
