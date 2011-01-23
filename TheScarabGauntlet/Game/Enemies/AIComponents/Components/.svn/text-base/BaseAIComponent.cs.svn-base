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

namespace PlatformerStarter.Enemies
{
    public class BaseAIComponent : TorqueComponent
    {
        protected float minAttackDist;
        protected float maxAlertDist;
        protected bool attacks;

        #region Properties
        public float AttackDist
        {
            get { return minAttackDist; }
            set { minAttackDist = value; }
        }
        public float AlertDist
        {
            get { return maxAlertDist; }
            set { maxAlertDist = value; }
        }
        public bool Attacks
        {
            get { return attacks; }
            set { attacks = value; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BaseAIComponent obj2 = obj as BaseAIComponent;

            obj2.AttackDist = AttackDist;
            obj2.AlertDist = AlertDist;
            obj2.Attacks = Attacks;
        }

        public void Initialize(TorqueObject owner)
        {
            _OnRegister(owner);
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;
            else
                return true;
        }
    }
}
