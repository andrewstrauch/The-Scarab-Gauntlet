using System;
using System.Collections.Generic;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;

namespace PlatformerStarter.Common.Util
{
    public class MountedParticle
    {
        #region Private Members
        private string linkPoint;
        private T2DParticleEffect particleTemplate;
        private T2DParticleEffect mountedParticle;
        #endregion

        #region Public Properties
        public string LinkPoint
        {
            get { return linkPoint; }
            set { linkPoint = value; }
        }

        public T2DParticleEffect ParticleTemplate
        {
            get { return particleTemplate; }
            set { particleTemplate = value; }
        }
        #endregion

        #region Public Routines
        /// <summary>
        /// Mounts the particle to the given scene object.
        /// </summary>
        /// <param name="mounter">The object to mount the particle to.</param>
        public void MountTo(T2DSceneObject mounter)
        {
            mountedParticle = particleTemplate.Clone() as T2DParticleEffect;

            if (mountedParticle != null)
            {
                TorqueObjectDatabase.Instance.Register(mountedParticle);
                mountedParticle.Mount(mounter, linkPoint, true);
            }
        }

        /// <summary>
        /// Plays the currently mounted particle.
        /// </summary>
        /// <param name="clearParticles">The option to clear the particles as the effect is playing.</param>
        public void Play(bool clearParticles)
        {
            mountedParticle.PlayEffect(clearParticles);
        }
        #endregion
    }
}
