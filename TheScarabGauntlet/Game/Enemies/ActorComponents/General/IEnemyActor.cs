using System;
using GarageGames.Torque.T2D;

namespace PlatformerStarter.Enemies
{
    public interface IEnemyActor
    {
        void Attack();

        T2DSceneObject Actor
        {
            get;
        }

        bool ReadyToAttack
        {
            get;
            set;
        }
    }
}
