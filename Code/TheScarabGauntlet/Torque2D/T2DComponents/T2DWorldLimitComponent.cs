//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Add this component to a T2DSceneObject to limit the range over which it can move.
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaDependency(Type = typeof(T2DCollisionComponent))]
    public class T2DWorldLimitComponent : TorqueComponent, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The minimum coordinates the T2DSceneObject can move to.
        /// </summary>
        public Vector2 MoveLimitMin
        {
            get { return _moveLimitMin; }
            set { _moveLimitMin = value; }
        }



        /// <summary>
        /// The maximum coordinates the T2DSceneObject can move to.
        /// </summary>
        public Vector2 MoveLimitMax
        {
            get { return _moveLimitMax; }
            set { _moveLimitMax = value; }
        }



        /// <summary>
        /// The resolve collision delegate to use if the T2DSceneObject
        /// collides against the move limit.
        /// </summary>
        public T2DResolveCollisionDelegate WorldLimitResolveCollision
        {
            get { return _worldLimitResolveCollision; }
            set { _worldLimitResolveCollision = value; }
        }



        /// <summary>
        /// Delegate called when the move limit is reached.
        /// </summary>
        public T2DOnCollisionDelegate OnWorldLimit
        {
            get { return _onWorldLimit; }
            set { _onWorldLimit = value; }
        }



        /// <summary>
        /// The owning T2DSceneObject.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Test to see if SceneObject can move from current position along given velocity for given amount of time
        /// without colliding with world limits.
        /// </summary>
        /// <param name="dt">Duration to move object, in seconds.</param>
        /// <param name="searchBox">Box containing entire swept path of object bounds.  Used to short cut world bounds check.</param>
        /// <param name="velocity">Velocity of object.</param>
        /// <param name="collider">Collision component of object.</param>
        /// <param name="collisions">List of collisions encountered during move.</param>
        public void TestMove(ref float dt, RectangleF searchBox, Vector2 velocity, T2DCollisionComponent collider, List<T2DCollisionInfo> collisions)
        {
            if (WorldLimitResolveCollision != null || OnWorldLimit != null)
            {
                ReadOnlyArray<T2DCollisionImage> images = SceneObject.Collision.Images;
                for (int i = 0; i < images.Count; i++)
                {
                    T2DCollisionImage image = images[i];

                    if (searchBox.X + searchBox.Width > _moveLimitMax.X)
                    {
                        // test against max x
                        _worldLimitPoly[0] = new Vector2(_moveLimitMax.X, _moveLimitMin.Y - 5.0f);
                        _worldLimitPoly[1] = new Vector2(_moveLimitMax.X, _moveLimitMax.Y + 5.0f);
                        _worldLimitImage.CollisionPolyBasis = _worldLimitPoly;
                        image.TestMove(ref dt, velocity, _worldLimitImage, collisions);
                    }
                    if (searchBox.X < _moveLimitMin.X)
                    {
                        // test against min x
                        _worldLimitPoly[0] = new Vector2(_moveLimitMin.X, _moveLimitMax.Y + 5.0f);
                        _worldLimitPoly[1] = new Vector2(_moveLimitMin.X, _moveLimitMin.Y - 5.0f);
                        _worldLimitImage.CollisionPolyBasis = _worldLimitPoly;
                        image.TestMove(ref dt, velocity, _worldLimitImage, collisions);
                    }
                    if (searchBox.Y + searchBox.Height > _moveLimitMax.Y)
                    {
                        // test against max y
                        _worldLimitPoly[0] = new Vector2(_moveLimitMax.X + 5.0f, _moveLimitMax.Y);
                        _worldLimitPoly[1] = new Vector2(_moveLimitMin.X - 5.0f, _moveLimitMax.Y);
                        _worldLimitImage.CollisionPolyBasis = _worldLimitPoly;
                        image.TestMove(ref dt, velocity, _worldLimitImage, collisions);
                    }
                    if (searchBox.Y < _moveLimitMin.Y)
                    {
                        // test against min y
                        _worldLimitPoly[0] = new Vector2(_moveLimitMin.X - 5.0f, _moveLimitMin.Y);
                        _worldLimitPoly[1] = new Vector2(_moveLimitMax.X + 5.0f, _moveLimitMin.Y);
                        _worldLimitImage.CollisionPolyBasis = _worldLimitPoly;
                        image.TestMove(ref dt, velocity, _worldLimitImage, collisions);
                    }
                }
            }
        }



        /// <summary>
        /// Test to see if SceneObject can move from current position along given velocity for given amount of time
        /// without colliding with world limits.
        /// </summary>
        /// <param name="dt">Duration to move object, in seconds.</param>
        /// <param name="velocity">Velocity of object.</param>
        /// <param name="collisions">List of collisions encountered during move.</param>
        public void TestMove(ref float dt, Vector2 velocity, List<T2DCollisionInfo> collisions)
        {
            // search world at our position extended by our velocity
            RectangleF searchBox = SceneObject.WorldCollisionClipRectangle;
            float vx = velocity.X * (dt + 0.001f);
            if (vx > 0.0f)
                searchBox.Width += vx;
            else
            {
                searchBox.X += vx;
                searchBox.Width -= vx;
            }
            float vy = velocity.Y * (dt + 0.001f);
            if (vy > 0.0f)
                searchBox.Height += vy;
            else
            {
                searchBox.Y += vy;
                searchBox.Height -= vy;
            }
            float extend = 0.01f;
            searchBox.X -= extend;
            searchBox.Width += 2.0f * extend;
            searchBox.Y -= extend;
            searchBox.Height += 2.0f * extend;

            TestMove(ref dt, searchBox, velocity, SceneObject.Collision, collisions);
        }



        /// <summary>
        /// Resolve world limit collision.  Typically called by T2DPhysicsComponent.
        /// </summary>
        /// <param name="info">Current collision information.</param>
        public void ResolveWorldLimitCollision(T2DCollisionInfo info)
        {
            T2DResolveCollisionDelegate resolve = WorldLimitResolveCollision;
            T2DOnCollisionDelegate onCollision = OnWorldLimit;
            T2DCollisionMaterial physicsMaterial = T2DPhysicsComponent.DefaultCollisionMaterial;
            if (SceneObject.Collision != null && SceneObject.Collision.CollisionMaterial != null)
                physicsMaterial = SceneObject.Collision.CollisionMaterial;

            // on collision, on resolve...
            if (onCollision != null)
                onCollision(SceneObject, null, info, ref resolve, ref physicsMaterial);
            Assert.Fatal(physicsMaterial != null, "OnWorldLimitCollision nulled out physics material");
            if (resolve != null)
                resolve(SceneObject, null, ref info, physicsMaterial, false);
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            T2DWorldLimitComponent obj2 = (T2DWorldLimitComponent)obj;

            obj2.MoveLimitMin = MoveLimitMin;
            obj2.MoveLimitMax = MoveLimitMax;
            obj2.OnWorldLimit = OnWorldLimit;
            obj2.WorldLimitResolveCollision = WorldLimitResolveCollision;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject) || owner.Components.FindComponent<T2DCollisionComponent>() == null)
                return false;

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        protected Vector2 _moveLimitMin;
        protected Vector2 _moveLimitMax;

        protected T2DResolveCollisionDelegate _worldLimitResolveCollision;
        protected T2DOnCollisionDelegate _onWorldLimit;

        protected static T2DPolyImage _worldLimitImage = new T2DPolyImage();
        protected static Vector2[] _worldLimitPoly = new Vector2[2];

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            _onWorldLimit = null;
            _worldLimitResolveCollision = null;
            base.Dispose();
        }

        #endregion
    }
}
