using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using PlatformerStarter.Common.Util;

namespace PlatformerStarter.Common.Traps
{
    [TorqueXmlSchemaType]
    public class TikiFlameTrapComponent : TrapComponent
    {
        #region Private Members
        private bool clearParticles;
        private List<MountedParticle> mountedParticles;
        #endregion

        #region Public Properties
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool ClearFlames
        {
            get { return clearParticles; }
            set { clearParticles = value; }
        }

        public List<MountedParticle> MountedParticles
        {
            get { return mountedParticles; }
            set { mountedParticles = value; }
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
            foreach (MountedParticle particle in mountedParticles)
            {
                particle.MountTo(SceneObject);
                particle.Play(clearParticles);
            }
        }

        /// <summary>
        /// Copies the public members of this component to a new cloned object.
        /// </summary>
        /// <param name="obj"></param>
        public override void CopyTo(TorqueComponent obj)
        {
            TikiFlameTrapComponent obj2 = obj as TikiFlameTrapComponent;
            obj2.ClearFlames = ClearFlames;
            obj2.MountedParticles = MountedParticles;
            //obj2.LinkPoints = LinkPoints;
            //obj2.ParticleTemplate = ParticleTemplate;
            
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

            // INSERT LOGGER HERE
            // Check particleTemplate
            // Check linkPoints
            //Assert.Fatal(particleTemplate != null, "A particle template must be assigned to the TikiFlameTrapComponent"); 

            return true;
        }

        /// <summary>
        /// Free up memory when this component is no longer needed.
        /// </summary>
        protected override void _OnUnregister()
        {
            //particleTemplate.Dispose();
            //linkPoints.Clear();
            if(mountedParticles.Count > 0)
                mountedParticles.Clear();

            base._OnUnregister();
        }

        #endregion
    }
}
