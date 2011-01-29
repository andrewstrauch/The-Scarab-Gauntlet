using System;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class HealthCollectibleComponent : CollectibleComponent, ITickObject
    {
        #region Private Members
        private int healingValue;
        private FigureEight movement;
        #endregion

        #region Public Properties
        
        public int HealingValue
        {
            get { return healingValue; }
            set { healingValue = value; }
        }

        public FigureEight Movement
        {
            get { return movement; }
            set { movement = value; }
        }
        
        #endregion

        #region Public Routines

        /// <summary>
        /// Runs the update logic per tick of the game time.
        /// </summary>
        /// <param name="move">The exposed movement of the object, if bound.</param>
        /// <param name="dt">The change in time between ticks.</param>
        public void ProcessTick(Move move, float dt)
        {
            movement.Update(dt);

            SceneObject.Position = movement.Position;
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

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            HealthCollectibleComponent obj2 = obj as HealthCollectibleComponent;

            obj2.HealingValue = HealingValue;
            obj2.Movement = Movement;
        }

        #endregion
        
        #region Private Routines
        
        protected override bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            if(actor is PlayerActorComponent)
            {
                if (ourObject.TestObjectType(PlatformerData.SpawnedObjectType))
                {
                    CheckpointSystemSpawnedObjectComponent spawnedObject = ourObject.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

                    if (spawnedObject != null)
                        spawnedObject.Recover = false;
                }

                actor.HealDamage(healingValue, ourObject);
               
                // true = yes, i was picked up. delete me!
                return true;
            }

            // false = no, this guy didn't pick me up.
            return false;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            base._OnRegister(owner);
            
            ProcessList.Instance.AddTickCallback(SceneObject, this);
            movement.StartingPosition = SceneObject.Position;
            movement.Initialize();

            return true;
        }
        
        #endregion
    }
}
