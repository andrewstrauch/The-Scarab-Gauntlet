//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Add this component to a T2DSceneObject in order to be able to add simple
    /// Processors which perform simple calculations on float ValueInterfaces exposed
    /// by other components in their RegisterInterfaces call.  Typical usage of a 
    /// T2DProcessComponent.Processor is to convert game pad input into the rotation
    /// of a link point or the strength of a force.  In addition to the flexibility
    /// the Processor framework provides, the output of a Processor is automatically
    /// interpolated during InterpolateTick callbacks.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DProcessComponent : TorqueComponent, ITickObject
    {
        /// <summary>
        /// Abstract base class for processors which can be added to T2DProcessComponent.
        /// </summary>
        abstract public class Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Enum for determine which of several potential operations to perform on input.
            /// </summary>
            public enum ProcessMode
            {
                /// <summary>
                /// Make no changes to the input other than clamping to MinValue and MaxValue and
                /// inverting the range if InvertInput is true.
                /// </summary>
                Direct,
                /// <summary>
                /// Move towards input value, never moving faster than MaxSpeed or slower than
                /// MinSpeed.
                /// </summary>
                Track,
                /// <summary>
                /// Update value using input as a velocity.  How the input is converted to velocity
                /// depends on the processor, but typically the input is scaled from it's possible range,
                /// say [0,1] to [MinSpeed,MaxSpeed].
                /// </summary>
                Delta
            };



            /// <summary>
            /// Name of interface to process.  If interface name not found among the registered interfaces for
            /// the object, then the procesor will have no effect.
            /// </summary>
            public String InterfaceName;



            /// <summary>
            /// Minimum value of output.
            /// </summary>
            public float MinValue;



            /// <summary>
            /// Maximum value of output.
            /// </summary>
            public float MaxValue;



            /// <summary>
            /// Minimum speed input can change for ProcessMode.Track and ProcessMode.Delta.
            /// </summary>
            public float MinSpeed;



            /// <summary>
            /// Maximum speed input can change for ProcessMode.Track and ProcessMode.Delta.
            /// </summary>
            public float MaxSpeed;



            /// <summary>
            /// If true, Processor will invert the range of the input.
            /// </summary>
            public bool InvertInput;



            /// <summary>
            /// If true and the mode is ProcessMode.Track or ProcessMode.Delta then Processor
            /// will treat range as a cyclical range (values less than MinValue will be mapped to
            /// MaxValue-(MinValue-value) and values greater than MaxValue will be mapped to
            /// Minvalue+(value-MaxValue).
            /// </summary>
            public bool Cycle;



            /// <summary>
            /// Mode of processing input.
            /// </summary>
            public ProcessMode Mode;

            #endregion


            #region Public methods

            /// <summary>
            /// Processor does it's work in this method.  If writing your own Processor this is
            /// where you do your thing.
            /// </summary>
            /// <param name="move">The move passed into ProcessTick for the object.</param>
            /// <param name="dt">The elapsed time, in seconds, passed into ProcessTick for the object.</param>
            /// <param name="val">The output value.</param>
            abstract public void Process(Move move, float dt, ref float val);



            /// <summary>
            /// This method called when Processor is first added.
            /// </summary>
            /// <param name="sceneObject">T2DSceneObject which owns the T2DProcessComponent.</param>
            /// <param name="initialVal">Initial value of the Processor.</param>
            virtual public void Init(T2DSceneObject sceneObject, ref float initialVal) { }

            #endregion


            #region Private, protected, internal methods

            protected void _Update(ref float val, float updateVal, bool prescaled)
            {
                if (!prescaled && InvertInput)
                    updateVal = 1.0f - updateVal;

                switch (Mode)
                {
                    case ProcessMode.Direct:
                        if (!prescaled)
                            updateVal = MinValue + (MaxValue - MinValue) * updateVal;
                        val = MathHelper.Clamp(updateVal, MinValue, MaxValue);
                        break;
                    case ProcessMode.Track:
                        if (!prescaled)
                            updateVal = MinValue + (MaxValue - MinValue) * updateVal;
                        float delta = updateVal - val;
                        if (Cycle)
                        {
                            if (Math.Abs(delta) > 0.5f * (MaxValue - MinValue))
                            {
                                // go the other way
                                if (delta > 0.0f)
                                    delta -= (MaxValue - MinValue);
                                else
                                    delta += (MaxValue - MinValue);
                            }
                        }
                        delta = MathHelper.Clamp(delta, -MinSpeed, MaxSpeed);
                        val += delta;
                        break;
                    case ProcessMode.Delta:
                        if (!prescaled)
                            updateVal = (1.0f - updateVal) * MinSpeed + updateVal * MaxSpeed;
                        val += updateVal;
                        if (Cycle)
                            val = ((val - MinValue) % (MaxValue - MinValue)) + MinValue;
                        else
                            val = MathHelper.Clamp(val, MinValue, MaxValue);
                        break;
                }
            }
            #endregion
        }


        #region Pre-defined processors

        /// <summary>
        /// Processor which processes a single axis of a Move's Stick.
        /// </summary>
        public class StickProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Index of stick to process.
            /// </summary>
            public int StickIndex;



            /// <summary>
            /// Axis to process (0 = x-axis, 1 = y-axis).
            /// </summary>
            public int Axis;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(StickIndex >= 0 && StickIndex < move.Sticks.Count, "StickIndex out of range");
                if (StickIndex >= move.Sticks.Count || StickIndex < 0)
                    return;

                float input = Axis == 0 ? move.Sticks[StickIndex].X : move.Sticks[StickIndex].Y;
                input = 0.5f * (input + 1.0f);
                _Update(ref val, input, false);
            }

            #endregion
        }



        /// <summary>
        /// Processor which processes a Move's Lever.
        /// </summary>
        public class LeverProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Lever axis to process.
            /// </summary>
            public int LeverIndex;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(LeverIndex >= 0 && LeverIndex < move.Levers.Count, "LeverIndex out of range");
                if (LeverIndex >= move.Levers.Count || LeverIndex < 0)
                    return;

                float input = move.Levers[LeverIndex].Value;
                input = 0.5f * (input + 1.0f);
                _Update(ref val, input, false);
            }

            #endregion
        }



        /// <summary>
        /// Processor which processes a Move's Trigger.
        /// </summary>
        public class TriggerProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Index of Trigger to process.
            /// </summary>
            public int TriggerIndex;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(TriggerIndex >= 0 && TriggerIndex < move.Triggers.Count, "TriggerIndex out of range");
                if (TriggerIndex >= move.Triggers.Count || TriggerIndex < 0)
                    return;

                float input = move.Triggers[TriggerIndex].Value;
                _Update(ref val, input, false);
            }

            #endregion
        }



        /// <summary>
        /// Processor which processes a Move's Button.
        /// </summary>
        public class ButtonProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Index of button to process.
            /// </summary>
            public int ButtonIndex;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(ButtonIndex >= 0 && ButtonIndex < move.Buttons.Count, "ButtonIndex out of range");
                if (ButtonIndex >= move.Buttons.Count || ButtonIndex < 0)
                    return;

                float input = move.Buttons[ButtonIndex].Pushed ? 1.0f : 0.0f;
                _Update(ref val, input, false);
            }

            #endregion
        }



        /// <summary>
        /// Processor which processes the rotation of a Move's stick.  See also PolarLengthProcessor.
        /// </summary>
        public class PolarRotationProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Index of Stick to process.
            /// </summary>
            public int StickIndex;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(StickIndex >= 0 && StickIndex < move.Sticks.Count, "StickIndex out of range");
                if (StickIndex >= move.Sticks.Count || StickIndex < 0)
                    return;

                float x = move.Sticks[StickIndex].X;
                float y = move.Sticks[StickIndex].Y;
                if (x * x + y * y < 0.01f)
                    // zero length, can't determine angle so leave angle as is
                    return;
                float invLen = 1.0f / (float)Math.Sqrt(x * x + y * y);
                x *= invLen;
                y *= invLen;
                float theta = (float)Math.Acos(-x);
                if (y < 0.0f)
                    theta = -theta;
                float targetRotation = (MathHelper.ToDegrees(theta) + 270.0f) % 360.0f;

                _Update(ref val, targetRotation, true);
            }

            #endregion
        }



        /// <summary>
        /// Processor which processes the length of a Move's stick.  See also PolarRotationProcessor.
        /// </summary>
        public class PolarLengthProcessor : Processor
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Index of Stick to process.
            /// </summary>
            public int StickIndex;

            #endregion


            #region Public methods

            public override void Process(Move move, float dt, ref float val)
            {
                if (move == null)
                    return;

                Assert.Fatal(StickIndex >= 0 && StickIndex < move.Sticks.Count, "StickIndex out of range");
                if (StickIndex >= move.Sticks.Count || StickIndex < 0)
                    return;

                float x = move.Sticks[StickIndex].X;
                float y = move.Sticks[StickIndex].Y;
                float len = (float)Math.Sqrt(x * x + y * y);
                _Update(ref val, len, false);
            }

            #endregion
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// T2DSceneObject which owns the component.
        /// </summary>
        T2DSceneObject SceneObject { get { return Owner as T2DSceneObject; } }

        #endregion


        #region Public methods

        public void ProcessTick(Move move, float dt)
        {
            for (int i = 0; i < _processors.Count; i++)
            {
                ProcessData pd = _processData[i];
                float val = pd.Interface.Value;
                pd.PreTick = val;
                _processors[i].Process(move, dt, ref val);
                pd.Interface.Value = val;
                pd.PostTick = val;
                _processData[i] = pd;
            }
        }



        public void InterpolateTick(float k)
        {
            for (int i = 0; i < _processors.Count; i++)
            {
                float pre = _processData[i].PreTick;
                float post = _processData[i].PostTick;
                float range = _processors[i].MaxValue - _processors[i].MinValue;
                if (_processors[i].Cycle)
                {
                    if (Math.Abs(post - pre) > 0.5f * range)
                    {
                        if (post > pre)
                            post -= range;
                        else
                            post += range;
                    }
                }
                float val = (1.0f - k) * pre + k * post;
                if (_processors[i].Cycle)
                    val = (val + range) % range;
                _processData[i].Interface.Value = val;
            }
        }



        /// <summary>
        /// Add a new processor.  Processors can be added on the fly to adjust how
        /// a game object responds depending on game conditions.
        /// </summary>
        /// <param name="p">New processor.</param>
        /// <returns>True if processor successfully added, otherwise false.</returns>
        public bool AddProcessor(Processor p)
        {
            if (SceneObject != null)
            {
                ProcessData data = new ProcessData();
                if (_InitProcessor(p, ref data))
                {
                    _processors.Add(p);
                    _processData.Add(data);
                    return true;
                }
                return false;
            }
            _processors.Add(p);
            _processData.Add(new ProcessData());
            return true;
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DProcessComponent obj2 = (T2DProcessComponent)obj;
            for (int i = 0; i < _processors.Count; i++)
            {
                obj2._processors.Add(_processors[i]);
                obj2._processData.Add(new ProcessData());
            }
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            for (int i = 0; i < _processors.Count; i++)
            {
                ProcessData data = new ProcessData();
                if (!_InitProcessor(_processors[i], ref data))
                {
                    Assert.Fatal(false, "Float interface " + _processors[i].InterfaceName + " not found.");
                    return false;
                }
                _processData[i] = data;
            }

            ProcessList.Instance.AddTickCallback(SceneObject, this, 0.0f);

            return true;
        }



        protected override void _OnUnregister()
        {
            ProcessList.Instance.RemoveObject(SceneObject);
            base._OnUnregister();
            _processors.Clear();
            _processData.Clear();
        }



        protected bool _InitProcessor(Processor p, ref ProcessData data)
        {
            ValueInterface<float> iface = SceneObject.Components.GetInterface<ValueInterface<float>>("float", p.InterfaceName);
            if (iface != null)
            {
                float val = iface.Value;
                p.Init(SceneObject, ref val);
                iface.Value = val;
                data.Interface = iface;
                data.PreTick = val;
                data.PostTick = val;
                return true;
            }

            return false;
        }



        [TorqueXmlDeserializeInclude]
        [XmlElement(ElementName = "ProcessNodes")]
        internal List<T2DProcessComponent.Processor> _XMLProcessNodes
        {
            get { return null; }
            set
            {
                for (int i = 0; i < value.Count; i++)
                {
                    AddProcessor(value[i]);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        protected struct ProcessData
        {
            public ValueInterface<float> Interface;
            public float PreTick;
            public float PostTick;
        }

        List<Processor> _processors = new List<Processor>();
        List<ProcessData> _processData = new List<ProcessData>();

        #endregion
    }
}
