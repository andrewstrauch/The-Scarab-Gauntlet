using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.PlatformerFramework;
using GarageGames.Torque.XNA;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class ScarabCollectibleComponent : CollectibleComponent
    {
        #region Private Routines
        protected override bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            if(actor is PlayerActorComponent)
            {
              /*  if(ourObject.TestObjectType(PlatformerData.SpawnedObjectType))
                {
                    CheckpointSystemSpawnedObjectComponent spawnedComp = ourObject.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

      //              if(spawnedComp != null)
        //                spawnedComp.Recover = false;
                }*/
                
                // Play sound effect here!
                //SoundManager.Instance.PlaySound("sound");
                CheckpointManager.Instance.CheckpointReached();

                // set the new respawn position of the actor
                if (SceneObject != null)
                    actor.RespawnPosition = SceneObject.Position;
                else
                    actor.RespawnPosition = actor.Actor.Position;

                // true = yes, i was picked up. delete me!
                return true;
            }

            // false = no, this guy didn't pick me up.
            return false;
        }
        #endregion
    }
}
