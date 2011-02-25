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
    public class WeakSpotComponent : TorqueComponent
    {
        protected T2DSceneObject targetObject;
        protected string linkPointName;
        protected TorqueEvent<bool> mountEvent;
        protected int mountFrame;
        protected int dismountFrame;
        protected int health;
        protected int armor;

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
        [System.Xml.Serialization.XmlIgnore]
        public TorqueEvent<bool> MountEvent
        {
            get { return mountEvent; }
            set { mountEvent = value; }
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
        public int Health
        {
            get { return health; }
            set { health = value; }
        }
        public int Armor
        {
            get { return armor; }
            set { armor = value; }
        }
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            WeakSpotComponent obj2 = obj as WeakSpotComponent;

            obj2.TargetObject = TargetObject;
            obj2.MountEvent = MountEvent;
            obj2.MountFrame = MountFrame;
            obj2.DismountFrame = DismountFrame;
            obj2.MountLPName = MountLPName;
            obj2.Health = Health;
            obj2.Armor = Armor;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            //mountEvent = new TorqueEvent<bool>("mountAction");

            //TorqueEventManager.Instance.MgrListenEvents<bool>(mountEvent, ProcessMount, null);

            // Set the type of the melee objec to a damage region typ so it collides with damage
            // trigger objects 
            SceneObject.SetObjectType(PlatformerData.EnemyObjectType, true); //ExtPlatformerData.DamageRegionObjectType, true);

            // Make sure we tell the melee object what to collide with, in this case
            // it will only collide with damage trigger objects (melee polygons)
            if(SceneObject.Collision.CollidesWith.Equals(TorqueObjectType.AllObjects))
                SceneObject.Collision.CollidesWith = PlatformerData.DamageTriggerObjecType;

            return true;
        }

        protected override void _OnUnregister()
        {
            // todo: perform de-initialization for the component

            base._OnUnregister();
        }

        public void TakeDamage(int damage, bool ignoreArmor)
        {
            int totalDamage = damage;

            if (!ignoreArmor)
                totalDamage -= armor;

            health -= totalDamage;

            if (health <= 0)
            {
                ActorComponent actor = targetObject.Components.FindComponent<ActorComponent>();

               // if (actor != null)
                 //   actor.Die();

                SceneObject.MarkForDelete = true;
            }
        }

        protected void ProcessMount(string eventName, bool mountObject)
        {
            if (SceneObject != null)
            {
                if (mountObject)
                    SceneObject.Mount(targetObject, linkPointName, false);
                else
                {
                    if (SceneObject.IsMounted)
                    {
                        SceneObject.Dismount();
                        SceneObject.MarkForDelete = true;
                    }
                }
            }
        }
    }
}