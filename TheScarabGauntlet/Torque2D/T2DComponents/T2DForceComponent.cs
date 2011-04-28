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
    /// Adding this component to a T2DSceneObject allows forces to be installed on the
    /// object.  Forces can be mounted to link points (and hence rotated) and can have
    /// their strength modified over time using TorqueInterfaces.
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaDependency(Type = typeof(T2DPhysicsComponent))]
    public class T2DForceComponent : TorqueComponent, IT2DForceGenerator
    {
        /// <summary>
        /// Base class for force to be added to T2DForceComponent.
        /// </summary>
        public class Force
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Force name.  The strength of the force can be looked up using a float ValueInterface on
            /// the owning object.  The range of this interface will be 0 to 1 (0 being min strength and
            /// 1 being max strength).
            /// </summary>
            public String Name;



            /// <summary>
            /// Link node to attach force to.  This can be left empty in which case the object center will be used.
            /// </summary>
            public String LinkName;



            /// <summary>
            /// Minimum strength of force.  Note, meaning of this depends on force type.  In base 
            /// Force class, force strength is the actual force size applied to the object.
            /// </summary>
            public float MinStrength;



            /// <summary>
            /// Maximum strength of force.  Note, meaning of this depends on force type.  In base 
            /// Force class, force strength is the actual force size applied to the object.
            /// </summary>
            public float MaxStrength;



            /// <summary>
            /// Initial strength of force.
            /// </summary>
            public float InitialStrength;



            /// <summary>
            /// Offset of force from object center (or link node if there is one).
            /// </summary>
            public Vector2 Offset;



            /// <summary>
            /// Rotation offset of force from object (or link node if there is one).
            /// </summary>
            public float RotationOffset;



            // 3 options for force direction: constant world space, constant object space, link node direction.  If use link
            // direction and object has no such link, then constant parameters are in effect.

            /// <summary>
            /// If true, the direction of the link point is used to determine force direction.  Othwerwise
            /// the constant direction is used.  This is false by default.
            /// </summary>
            public bool UseLinkDirection;



            /// <summary>
            /// If true, then supplied constant direction is in world space.  Otherwise constant direction
            /// is in object space.  This is false by default.
            /// </summary>
            public bool ConstantDirectionIsWorldSpace;



            /// <summary>
            /// Constant direction of force.  See also UseLinkDirection and ConstantDirectionIsWorldSpace.
            /// </summary>
            public float ConstantDirection;

            #endregion


            #region Public methods

            public virtual void UpdateForce(T2DPhysicsComponent physics, float strength, Vector2 offset, Vector2 direction, float dt)
            {
                strength = strength * (MaxStrength - MinStrength) + MinStrength;
                // will be modified by inverse mass (and inverse rot inertia)
                physics.ApplyImpulse(-direction * strength * dt, offset);
            }

            #endregion


            #region Private, protected, internal fields

            internal bool _preUpdateForce = false;

            #endregion
        }



        /// <summary>
        /// A force which ignores the mass of the object.  Force strength is acceleration applied to object.
        /// </summary>
        public class MasslessForce : Force
        {

            #region Public methods

            public override void UpdateForce(T2DPhysicsComponent physics, float strength, Vector2 offset, Vector2 direction, float dt)
            {
                strength = strength * (MaxStrength - MinStrength) + MinStrength;
                if (offset.LengthSquared() < 0.0001f || physics.Immovable)
                    // skip the mass route -- will work on immovable
                    physics.Velocity -= direction * strength * dt;
                else
                    // have to use real impulse, multiply by mass
                    physics.ApplyImpulse(-direction * strength * physics.Mass * dt, offset);
            }

            #endregion
        }



        /// <summary>
        /// A force which acts in the opposite direction as velocity.
        /// </summary>
        public class DragForce : Force
        {

            #region Constructors

            public DragForce()
            {
                _preUpdateForce = true;
            }

            #endregion


            #region Public properties, operators, constants, and enums

            /// <summary>
            /// If ConstantDrag is true then force strength is acceleration applied against velocity.  Otherwise, force
            /// strength is the multiplier applied to velocity to get acceleration.  So a value of
            /// 1 means we'll decelerate at a velocity that would stop us in 1 second if it were applied for the full
            /// second (but since we'll be going slower later on, it'll take longer...in fact we'll never fully stop).
            /// On the other hand, a value of 30 will stop us in a tick if we tick 30 times a second.
            /// </summary>
            public bool ConstantDrag;



            /// <summary>
            /// Minimum degrees per second to reduce rotation.
            /// </summary>
            public float MinimumRotationDrag;



            /// <summary>
            /// Maximum degrees per second to reduce rotation.
            /// </summary>
            public float MaximumRotationDrag;



            /// <summary>
            /// Maximum drag force that can be applied, in units/sec, regardless of velocity.  When
            /// this force is exceeded then a force of AbsoluteDragFactor * AbsoluteDragCap will be
            /// applied.  This allows "break free" type effects on, for example, car wheels.  If
            /// AbsoluteDragFactor=1 then this property simply acts as a maximum force which can be
            /// applied.
            /// </summary>
            public float AbsoluteDragCap = 10E10f;



            /// <summary>
            /// Maximum rotational drag which can be applied.  See also AbsoluteDragCap.
            /// </summary>
            public float AbsoluteDragRotationCap = 10E10f;



            /// <summary>
            /// Multiplier for force when drag force exceeds AbsoluteDragCap.  See AbsoluteDragCap for
            /// more information.
            /// </summary>
            public float AbsoluteDragFactor = 0.7f;

            #endregion


            #region Public methods

            public override void UpdateForce(T2DPhysicsComponent physics, float strength, Vector2 offset, Vector2 direction, float dt)
            {
                float rotStrength = strength * (MaximumRotationDrag - MinimumRotationDrag) + MinimumRotationDrag;
                strength = strength * (MaxStrength - MinStrength) + MinStrength;

                _ApplyDrag(physics.Velocity, physics, strength, rotStrength, offset, direction, dt);
            }

            #endregion


            #region Private, protected, internal methods

            protected void _ApplyDrag(Vector2 vel, T2DPhysicsComponent physics, float strength, float rotStrength, Vector2 offset, Vector2 direction, float dt)
            {
                // adjust rotation velocity based on rotation drag
                if (rotStrength < 0.0f)
                    // handle case where negative force is set
                    rotStrength = 0.0f;
                if (!ConstantDrag)
                    rotStrength *= Math.Abs(physics.AngularVelocity);
                if (rotStrength > AbsoluteDragRotationCap)
                    rotStrength = AbsoluteDragFactor * AbsoluteDragRotationCap;
                rotStrength *= dt;
                if (rotStrength > Math.Abs(physics.AngularVelocity))
                    physics.AngularVelocity = 0.0f;
                else if (physics.AngularVelocity > 0.0f)
                    physics.AngularVelocity -= rotStrength;
                else
                    physics.AngularVelocity += rotStrength;

                float velLen = vel.Length();
                if (velLen > Epsilon.Value)
                    vel.Normalize();

                // adjust by velocity and update interval
                if (!ConstantDrag)
                    strength *= velLen;
                if (strength > AbsoluteDragCap)
                    strength = AbsoluteDragFactor * AbsoluteDragCap;
                strength *= dt;

                // make sure we don't overshoot
                if (strength > velLen)
                    strength = velLen;

                if (!physics.Immovable)
                    physics.ApplyImpulse(-strength * vel * physics.Mass, offset);
                else
                    physics.Velocity -= strength * vel;
            }

            #endregion
        }



        /// <summary>
        /// A drag force which only acts on velocity in the direction of the force as determined by
        /// constant direction, link nodes, and owning SceneObject direction.
        /// </summary>
        public class DragForceDirectional : DragForce
        {

            #region Public methods

            public override void UpdateForce(T2DPhysicsComponent physics, float strength, Vector2 offset, Vector2 direction, float dt)
            {
                float rotStrength = strength * (MaximumRotationDrag - MinimumRotationDrag) + MinimumRotationDrag;
                strength = strength * (MaxStrength - MinStrength) + MinStrength;

                // figure velocity due to rotation
                Vector2 vel = new Vector2(-offset.Y, offset.X);
                vel *= 6.28f * physics.AngularVelocity / 360.0f;

                // add in linear velocity
                vel += physics.Velocity;

                float dot = Vector2.Dot(vel, direction);
                if (dot < 0.0f)
                    _ApplyDrag(dot * direction, physics, strength, rotStrength, offset, direction, dt);
            }

            #endregion
        }



        /// <summary>
        /// A drag force which only acts on velocity in the direction and the opposite direction of the 
        /// force as determined by constant direction, link nodes, and owning SceneObject direction.
        /// </summary>
        public class DragForceBidirectional : DragForce
        {
            // Apply drag force in direction specified by constant or link direction plus the opposite direction.


            #region Public methods

            public override void UpdateForce(T2DPhysicsComponent physics, float strength, Vector2 offset, Vector2 direction, float dt)
            {
                float rotStrength = strength * (MaximumRotationDrag - MinimumRotationDrag) + MinimumRotationDrag;
                strength = strength * (MaxStrength - MinStrength) + MinStrength;

                // figure velocity due to rotation
                Vector2 vel = new Vector2(-offset.Y, offset.X);
                vel *= 6.28f * physics.AngularVelocity / 360.0f;

                // add in linear velocity
                vel += physics.Velocity;

                float dot = Vector2.Dot(vel, direction);
                _ApplyDrag(dot * direction, physics, strength, rotStrength, offset, direction, dt);
            }

            #endregion

        }



        /// <summary>
        /// Instance data for a force.
        /// </summary>
        public struct ForceInstance
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Strength of force scaled between 0 and 1.
            /// </summary>
            public float Strength
            {
                get { return _strength.Value; }
                set { _strength.Value = MathHelper.Clamp(value, 0.0f, 1.0f); }
            }

            #endregion


            #region Private, protected, internal fields

            internal ValueInPlaceInterface<float> _strength;
            internal ValueInterface<float> _linkRotation;
            internal ValueInterface<Vector2> _linkPosition;

            #endregion
        }



        #region Public properties, operators, constants, and enums

        /// <summary>
        /// T2DSceneObject which owns the T2DForceComponent.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }



        /// <summary>
        /// Forces which are currently applied.
        /// </summary>
        public ReadOnlyArray<Force> Forces
        {
            get { return new ReadOnlyArray<Force>(_forces); }
        }



        /// <summary>
        /// Instance data for current forces.
        /// </summary>
        public ReadOnlyArray<ForceInstance> ForceInstances
        {
            get { return new ReadOnlyArray<ForceInstance>(_forceInstances); }
        }



        /// <summary>
        /// Number of current forces.
        /// </summary>
        public int Count
        {
            get { return _forces.Count; }
        }

        #endregion


        #region Public methods

        public void PreUpdateForces(Move move, float dt)
        {
            Assert.Fatal(SceneObject != null, "SceneObject must be set");

            T2DPhysicsComponent physics = SceneObject.Physics;
            Assert.Fatal(physics != null, "Physics must not be null");

            for (int i = 0; i < Count; i++)
            {
                if (_forces[i]._preUpdateForce)
                {
                    Vector2 offset, direction;
                    _GetForceData(_forces[i], _forceInstances[i], out offset, out direction);
                    _forces[i].UpdateForce(physics, _forceInstances[i].Strength, offset, direction, dt);
                }
            }
        }



        public void PostUpdateForces(Move move, float dt)
        {
            Assert.Fatal(SceneObject != null, "SceneObject must be set");

            T2DPhysicsComponent physics = SceneObject.Physics;
            Assert.Fatal(physics != null, "Physics must not be null");

            for (int i = 0; i < Count; i++)
            {
                if (!_forces[i]._preUpdateForce)
                {
                    Vector2 offset, direction;
                    _GetForceData(_forces[i], _forceInstances[i], out offset, out direction);
                    _forces[i].UpdateForce(physics, _forceInstances[i].Strength, offset, direction, dt);
                }
            }
        }



        /// <summary>
        /// Add a new force to this object.  Force will continue to be applied until RemoveForce
        /// is called for the force.
        /// </summary>
        /// <param name="force">Force to applye.</param>
        public void AddForce(Force force)
        {
            _forces.Add(force);
            ForceInstance fi = new ForceInstance();
            fi._strength = new ValueInPlaceInterface<float>();
            if (SceneObject != null)
                _SetupForce(force, ref fi);
            _forceInstances.Add(fi);
        }



        /// <summary>
        /// Remove an existing force.  If force is not currently being applied then this method
        /// returns false.
        /// </summary>
        /// <param name="force">Force to remove.</param>
        /// <returns>True if force was being applied.</returns>
        public bool RemoveForce(Force force)
        {
            for (int i = 0; i < Count; i++)
                if (_forces[i] == force)
                {
                    _TeardownForce(i);
                    return true;
                }
            return false;
        }



        /// <summary>
        /// Determine whether a given force is being applied to this object.
        /// </summary>
        /// <param name="force">Force to test.</param>
        /// <returns>True if force is being applied.</returns>
        public bool HasForce(Force force)
        {
            for (int i = 0; i < Count; i++)
                if (_forces[i] == force)
                    return true;
            return false;
        }



        /// <summary>
        /// Get strength of given force, scaled between Force.MinStrength and Force.MaxStrength.
        /// </summary>
        /// <param name="force">Force to query.</param>
        /// <returns>Raw strength of force.</returns>
        public float GetRawForceStrength(Force force)
        {
            for (int i = 0; i < Count; i++)
                if (_forces[i] == force)
                    return GetRawForceStrength(i);
            return 0.0f;
        }



        /// <summary>
        /// Get strength of indexed force, scaled between Force.MinStrength and Force.MaxStrength.
        /// </summary>
        /// <param name="idx">Index of force to query.</param>
        /// <returns>Raw strength of force.</returns>
        public float GetRawForceStrength(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < Count, "Index out of range");
            if (idx >= 0 && idx < Count)
                return _forceInstances[idx].Strength * (_forces[idx].MaxStrength - _forces[idx].MinStrength) + _forces[idx].MinStrength;
            return 0.0f;
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DForceComponent obj2 = (T2DForceComponent)obj;
            for (int i = 0; i < _forces.Count; i++)
            {
                obj2.AddForce(_forces[i]);
                // set initial value
                obj2._forceInstances[i]._strength.Value = _forceInstances[i]._strength.Value;
            }
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            for (int i = 0; i < Count; i++)
            {
                ForceInstance fi = _forceInstances[i];
                _SetupForce(_forces[i], ref fi);
                _forceInstances[i] = fi;
            }

            return true;
        }



        protected override void _RegisterInterfaces(TorqueObject owner)
        {
            base._RegisterInterfaces(owner);

            // search us for float interfaces of any name
            Owner.RegisterCachedInterface("float", null, this, null);
            // cache force interface and match empty name only
            Owner.RegisterCachedInterface("force", String.Empty, this, _forceInterface);
        }



        protected override void _GetInterfaces(PatternMatch typeMatch, PatternMatch nameMatch, List<TorqueInterface> list)
        {
            if (typeMatch.TestMatch("float"))
            {
                for (int i = 0; i < Count; i++)
                {
                    if (nameMatch.TestMatch(_forces[i].Name))
                        list.Add(_forceInstances[i]._strength);
                }
            }

            base._GetInterfaces(typeMatch, nameMatch, list);
        }



        protected override void _OnUnregister()
        {
            base._OnUnregister();

            while (Count > 0)
                _TeardownForce(Count - 1);
        }



        protected void _GetForceData(Force force, ForceInstance fi, out Vector2 offset, out Vector2 direction)
        {
            offset = SceneObject.GetWorldLinkPosition(fi._linkPosition, fi._linkRotation, force.Offset) - SceneObject.Position;

            float dirRot;
            if (force.UseLinkDirection)
            {
                dirRot = SceneObject.GetWorldLinkRotation(fi._linkRotation, force.RotationOffset);
            }
            else
            {
                dirRot = force.ConstantDirection;
                if (!force.ConstantDirectionIsWorldSpace)
                    dirRot += SceneObject.Rotation;
                dirRot = (dirRot + force.RotationOffset) % 360.0f;
            }

            direction = T2DVectorUtil.VectorFromAngle(dirRot);
        }



        protected void _SetupForce(Force force, ref ForceInstance fi)
        {
            Assert.Fatal(SceneObject != null, "Cannot set up force before adding to scene");

            SceneObject.RegisterInterface(this, fi._strength);
            fi._strength.Value = force.InitialStrength;

            if (force.LinkName != null && force.LinkName != String.Empty)
            {
                fi._linkPosition = SceneObject.Components.GetInterface<ValueInterface<Vector2>>("vector2", force.LinkName);
                fi._linkRotation = SceneObject.Components.GetInterface<ValueInterface<float>>("float", force.LinkName);
            }
        }



        protected void _TeardownForce(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _forceInstances.Count && _forceInstances.Count == _forces.Count, "Illegal index or force count.");
            if (_forceInstances[idx]._strength != null)
                // invalidate interfaces we own
                _forceInstances[idx]._strength._owner.Object = null;
            _forces.RemoveAt(idx);
            _forceInstances.RemoveAt(idx);
        }



        [TorqueXmlDeserializeInclude]
        [XmlElement(ElementName = "Forces")]
        internal List<Force> _XMLForces
        {
            get { return null; }
            set
            {
                for (int i = 0; i < value.Count; i++)
                {
                    AddForce(value[i]);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        TorqueInterfaceWrap<IT2DForceGenerator> _forceInterface = new TorqueInterfaceWrap<IT2DForceGenerator>();
        List<Force> _forces = new List<Force>();
        List<ForceInstance> _forceInstances = new List<ForceInstance>();

        #endregion

    }
}
