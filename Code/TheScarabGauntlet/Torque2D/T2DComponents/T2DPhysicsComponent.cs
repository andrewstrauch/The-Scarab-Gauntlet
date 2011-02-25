//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interface for providing forces to the T2DPhysicsComponent.  The interface
    /// should be exposed using a TorqueInterfaceWrap during a components RegisterInterfaces
    /// call using an interface type name of "force".
    /// </summary>
    public interface IT2DForceGenerator
    {
        /// <summary>
        /// Update force before movement.  Force should act directly on the
        /// T2DSceneObject velocity and angular velocity.
        /// </summary>
        /// <param name="move">Move passed into ProcessTick.</param>
        /// <param name="dt">Delta time, in seconds.</param>
        void PreUpdateForces(Move move, float dt);



        /// <summary>
        /// Update force after movement.  Force should act directly on the
        /// T2DSceneObject velocity and angular velocity.
        /// </summary>
        /// <param name="move">Move passed into ProcessTick.</param>
        /// <param name="dt">Delta time, in seconds.</param>
        void PostUpdateForces(Move move, float dt);
    }



    public delegate void T2DResolveCollisionDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth);



    /// <summary>
    /// Adding this component to a T2DSceneObject gives it physics.  T2DPhysicsComponent
    /// supports rigid body physics as well as simpler physical simulation.  Set the 
    /// ResolveCollision property to determine how the object will respond to collisions.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DPhysicsComponent : TorqueComponent, ITickObject, IDisposable
    {

        #region Public properties, operators, constants, and enums

        public const float MinimumMass = 0.01f;
        public const float MinimumRotationalInertia = 0.01f;



        /// <summary>
        /// Current velocity of SceneObject.
        /// </summary>
        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }



        /// <summary>
        /// X component of current SceneObject velocity.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public float VelocityX
        {
            get { return _velocity.X; }
            set { _velocity.X = value; }
        }



        /// <summary>
        /// Y component of current SceneObject velocity.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public float VelocityY
        {
            get { return _velocity.Y; }
            set { _velocity.Y = value; }
        }



        /// <summary>
        /// Current angular (rotational) velocity of SceneObject (in degrees/sec).
        /// </summary>
        public float AngularVelocity
        {
            get { return _angularVelocity; }
            set { _angularVelocity = value; }
        }



        /// <summary>
        /// Inverse mass of SceneObject.  Setting this to 0 is same as marking object
        /// Immovable.  Mass (and InverseMass) is used when responding to collisions 
        /// and when forces are applied.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1.0")]
        public float InverseMass
        {
            get { return _inverseMass; }
            set { _inverseMass = value; }
        }



        /// <summary>
        /// Inverse moment of rotational inertia.  This affects how easily the object
        /// rotates.  By default, it is equal to the value expected from a box with 
        /// uniform distribution of mass.  Set RotationalScale to affect this property.
        /// </summary>
        public float InverseRotationalInertia
        {
            get
            {
                if (SceneObject == null)
                    // can't compute without scene object
                    return 0.0f;
                float x = SceneObject.Size.X;
                float y = SceneObject.Size.Y;
                float areaConstant = x * x + y * y + x * y;
                if (areaConstant < Epsilon.Value)
                    // just return generically big number
                    return 10.0f;
                return 6.0f * _rotationalScale * InverseMass / areaConstant;
            }
        }



        /// <summary>
        /// Scale the inverse inertia.  Default value is 1.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1.0")]
        public float RotationScale
        {
            get { return _rotationalScale; }
            set { _rotationalScale = value; }
        }



        /// <summary>
        /// If true, object cannot be moved in a collision with another object.
        /// Note that object can still move if velocity is set manually.  Collisions
        /// with this object will then act like collisions against a moving wall.
        /// If two immovable objects collide then resolution will be erratic, so this
        /// should be avoided.
        /// </summary>
        [XmlIgnore]
        public bool Immovable
        {
            get { return _inverseMass < Epsilon.Value; }
            set { _inverseMass = value == true ? 0 : 1; }
        }



        /// <summary>
        /// If false, object cannot be moved in a collision with another object.
        /// See Immovable for more information.
        /// </summary>
        public bool CanMove
        {
            get { return !Immovable; }
        }



        /// <summary>
        /// Mass of object.  Mass (and InverseMass) is used when responding to collisions 
        /// and when forces are applied.
        /// </summary>
        [TorqueCloneIgnore]
        public float Mass
        {
            get { return Immovable ? 10E10f : 1.0f / _inverseMass; }
            set { _inverseMass = 1.0f / MathHelper.Max(MinimumMass, value); }
        }



        /// <summary>
        /// True if object can be rotated in a collision, false otherwise.  Set
        /// RotationalScale to 0 to make this property false.  Note that if this
        /// property is set to false the object can still rotate if AngularRotation
        /// is set manually.
        /// </summary>
        public bool CanRotate
        {
            get { return _rotationalScale > Epsilon.Value; }
        }



        /// <summary>
        /// Rotational moment of intertia of SceneObject.  See InverseRotationalInertia
        /// for more information.
        /// </summary>
        public float RotationalInertia
        {
            get
            {
                float inverseRotI = InverseRotationalInertia;
                if (inverseRotI < Epsilon.Value)
                    return 1.0f / inverseRotI;
                return 10E10f;
            }
        }



        /// <summary>
        /// If true, collisions will be processed even while object is at rest.
        /// If false, object with zero velocity and angular velocity will not 
        /// process collisions.  Setting to false can lead to better performance
        /// but in some situations can miss collisions.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "false")]
        public bool ProcessCollisionsAtRest
        {
            get { return _processCollisionsAtRest; }
            set { _processCollisionsAtRest = value; }
        }



        /// <summary>
        /// Owning T2DSceneObject.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }



        /// <summary>
        /// If ResolveCollision is set to this static delegate then collisions
        /// will result in a bounce with no rotation.  The physics material will be used
        /// to determine the amount of bounce.
        /// </summary>
        static public T2DResolveCollisionDelegate BounceCollision
        {
            get { return _bounceCollision; }
        }



        /// <summary>
        /// If ResolveCollision is set to this static delegate then collisions
        /// will result in removing all velocity into the surface.
        /// </summary>
        static public T2DResolveCollisionDelegate ClampCollision
        {
            get { return _clampCollision; }
        }



        /// <summary>
        /// If ResolveCollision is set to this static delegate then collisions
        /// will be rigid body collisions.  The physics material will be used
        /// to determine parameters of the collision response.
        /// </summary>
        static public T2DResolveCollisionDelegate RigidCollision
        {
            get { return _rigidCollision; }
        }



        /// <summary>
        /// If ResolveCollision is set to this static delegate then collisions
        /// will result in velocity and angular velocity being set to zero.
        /// </summary>
        static public T2DResolveCollisionDelegate StickyCollision
        {
            get { return _stickyCollision; }
        }



        /// <summary>
        /// If ResolveCollision is set to this static delegate then collisions
        /// will result in Unregister being called on the owning SceneObject.
        /// </summary>
        static public T2DResolveCollisionDelegate KillCollision
        {
            get { return _killCollision; }
        }



        /// <summary>
        /// The physics material that will be used in all collisions which
        /// don't otherwise specify a physics material.
        /// </summary>
        static public T2DCollisionMaterial DefaultCollisionMaterial
        {
            get { return _DefaultCollisionMaterial; }
            set { if (value != null)_DefaultCollisionMaterial = value; }
        }

        #endregion


        #region Public methods

#if DEBUG
        static private ProfilerCodeBlock Profiler_ProcessTick = new ProfilerCodeBlock("T2DPhysicsComponent.ProcessTick");
#endif



        public void ProcessTick(Move move, float dt)
        {
#if DEBUG
            Profiler.Instance.StartBlock(Profiler_ProcessTick);
#endif
            T2DSceneObject sceneObject = SceneObject;
            sceneObject.StartTick();

            if (sceneObject.IsMounted)
            {
                // scene object handles mount behavior
                if (AngularVelocity != 0.0f)
                    sceneObject.SetRotation(sceneObject.Rotation + AngularVelocity * dt, false);

                // slam this component with it's mount parent's velocity values
                if (sceneObject.MountedTo != null && sceneObject.MountedTo.Physics != null)
                {
                    Velocity = SceneObject.MountedTo.Physics.Velocity;
                    if (sceneObject.TrackMountRotation)
                        AngularVelocity = sceneObject.MountedTo.Physics.AngularVelocity;
                }

#if DEBUG
                Profiler.Instance.EndBlock(Profiler_ProcessTick);
#endif
                return;
            }

            // apply forces for this turn
            if (_forceGenerators != null)
            {
                for (int i = 0; i < _forceGenerators.Count; i++)
                {
                    _forceGenerators[i].Wrap.PreUpdateForces(move, dt);
                }
            }

            bool atRest = false;
            if (!_processCollisionsAtRest && Math.Abs(_velocity.X) < Epsilon.Value && Math.Abs(_velocity.Y) < Epsilon.Value && Math.Abs(_angularVelocity) < Epsilon.Value)
            {
                // clamp velocity to zero and short cut collision checking.
                _velocity.X = 0.0f;
                _velocity.Y = 0.0f;
                _angularVelocity = 0.0f;
                atRest = true;
            }

            int iterations = 5;
            while (iterations-- > 0 && dt > 0.00001f)
            {
                float firstCollideTime = dt;

                _collisions.Clear();

                // only check collisions if we haven't already exhausted all checking...just move if we can't
                // resolve (note: this gives objects every opportunity to behave properly but still prevents
                // them from getting stuck...which is worse than minor tunneling/penetration issues).
                if (iterations > 0)
                {
                    if (sceneObject.Collision != null && !atRest)
                        sceneObject.Collision.TestMove(ref firstCollideTime, _velocity, _collisions);
                    else if (!atRest && sceneObject.WorldLimit != null)
                        sceneObject.WorldLimit.TestMove(ref firstCollideTime, _velocity, _collisions);
                }

                if (firstCollideTime > 0.0f && _collisions.Count != 0)
                {
                    // back off a little pre-collision
                    firstCollideTime = MathHelper.Max(firstCollideTime - 0.001f, 0.0f);
                }

                if (firstCollideTime > 0.0f)
                {
                    // integrate position and rotation
                    if (!atRest)
                    {
                        sceneObject.SetPosition(sceneObject.Position + Velocity * firstCollideTime, false);
                        // Note: this is different than TGB.  In TGB, don't rotate if we collide.
                        sceneObject.SetRotation((sceneObject.Rotation + AngularVelocity * firstCollideTime + 360.0f) % 360.0f, false);
                    }
                }
                else if (sceneObject.Collision == null || sceneObject.Collision.SolveOverlap)
                {
                    Vector2 penetration = new Vector2();

                    // need to resolve overlap
                    for (int i = 0; i < _collisions.Count; i++)
                    {
                        float pushScale = 1.05f;
                        float ourInvMass = InverseMass;
                        float theirInvMass = 0.0f;
                        T2DSceneObject theirObj = _collisions[i].SceneObject;
                        if (theirObj != null)
                        {
                            if (theirObj.Collision != null && !theirObj.Collision.SolveOverlap)
                                // if they don't solve overlap, don't solve overlap even though it's our turn
                                continue;
                            if (theirObj.Physics != null)
                                theirInvMass = theirObj.Physics.InverseMass;
                        }

                        if (ourInvMass > Epsilon.Value || theirInvMass < Epsilon.Value)
                        {
                            // displace us -- either we're movable or they're immovable
                            Vector2 addPenetration = pushScale * _collisions[i].Penetration;
                            float len2 = penetration.LengthSquared();
                            // If we have multiple contacts we need to be careful about adding up sources of
                            // penetration.  In the following we make sure not to accumulate the same penetration
                            // multiple times.
                            if (len2 > Epsilon.Value)
                                addPenetration -= (Vector2.Dot(penetration, addPenetration) / len2) * penetration;
                            penetration += addPenetration;
                        }
                        else // (ourInvMass <= Epsilon.Value && theirInvMass >= Epsilon.Value)
                        {
                            // displace them -- special case in which we're immovable and we ram something which isn't
                            theirObj.Position = theirObj.Position - pushScale * _collisions[i].Penetration;
                        }
                    }
                    sceneObject.SetPosition(sceneObject.Position + penetration, false);
                }

                // allow force generators to apply forces for elapsed time
                if (_forceGenerators != null)
                {
                    for (int i = 0; i < _forceGenerators.Count; i++)
                    {
                        // how do we handle force generator that moves object rather than 
                        // pushes it?  undo work? convert to 1 time velocity?
                        _forceGenerators[i].Wrap.PostUpdateForces(move, firstCollideTime);
                    }
                }

                _ResolveCollisions(_collisions);
                atRest = false;

                // get rid of objects marked for delete so that
                // we don't run into things that are going away
                if (Owner != null)
                    sceneObject.Manager.DeleteMarkedObjects();

                if (Owner == null)
                {
#if DEBUG
                    Profiler.Instance.EndBlock(Profiler_ProcessTick);
#endif
                    // deleted ourself... get out now
                    return;
                }

                dt -= firstCollideTime;
            }

            sceneObject.UpdateSpatialData();

#if DEBUG
            Profiler.Instance.EndBlock(Profiler_ProcessTick);
#endif
        }



        public void InterpolateTick(float k)
        {
        }



        /// <summary>
        /// Apply an impulse to object at the given offset.  Mass and RotationalInertia will be used to 
        /// determine the result of the impulse.  Note that Impulse = Force * Time.
        /// </summary>
        /// <param name="impulse">Impulse to apply.</param>
        /// <param name="offset">Offset to apply force, in object space.</param>
        public void ApplyImpulse(Vector2 impulse, Vector2 offset)
        {
            Velocity += impulse * InverseMass;
            AngularVelocity += MathHelper.ToDegrees(Collision2D.PerpDot(offset, impulse) * InverseRotationalInertia);
        }



        /// <summary>
        /// Apply an impulse to object at the center of the object.  Mass will be used to determine the result
        /// of the impulse.  Note that Impulse = Force * Time.
        /// </summary>
        /// <param name="impulse"></param>
        public void ApplyImpulse(Vector2 impulse)
        {
            Velocity += impulse * InverseMass;
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DPhysicsComponent obj2 = (T2DPhysicsComponent)obj;
            obj2.Velocity = Velocity;
            obj2.AngularVelocity = AngularVelocity;
            obj2.InverseMass = InverseMass;
            obj2.RotationScale = RotationScale;
            obj2.ProcessCollisionsAtRest = ProcessCollisionsAtRest;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            // Add movement due to physics component just after default order.
            ProcessList.Instance.AddTickCallback(owner, this, 0.6f);

            // first check to see if there are any force generators
            if (owner.Components.GetInterface<TorqueInterfaceWrap<IT2DForceGenerator>>("force", String.Empty) != null)
            {
                if (_forceGenerators == null)
                    _forceGenerators = new List<TorqueInterfaceWrap<IT2DForceGenerator>>();
                _forceGenerators.Clear();
                owner.Components.GetInterfaceList<TorqueInterfaceWrap<IT2DForceGenerator>>("force", String.Empty, _forceGenerators);
            }

            return true;
        }

        protected override void _OnUnregister()
        {
            ProcessList.Instance.RemoveObject(Owner);
            base._OnUnregister();
        }



        protected override void _RegisterInterfaces(TorqueObject owner)
        {
            base._RegisterInterfaces(owner);
        }



        protected void _ResolveCollisions(List<T2DCollisionInfo> list)
        {
            T2DCollisionMaterial ourPhysicsMaterial = null;
            if (SceneObject.Collision != null)
                ourPhysicsMaterial = SceneObject.Collision.CollisionMaterial;
            if (ourPhysicsMaterial == null)
                ourPhysicsMaterial = DefaultCollisionMaterial;

            for (int i = 0; i < list.Count; i++)
            {
                T2DCollisionInfo info = list[i];

                if (info.SceneObject == null)
                {
                    // if scene object null, then this is a world limit collision
                    if (SceneObject.WorldLimit != null)
                        SceneObject.WorldLimit.ResolveWorldLimitCollision(info);
                    continue;
                }
                Assert.Fatal(SceneObject != null, "Should not destroy object inside resolve collision");

                if (info.SceneObject.IsUnregistered)
                    // if callback deleted other object already, don't process collisions
                    continue;

                // our default resolve routine
                T2DResolveCollisionDelegate ourResolve = SceneObject.Collision.ResolveCollision;
                T2DOnCollisionDelegate onCollision = SceneObject.Collision.OnCollision;
                T2DCollisionMaterial physicsMaterial = ourPhysicsMaterial;

                if (info.SceneObject.Collision.CollisionMaterial != null)
                    if (ourPhysicsMaterial.Priority < info.SceneObject.Collision.CollisionMaterial.Priority)
                        physicsMaterial = info.SceneObject.Collision.CollisionMaterial;

                // on collision, on resolve...
                if (onCollision != null)
                    onCollision(SceneObject, info.SceneObject, info, ref ourResolve, ref physicsMaterial);
                Assert.Fatal(physicsMaterial != null, "OnCollision nulled out physics material");

                // if callback deleted other object already, don't continue to process collisions
                // Note: this short circuits the collision resolution and may lead to inconsistent
                // behavoir, but we don't want to crash on it either.  Perhaps we should assert instead
                // and force the user to use MarkForDelete?
                if (info.SceneObject.IsUnregistered)
                    continue;
                if (SceneObject == null)
                    break;

                // now find reversed collision info
                T2DResolveCollisionDelegate theirResolve = info.SceneObject.Collision.ResolveCollision;
                T2DCollisionInfo otherInfo = info;
                otherInfo.Normal = -info.Normal;
                otherInfo.SceneObject = SceneObject;
                otherInfo.Penetration = -info.Penetration;
                otherInfo.MaterialIndex = 0; // dont' support multiple materials on colliding object?

                if (info.SceneObject.Collision.OnCollision != null)
                {
                    info.SceneObject.Collision.OnCollision(info.SceneObject, SceneObject, otherInfo, ref theirResolve, ref physicsMaterial);
                    Assert.Fatal(physicsMaterial != null, "OnCollision nulled out physics material");
                }

                // if callback deleted other object already, don't continue to process collisions
                // Note: this short circuits the collision resolution and may lead to inconsistent
                // behavoir, but we don't want to crash on it either.  Perhaps we should assert instead
                // and force the user to use MarkForDelete?
                if (info.SceneObject.IsUnregistered)
                    continue;
                if (SceneObject == null)
                    break;

                if (ourResolve == theirResolve && ourResolve != null)
                    ourResolve(SceneObject, info.SceneObject, ref info, physicsMaterial, true);
                else
                {
                    if (ourResolve != null)
                        ourResolve(SceneObject, info.SceneObject, ref info, physicsMaterial, false);
                    if (theirResolve != null)
                        theirResolve(info.SceneObject, SceneObject, ref otherInfo, physicsMaterial, false);
                }

                Assert.Fatal(SceneObject != null, "Should not destroy object inside resolve collision");
            }
        }

        #region Stock collision response methods

        static protected void _resolveBounce(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            if (ourObject.Physics != null)
            {
                // are we already bouncing away?
                float dot = Vector2.Dot(ourObject.Physics.Velocity, info.Normal);
                if (dot < 0.0f)
                    ourObject.Physics.Velocity = ourObject.Physics.Velocity - (1.0f + physicsMaterial.Restitution) * dot * info.Normal;
            }
            if (handleBoth && theirObject.Physics != null)
            {
                // are they already bouncing away?
                float dot = Vector2.Dot(theirObject.Physics.Velocity, -info.Normal);
                if (dot < 0.0f)
                    theirObject.Physics.Velocity = theirObject.Physics.Velocity + (1.0f + physicsMaterial.Restitution) * dot * info.Normal;
            }
        }



        static protected void _resolveClamp(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            if (ourObject.Physics != null)
            {
                float dot = Vector2.Dot(ourObject.Physics.Velocity, info.Normal);
                if (dot < 0.0f)
                    ourObject.Physics.Velocity = ourObject.Physics.Velocity - dot * info.Normal;
            }
            if (handleBoth && theirObject.Physics != null)
            {
                float dot = Vector2.Dot(theirObject.Physics.Velocity, -info.Normal);
                if (dot < 0.0f)
                    theirObject.Physics.Velocity = theirObject.Physics.Velocity + dot * info.Normal;
            }
        }



        static protected void _resolveRigid(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            Assert.Fatal(ourObject.Physics != null, "Cannot use rigid collision response without physics component");
            Assert.Fatal((theirObject != null && theirObject.Physics != null) || !handleBoth, "Cannot use rigid collision response without physics component");

            //------------------------------------------------------------------------------------------------------
            // Fetch information.
            //------------------------------------------------------------------------------------------------------

            // collision normal
            Vector2 normal = -info.Normal;

            // Positions
            Vector2 srcPosition = ourObject.Position;
            Vector2 dstPosition = theirObject != null ? theirObject.Position : Vector2.Zero;

            // Velocities
            Vector2 srcVelocity = ourObject.Physics.Velocity;
            Vector2 dstVelocity = Vector2.Zero;

            // Angular Velocities.
            float srcAngularVelocity = MathHelper.ToRadians(-ourObject.Physics.AngularVelocity);
            float dstAngularVelocity = 0.0f;

            // Friction/Restitution.
            float friction = physicsMaterial.Friction;
            float restitution = physicsMaterial.Restitution;

            // Inverse Masses.
            float srcInverseMass = ourObject.Physics.InverseMass;
            float dstInverseMass = 0.0f;

            // Inverse Inertial Moments.
            float srcInverseInertialMoment = ourObject.Physics.InverseRotationalInertia;
            float dstInverseInertialMoment = 0.0f;

            if (theirObject != null && theirObject.Physics != null)
            {
                dstVelocity = theirObject.Physics.Velocity;
                dstAngularVelocity = MathHelper.ToRadians(-theirObject.Physics.AngularVelocity);
                dstInverseMass = theirObject.Physics.InverseMass;
                dstInverseInertialMoment = theirObject.Physics.InverseRotationalInertia;
            }

            //------------------------------------------------------------------------------------------------------
            // Contact info.
            //------------------------------------------------------------------------------------------------------

            // Contact Velocity.
            Vector2 srcContactDelta = info.Position - srcPosition;
            Vector2 dstContactDelta = info.Position - dstPosition;
            Vector2 srcContactDeltaPerp = new Vector2(-srcContactDelta.Y, srcContactDelta.X);
            Vector2 dstContactDeltaPerp = new Vector2(-dstContactDelta.Y, dstContactDelta.X);
            Vector2 srcVP = srcVelocity - srcAngularVelocity * srcContactDeltaPerp;
            Vector2 dstVP = dstVelocity - dstAngularVelocity * dstContactDeltaPerp;

            //------------------------------------------------------------------------------------------------------
            // Calculate Impact Velocity.
            //------------------------------------------------------------------------------------------------------
            Vector2 deltaImpactVelocity = dstVP - srcVP;
            float deltaVelocityDot = Vector2.Dot(deltaImpactVelocity, normal);

            // Are we seperated?
            if (deltaVelocityDot > 0.0f)
                // Yes, so no interaction!
                return;

            // Normalise velocity.
            Vector2 Vn = deltaVelocityDot * normal;
            Vector2 Vt = deltaImpactVelocity - Vn;
            float vt = Vt.Length();
            Vector2 VtDir = Vt;
            if (vt > 0.0f)
                VtDir *= 1.0f / vt;

            //------------------------------------------------------------------------------------------------------
            // Calculate Impulse ( Dynamic-Friction and Restitution )
            //------------------------------------------------------------------------------------------------------

            // Break Impulse Function Down a little...
            float srcRN = Collision2D.PerpDot(srcContactDelta, normal);
            float dstRN = Collision2D.PerpDot(dstContactDelta, normal);
            float t0 = srcRN * srcRN * srcInverseInertialMoment;
            float t1 = dstRN * dstRN * dstInverseInertialMoment;
            float denom = srcInverseMass + dstInverseMass + t0 + t1;
            // handle two non-rotating, non-moving objects colliding (two platforms?)
            float jn = Math.Abs(denom) > Epsilon.Value ? deltaVelocityDot / denom : 0.0f;

            // Calculate Impulse (include restitution/dynamic friction).
            Vector2 impulseForce = ((-(1.0f + restitution) * jn) * normal) + ((friction * jn) * VtDir);

            //------------------------------------------------------------------------------------------------------
            // Changes in Momentum ( Linear and Angular ).
            //------------------------------------------------------------------------------------------------------

            // Calculate Linear Acceleration.
            Vector2 srcLinearDelta = -impulseForce * srcInverseMass;
            Vector2 dstLinearDelta = impulseForce * dstInverseMass;
            // Calculate Angular Acceleration.
            float srcAngularDelta = Collision2D.PerpDot(srcContactDelta, impulseForce) * srcInverseInertialMoment;
            float dstAngularDelta = -Collision2D.PerpDot(dstContactDelta, impulseForce) * dstInverseInertialMoment;

            //------------------------------------------------------------------------------------------------------
            // Finally, apply acceleration.
            //------------------------------------------------------------------------------------------------------

            ourObject.Physics.Velocity += srcLinearDelta;
            ourObject.Physics.AngularVelocity -= MathHelper.ToDegrees(srcAngularDelta);
            if (handleBoth)
            {
                theirObject.Physics.Velocity += dstLinearDelta;
                theirObject.Physics.AngularVelocity -= MathHelper.ToDegrees(dstAngularDelta);
            }
        }



        static protected void _resolveSticky(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            if (ourObject.Physics != null)
            {
                ourObject.Physics.Velocity = new Vector2(0, 0);
                ourObject.Physics.AngularVelocity = 0;
            }
            if (handleBoth && theirObject.Physics != null)
            {
                theirObject.Physics.Velocity = new Vector2(0, 0);
                theirObject.Physics.AngularVelocity = 0;
            }
        }



        static protected void _resolveKill(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            ourObject.MarkForDelete = true;
            if (handleBoth)
                theirObject.MarkForDelete = true;
        }

        #endregion

        #endregion


        #region Private, protected, internal fields

        protected Vector2 _velocity;
        protected float _angularVelocity;
        protected float _inverseMass = 1.0f;
        protected float _rotationalScale = 1.0f;
        protected bool _processCollisionsAtRest = false;

        List<TorqueInterfaceWrap<IT2DForceGenerator>> _forceGenerators;

        static protected T2DResolveCollisionDelegate _bounceCollision = new T2DResolveCollisionDelegate(_resolveBounce);
        static protected T2DResolveCollisionDelegate _clampCollision = new T2DResolveCollisionDelegate(_resolveClamp);
        static protected T2DResolveCollisionDelegate _rigidCollision = new T2DResolveCollisionDelegate(_resolveRigid);
        static protected T2DResolveCollisionDelegate _stickyCollision = new T2DResolveCollisionDelegate(_resolveSticky);
        static protected T2DResolveCollisionDelegate _killCollision = new T2DResolveCollisionDelegate(_resolveKill);
        static T2DCollisionMaterial _DefaultCollisionMaterial = new T2DCollisionMaterial(0.5f, 0.0f, 0.0f);

        static protected List<T2DCollisionInfo> _collisions = new List<T2DCollisionInfo>();

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            base.Dispose();
        }

        #endregion
    }
}
