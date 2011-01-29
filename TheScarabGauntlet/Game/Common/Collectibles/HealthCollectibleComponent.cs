using System;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class HealthCollectibleComponent : CollectibleComponent
    {
        #region Private Members
        private int healingValue;
        #endregion

        #region Public Properties
        
        public int HealingValue
        {
            get { return healingValue; }
            set { healingValue = value; }
        }
        
        #endregion

        #region Public Routines
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            HealthCollectibleComponent obj2 = obj as HealthCollectibleComponent;

            obj2.HealingValue = HealingValue;
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
        #endregion
    }
}
