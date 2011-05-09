using GarageGames.Torque.T2D;

namespace PlatformerStarter.Common.Triggers
{
    public interface ISwitchBehavior
    {
        void Execute(T2DSceneObject switchObject, bool switchedOn);
    }
}