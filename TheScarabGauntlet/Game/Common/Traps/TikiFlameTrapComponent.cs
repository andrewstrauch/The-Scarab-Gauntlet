using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using PlatformerStarter.Common.Util;

namespace PlatformerStarter.Common.Traps
{
    public class TimedWeapon
    {
        #region Private Members
        private string name;
        private WeaponComponent weapon;
        private TimeSpan coolDown;
        private TimeSpan startTime;
        #endregion

        #region Public Properties
        
        /// <summary>
        /// The name associated with the weapon.
        /// </summary>
        public string WeaponName
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The time to wait between shots.
        /// </summary>
        public int CoolDown
        {
            get { return coolDown.Milliseconds; }
            set { coolDown = TimeSpan.FromMilliseconds(value); }
        }

        /// <summary>
        /// The time to start the first shot.
        /// </summary>
        public int StartTime
        {
            get { return startTime.Milliseconds; }
            set { startTime = TimeSpan.FromMilliseconds(value); }
        }
        #endregion

        #region Public Routines
        /// <summary>
        /// Initializes all internal members.
        /// </summary>
        public void Initialize()
        {
            T2DSceneObject weaponObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>(name);

            if (weaponObj != null)
                weapon = weaponObj.Components.FindComponent<WeaponComponent>();
        }

        /// <summary>
        /// Updates the weapon and fires if the cooldown has expired.
        /// </summary>
        /// <param name="dt">The change in time between update calls.</param>
        public void Update(float dt)
        {
            startTime = startTime.Subtract(TimeSpan.FromSeconds(dt));

            if (startTime.Seconds <= 0)
            {
                weapon.Fire();
                startTime = coolDown;
            }
        }
        #endregion
    }

    [TorqueXmlSchemaType]
    public class TikiFlameTrapComponent : TrapComponent, ITickObject
    {
        #region Private Members
        private bool isActive;
        private List<TimedWeapon> weapons;
        #endregion

        #region Public Properties
        
        /// <summary>
        /// A list of names of the mounted weapons on the totem.  This allows the 
        /// tiki totem to fire each weapon when necessary.
        /// </summary>
        public List<TimedWeapon> MountedWeapons
        {
            get { return weapons; }
            set { weapons = value; }
        }
        
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Makes the tikis shoot flames from the given linkpoint positions.
        /// </summary>
        public override void Activate()
        {
            isActive = true;
        }

        /// <summary>
        /// Runs the update logic per tick of the game time.
        /// </summary>
        /// <param name="move">The exposed movement of the object, if bound.</param>
        /// <param name="dt">The change in time between ticks.</param>
        public void ProcessTick(Move move, float dt)
        {
            if (isActive)
                foreach (TimedWeapon weapon in weapons)
                    weapon.Update(dt);
        }

        /// <summary>
        /// Routine to run interpolation logic for component.  Used mainly when the 
        /// game time is too slow.  Note: This is not implemented for this component.
        /// </summary>
        /// <param name="dt">The change in time between ticks.</param>
        public void InterpolateTick(float dt)
        {
            // Move along. Nothing to see here!!
        }

        /// <summary>
        /// Copies the public members of this component to a new cloned object.
        /// </summary>
        /// <param name="obj"></param>
        public override void CopyTo(TorqueComponent obj)
        {
            TikiFlameTrapComponent obj2 = obj as TikiFlameTrapComponent;
            obj2.MountedWeapons = MountedWeapons;
            
            base.CopyTo(obj);
        }

        #endregion

        #region Private Routines

        /// <summary>
        /// Initialize the component upon registration.
        /// </summary>
        /// <param name="owner">The object this component belongs to.</param>
        /// <returns>True if initialization succeeds, false otherwise.</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            foreach (TimedWeapon weapon in weapons)
                weapon.Initialize();
            
            // INSERT LOGGER HERE
            // Check particleTemplate
            // Check linkPoints
            //Assert.Fatal(particleTemplate != null, "A particle template must be assigned to the TikiFlameTrapComponent"); 
            // register for a tick callback
            ProcessList.Instance.AddTickCallback(SceneObject, this);

            isActive = false;

            return true;
        }

        /// <summary>
        /// Free up memory when this component is no longer needed.
        /// </summary>
        protected override void _OnUnregister()
        {
            //particleTemplate.Dispose();
            //linkPoints.Clear();
            if(weapons.Count > 0)
                weapons.Clear();

            base._OnUnregister();
        }

        #endregion
    }
}
