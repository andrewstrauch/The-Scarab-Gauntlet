using System;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.PlatformerFramework;

using PlatformerStarter.Common.Util;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class GoldCrystalCollectible : CollectibleComponent
    {
        #region Private Routines

        protected override bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            base._confirmPickup(ourObject, theirObject, actor);

            PlayerActorComponent player = actor as PlayerActorComponent;

            if (player != null)
            {
                if (ourObject.TestObjectType(PlatformerData.SpawnedObjectType))
                {
                    CheckpointSystemSpawnedObjectComponent spawnedObject = ourObject.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

                    if (spawnedObject != null)
                        spawnedObject.Recover = false;
                }

                player.AddGoldCrystal();
                SoundManager.Instance.PlaySound("sounds", "gold_crystal");

                return true;
            }

            return false;
        }

        #endregion

    }
}
