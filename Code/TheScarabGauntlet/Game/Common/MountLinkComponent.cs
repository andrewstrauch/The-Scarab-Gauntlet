using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

namespace PlatformerStarter.Common
{
    [TorqueXmlSchemaType]
    public class MountLinkComponent : TorqueComponent
    {
        #region Private Members
        private T2DSceneObject mount;
        private string linkPointName;
        private Vector2 mountOffset;
        #endregion
        
        #region Properties

        public T2DSceneObject ObjectToMountTo
        {
            get { return mount; }
            set { mount = value; }
        }

        public string MountLPName
        {
            get { return linkPointName; }
            set { linkPointName = value; }
        }

        public Vector2 MountOffset
        {
            get { return mountOffset; }
            set { mountOffset = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
        }

        #endregion

        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            if (mount != null)
            {
                

                List<T2DSceneObject> mountedObjects = new List<T2DSceneObject>();
                mount.GetMountedObjects("*", mountedObjects);

                foreach (T2DSceneObject obj in mountedObjects)
                    TorqueObjectDatabase.Instance.Register(obj);
                
                TorqueObjectDatabase.Instance.Register(mount);
                float rotation = 0.0f;
                Vector2 linkPointOffset;

                if (mount.LinkPoints.HasLinkPoint(linkPointName))
                {
                    mount.LinkPoints.GetLinkPoint(linkPointName, out linkPointOffset, out rotation);
                    mount.Mount(SceneObject, linkPointName, -(linkPointOffset + mountOffset), 0, true);
                }
            }

            return true;
        }

        protected override void _OnUnregister()
        {
            // todo: perform de-initialization for the component

            base._OnUnregister();
        }
        #endregion
    }
}
