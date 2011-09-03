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

using GarageGames.Torque.PlatformerFramework;
using PlatformerStarter.Enemies.ActorComponents;

namespace PlatformerStarter.Common
{
    [TorqueXmlSchemaType]
    public class CutsceneTrigger : DirectionalTriggerComponent, ITickObject
    {
        #region Private Members
        private T2DSceneObject cameraTarget;
        private Vector2 stopOffset;
        private HulkActorComponent hulk;
        private bool playerEntered;
        private bool transitionFinished;
        private CameraManager camera;
        #endregion

        #region Public Properties
        public T2DSceneObject CameraTarget
        {
            get { return cameraTarget; }
            set { cameraTarget = value; }
        }
        public Vector2 StopOffset
        {
            get { return stopOffset; }
            set { stopOffset = value; }
        }
        public T2DOnCollisionDelegate CutsceneCollision
        {
            get { return OnCollision; }
        }
        #endregion

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            PlayerActorComponent player = theirObject.Components.FindComponent<PlayerActorComponent>();

            if (player != null && player.OnGround)
            {
                ((PlayerController)player.Controller).TogglePlayerControl();
                playerEntered = true;
                player.HorizontalStop();

                camera.MoveToTarget(cameraTarget, 2000f);
                SceneObject.CollisionsEnabled = false;
            }
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if(!base._OnRegister(owner))
                return false;

            if (cameraTarget != null)
                hulk = cameraTarget.Components.FindComponent<HulkActorComponent>();

            ProcessList.Instance.AddTickCallback(this.SceneObject, this);

            playerEntered = false;

            SceneObject.SetObjectType(PlatformerData.ActorTriggerObjectType, true);

            transitionFinished = false;

            //camera = CameraManager.Instance;

            return true;
        }

        public virtual void ProcessTick(Move move, float elapsed)
        {
            if (playerEntered)
            {
                if(!camera.IsMoving)
                {
                    if (!transitionFinished)
                    {
                        transitionFinished = true;
                        hulk.PlayIntro();
                    }
                }

                if (hulk.AnimationManager.CurrentState.StateName == "intro")
                {
                    if (hulk.AnimatedSprite.CurrentFrame == hulk.AnimatedSprite.FinalFrame)
                    {
                        T2DSceneObject player = (T2DSceneObject)TorqueObjectDatabase.Instance.FindObject("Amanda");

                        camera.ReturnToPlayer(player, 200f);

                    }
                }

                if (hulk.AnimationManager.PreviousState != null)
                {
                    if (hulk.AnimationManager.PreviousState.StateName == "intro")
                    {
                        if (!camera.IsMoving)
                        {
                            camera.Zoom(0.5f, 3000f);
                            T2DSceneObject player = (T2DSceneObject)TorqueObjectDatabase.Instance.FindObject("Amanda");
                            PlayerActorComponent actor = player.Components.FindComponent<PlayerActorComponent>();
                            ((PlayerController)actor.Controller).TogglePlayerControl();
                            SceneObject.MarkForDelete = true;
                         }
                    }
                }
            }

        }

        public virtual void InterpolateTick(float k) { }
    }
}
