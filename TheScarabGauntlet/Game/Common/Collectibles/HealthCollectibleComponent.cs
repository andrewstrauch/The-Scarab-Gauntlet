using System;
using GarageGames.Torque.T2D;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Common.Collectibles
{
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

        #region Private Routines
        protected override bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            if(actor is PlayerActorComponent)
            {
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
