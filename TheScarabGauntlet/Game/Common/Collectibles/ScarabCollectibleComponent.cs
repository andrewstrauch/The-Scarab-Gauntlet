using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.PlatformerFramework;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GUI;
using PlatformerStarter.Common.Util;
using PlatformerStarter.Common.GUI;

namespace PlatformerStarter.Common.Collectibles
{
    [TorqueXmlSchemaType]
    public class ScarabCollectibleComponent : CollectibleComponent
    {
        #region Private Members
        private SpawnedParticle effect;
        #endregion

        #region Public Properties
        
        public SpawnedParticle Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ScarabCollectibleComponent obj2 = obj as ScarabCollectibleComponent;
            obj2.Effect = Effect;
        }
        #endregion

        #region Private Routines

        protected override bool _confirmPickup(T2DSceneObject ourObject, T2DSceneObject theirObject, ActorComponent actor)
        {
            if(actor is PlayerActorComponent)
            {
                if(ourObject.TestObjectType(PlatformerData.SpawnedObjectType))
                {
                    CheckpointSystemSpawnedObjectComponent spawnedComp = ourObject.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

                    if(spawnedComp != null)
                        spawnedComp.Recover = false;
                }
                
                // Play sound effect here!
                SoundManager.Instance.PlaySound("sounds", "checkpoint");
                CheckpointManager.Instance.CheckpointReached();

                // set the new respawn position of the actor
                if (SceneObject != null)
                    actor.RespawnPosition = SceneObject.Position;
                else
                    actor.RespawnPosition = actor.Actor.Position;

                //GUICanvas.Instance.SetContentControl(new Checkpoint_GUI(SceneObject.Position + new Vector2(0, -5)));
                effect.Spawn(SceneObject.Position);
                
                // true = yes, i was picked up. delete me!
                return true;
            }

            // false = no, this guy didn't pick me up.
            return false;
        }
        #endregion
    }
}
