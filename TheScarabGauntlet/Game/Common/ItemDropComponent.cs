//**btp_replace(namespace GarageGames.VCSTemplates.ItemTemplates,namespace PlatformerStarter.Common)
//**btp_replace(class TestClass,public class ItemDropComponent)
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
    public class ItemDropComponent : TorqueComponent
    {
        #region Private Members

        private T2DSceneObject dropItemTemplate;
        private int numItems;
        
        #endregion

        #region Public Properties

        /// <summary>
        /// The template to reference when we want to spawn new items.
        /// </summary>
        public T2DSceneObject DropItemTemplate
        {
            get { return dropItemTemplate; }
            set { dropItemTemplate = value; }
        }

        /// <summary>
        /// The number of items to be dropped.
        /// </summary>
        public int NumItems
        {
            get { return numItems; }
            set { numItems = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ItemDropComponent obj2 = obj as ItemDropComponent;
            obj2.DropItemTemplate = DropItemTemplate;
            obj2.NumItems = NumItems;
        }

        /// <summary>
        /// Drops a number of referenced items.
        /// </summary>
        public void DropItems()
        {
            T2DSceneObject newItem;

            if (dropItemTemplate != null)
            {
                for (int i = 0; i < numItems; ++i)
                {
                    newItem = dropItemTemplate.Clone() as T2DSceneObject;
                    newItem.Position = SceneObject.Position;
                    TorqueObjectDatabase.Instance.Register(newItem);
                }
            }
            else
                Console.WriteLine("The template is not set!");

        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;            

            return true;
        }

        protected override void _OnUnregister()
        {
            dropItemTemplate.Dispose();

            base._OnUnregister();
        }

        #endregion
    }
}
