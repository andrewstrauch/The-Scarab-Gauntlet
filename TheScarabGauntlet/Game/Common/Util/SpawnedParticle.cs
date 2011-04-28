using System;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;

namespace PlatformerStarter.Common.Util
{
    public class SpawnedParticle
    {
        #region Private Members
        private T2DParticleEffect effectTemplate;
        private Vector2 spawnOffset;
        private Vector2 position;
        #endregion

        #region Public Properties

        public T2DParticleEffect Effect
        {
            get { return effectTemplate; }
            set { effectTemplate = value; }
        }

        public Vector2 Offset
        {
            get { return spawnOffset; }
            set { spawnOffset = value; }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Spawns and plays the particle at the desired position.
        /// </summary>
        /// <param name="position">The position to spawn the particle at.</param>
        public void Spawn(Vector2 position)
        {
            if (effectTemplate != null)
            {
                T2DParticleEffect effect = effectTemplate.Clone() as T2DParticleEffect;
                effect.Position = position + spawnOffset;
                TorqueObjectDatabase.Instance.Register(effect);
                effect.PlayEffect(true);
            }
        }
        
        #endregion
    }
}
