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

namespace PlatformerStarter
{
    [TorqueXmlSchemaType]
    public class ReplacementComponent : TorqueComponent
    {
        private T2DSceneObject replacementTemplate;
        private bool replaced;

        #region Properties
        public T2DSceneObject ReplacementTemplate
        {
            get { return replacementTemplate; }
            set { replacementTemplate = value; }
        }
        public bool Replaced
        {
            get { return replaced; }
        }
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }
        #endregion

        public void ReplaceObject()
        {
            if(!replaced)
            {
                T2DSceneObject replacement = replacementTemplate.Clone() as T2DSceneObject;
                replacement.Position = SceneObject.Position;
                
                TorqueObjectDatabase.Instance.Register(replacement);
                replaced = true;
            }
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ReplacementComponent obj2 = obj as ReplacementComponent;
            obj2.ReplacementTemplate = ReplacementTemplate;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            if(replacementTemplate != null)
                replaced = false;
            else
                replaced = true;

            return true;
        }

        protected override void _OnUnregister()
        {
            // todo: perform de-initialization for the component

            base._OnUnregister();
        }
    }
}
