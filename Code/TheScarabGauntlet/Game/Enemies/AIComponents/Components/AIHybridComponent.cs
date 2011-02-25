using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    [TorqueXmlSchemaType]
    public class AIHybridComponent : BaseAIComponent, IBehavior
    {
        protected AIHybridController controller;
        protected float maxRangedDist;
        protected float minRangedDist;

        #region Properties
        [System.Xml.Serialization.XmlIgnore]
        public BaseAIController Controller
        {
            get { return controller; }
            set { controller = (AIHybridController)value; }
        }
        public float MaxRangedDist
        {
            get { return maxRangedDist; }
            set { maxRangedDist = value; }
        }
        public float MinRangedDist
        {
            get { return minRangedDist; }
            set { minRangedDist = value; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            AIHybridComponent obj2 = obj as AIHybridComponent;

            obj2.MaxRangedDist = MaxRangedDist;
            obj2.MinRangedDist = MinRangedDist;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            controller = new AIHybridController();

            controller.AttackDist = AttackDist;
            controller.AlertDist = AlertDist;
            controller.Attacks = Attacks;
            controller.MaxRangedDist = maxRangedDist;
            controller.MinRangedDist = minRangedDist;

            return true;
        }
    }
}
