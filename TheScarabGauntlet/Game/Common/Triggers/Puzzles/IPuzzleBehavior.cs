#region Using Directives
using GarageGames.Torque.T2D;
#endregion

namespace PlatformerStarter.Common.Triggers
{
    public interface IPuzzleBehavior
    {
        bool Active
        {
            get;
            set;
        }

        void Execute(T2DSceneObject puzzleObject);
    }
}