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
    public class BoundedRotationComponent : TorqueComponent, ITickObject
    {
        #region Private members
        private float speed;
        private float currentRotation;
        private float startRotation;
        private float endRotation;
        private float signCoefficient;
        private bool readyToRotate;
        private bool facingLeft;
        private TorqueEventDelegate<T2DSceneObject> onRotationFinished;
        TorqueEvent<T2DSceneObject> rotationFinishedEvent;
        #endregion

        #region Properties
        public float StartRotation
        {
            get { return startRotation; }
            set { startRotation = value; }
        }
        public float EndRotation
        {
            get { return endRotation; }
            set { endRotation = value; }
        }
        public float RotationSpeed
        {
            get { return speed; }
            set { speed = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool FacingLeft
        {
            get { return facingLeft; }
            set { facingLeft = value; }
        }
        [System.Xml.Serialization.XmlIgnore]
        public TorqueEventDelegate<T2DSceneObject> OnRotationFinished
        {
            get { return onRotationFinished; }
            set { onRotationFinished = value; }
        }
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines
        /// <summary>
        /// Starts the rotation of the attack
        /// </summary>
        public void BeginRotation()
        {
            readyToRotate = true;
        }

        public virtual void ProcessTick(Move move, float dt)
        {
            //BeginRotation();
            if (readyToRotate)
            {
                if (GetRotationCondition())
                    currentRotation += (signCoefficient * speed);
                else
                {
                    currentRotation = startRotation;
                    readyToRotate = false;
                    if (onRotationFinished != null)
                        TorqueEventManager.TriggerEvent<T2DSceneObject>(rotationFinishedEvent, SceneObject);
                }
            }

            SceneObject.Rotation = currentRotation;
        }

        public virtual void InterpolateTick(float k)
        {
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BoundedRotationComponent obj2 = obj as BoundedRotationComponent;

            obj2.StartRotation = StartRotation;
            obj2.EndRotation = EndRotation;
            obj2.FacingLeft = FacingLeft;
        }

        #endregion

        #region Private Routines
        private bool GetRotationCondition()
        {
            if (facingLeft)
                return (currentRotation > endRotation);
            else
                return (currentRotation < endRotation);
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            readyToRotate = false;

            if (facingLeft)
                signCoefficient = -1.0f;
            else
            {
                signCoefficient = 1.0f;
                endRotation += 180.0f;
            }

            if (onRotationFinished != null)
            {
                rotationFinishedEvent = new TorqueEvent<T2DSceneObject>("RotationFinished");
                TorqueEventManager.ListenEvents<T2DSceneObject>(rotationFinishedEvent, OnRotationFinished);
            }

            BeginRotation();
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
