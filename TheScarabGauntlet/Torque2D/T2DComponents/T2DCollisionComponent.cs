//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GFX;
using GarageGames.Torque.RenderManager;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Information about a collision.
    /// </summary>
    public struct T2DCollisionInfo
    {
        /// <summary>
        /// Position in world-space of collision.
        /// </summary>
        public Vector2 Position;



        /// <summary>
        /// Vector representing amount of penetration achieved on current collision.
        /// </summary>
        public Vector2 Penetration;



        /// <summary>
        /// Normal of surface collided against, in world-space.
        /// </summary>
        public Vector2 Normal;



        /// <summary>
        /// Scene object collided against.
        /// </summary>
        public T2DSceneObject SceneObject;



        /// <summary>
        /// Collision material index of collision surface.  In most cases will be 0.
        /// </summary>
        public int MaterialIndex;
    }



    /// <summary>
    /// Describes a physical surface for collision processing.
    /// </summary>
    public class T2DCollisionMaterial : TorqueBase, IDisposable
    {

        #region Constructors

        public T2DCollisionMaterial()
        {
            _friction = 0.5f;
            _restitution = 0.0f;
            _priority = 0.0f;
        }



        public T2DCollisionMaterial(float restitution, float friction, float priority)
        {
            _friction = MathHelper.Clamp(friction, 0.0f, 1.0f);
            _restitution = MathHelper.Clamp(restitution, 0.0f, 1.0f);
            _priority = priority;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Parameter which determines how much objects bounce off this surface.  A value
        /// of 0 means no bounce, a value of 1 means a full bounce.
        /// </summary>
        public float Restitution
        {
            get { return _restitution; }
            internal set { _restitution = value; } // for deserialization
        }



        /// <summary>
        /// Parameter which determines how frictional this surface is.  A value of 0 means a
        /// frictionless contact.  A value of 1 means that the frictional force will 
        /// equal the downard force.
        /// </summary>
        public float Friction
        {
            get { return _friction; }
            internal set { _friction = value; } // for deserialization
        }



        /// <summary>
        /// In any given collision only one collision material will be used.  The highest priority material will be used.
        /// </summary>
        public float Priority
        {
            get { return _priority; }
            internal set { _priority = value; } // for deserialization
        }

        #endregion


        #region Private, protected, internal fields

        float _friction;
        float _restitution;
        float _priority;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            this._ResetRefs();
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// Called whenever a genuine collision occurs.  The resolve delegate passed in will be the default resolve delegate.  Do not call it from inside the T2DOnCollisionDelegate.  Instead, 
    /// if you wish to use a different resolve delegate then set the new one on exit.  Similarly, the passed T2DCollisionMaterial can be replaced inside the T2DOnCollisionDelegate.
    /// </summary>
    /// <param name="ourObject">First object involved in collision.</param>
    /// <param name="theirObject">Second object involved in collision.</param>
    /// <param name="info">Information about current collision.</param>
    /// <param name="resolve">Resolve delegate.  Set to whatever resolve delegate should be used.</param>
    /// <param name="physicsMaterial">Physics material.  Set to whatever collision material should be used.</param>
    public delegate void T2DOnCollisionDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial);



    /// <summary>
    /// Test whether collision checking should continue between the two objects.  This should only be used between two objects which sometimes collide
    /// and sometimes don't. If two objects never collide, use the CollidesWith property.
    /// </summary>
    /// <param name="ourObject">First object involved in potential collision.</param>
    /// <param name="theirObject">Second object involved in potential collision.</param>
    /// <returns>Return true to early out collision.</returns>
    public delegate bool T2DTestCollisionEarlyOutDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject);



    /// <summary>
    /// Abstract base class for collision 2D images usable by T2DCollisionComponent.
    /// </summary>
    abstract public class T2DCollisionImage : ICloneable, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Priority of this collision image.  The priority is used to determine which of two collision immages
        /// should do the testing when testing against each other.  If the moving object has a higher priority, 
        /// then the TestMove method is used.  If the object being moved against has a higher priority then the
        /// TestMoveAgainst method is used.  Tie goes to the object doing the moving.
        /// </summary>
        abstract public int Priority
        {
            get;
        }



        /// <summary>
        /// The T2DSceneObject which this image belongs to.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return _sceneObject; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Regenerate any internally cached collision values. This should be called if, for instance, a T2DSceneObject this
        /// collision belongs to is resized.
        /// </summary>
        abstract public void MarkCollisionDirty();



        /// <summary>
        /// Test whether this collision image collides with other image when moving at given velocity for given amount of time. 
        /// Collisions are added to 'list'.
        /// </summary>
        /// <param name="dt">Time spent moving, in seconds.</param>
        /// <param name="ourVelocity">Our velocity.</param>
        /// <param name="theirImage">Collision image to test against.</param>
        /// <param name="list">List of collision information to which current collisions will be added to.</param>
        public virtual void TestMove(ref float dt, Vector2 ourVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
        {
            // we'll need to make sure theirCollider is of the right type here...and maybe switch based on one of a few types

            // base collider component never collides with anything
        }



        /// <summary>
        /// Test whether other collision image collides with this image when moving at given velocity for given amount of time.
        /// Collisions are added to 'list'.
        /// </summary>
        /// <param name="dt">Time spent moving, in seconds.</param>
        /// <param name="theirVelocity">Other image velocity.</param>
        /// <param name="theirImage">Collision image to test against</param>
        /// <param name="list">List of collision information to which current collisions will be added to.</param>
        public virtual void TestMoveAgainst(ref float dt, Vector2 theirVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
        {
            // we'll need to make sure theirCollider is of the right type here...and maybe switch based on one of a few types

            // base collider component never collides with anything
        }



        /// <summary>
        /// Render position of last collision point and normal.  Strictly for debug purposes.  Should be called by 
        /// derived class.
        /// </summary>
        /// <param name="objToWorld">Current render transform.</param>
        /// <param name="srs">Scene render state passed in for rendering purposes.</param>
        public virtual void RenderBounds(Matrix objToWorld, SceneRenderState srs)
        {
            if (_keepCollision > 0)
            {
                _keepCollision--;
                GraphicsDevice d3d = srs.Gfx.Device;

                int numVerts = 6;

                GarageGames.Torque.Materials.SimpleMaterial effect = new GarageGames.Torque.Materials.SimpleMaterial();

                // create VB 
                int sizeInBytes = numVerts * GFXVertexFormat.VertexSize;
                VertexBuffer vb = new VertexBuffer(GFXDevice.Instance.Device, sizeInBytes, BufferUsage.WriteOnly);

                // fill VB

                Color faceColor = Color.Red;

                GFXVertexFormat.PCTTBN[] vertices = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(numVerts);
                for (int i = 0; i < numVerts; ++i)
                    vertices[i].Color = faceColor;

                float d = 0.5f;
                vertices[0].Position = new Vector3(_lastCollisionPoint.X - d, _lastCollisionPoint.Y - d, 0.0f);
                vertices[1].Position = new Vector3(_lastCollisionPoint.X + d, _lastCollisionPoint.Y + d, 0.0f);
                vertices[2].Position = new Vector3(_lastCollisionPoint.X - d, _lastCollisionPoint.Y + d, 0.0f);
                vertices[3].Position = new Vector3(_lastCollisionPoint.X + d, _lastCollisionPoint.Y - d, 0.0f);
                vertices[4].Position = new Vector3(_lastCollisionPoint.X, _lastCollisionPoint.Y, 0.0f);
                vertices[5].Position = new Vector3(_lastCollisionPoint.X + _lastCollisionNormal.X * 2.0f, _lastCollisionPoint.Y + _lastCollisionNormal.Y * 2.0f, 0.0f);

                vb.SetData<GFXVertexFormat.PCTTBN>(vertices, 0, numVerts);

                RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();
                ri.Type = RenderInstance.RenderInstanceType.Mesh2D;
                ri.ObjectTransform = Matrix.Identity;
                ri.VertexBuffer = vb;
                ri.PrimitiveType = PrimitiveType.LineList;
                ri.VertexSize = GFXVertexFormat.VertexSize;
                ri.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(d3d);
                ri.BaseVertex = 0;
                ri.PrimitiveCount = 3;
                ri.VertexCount = numVerts;
                ri.UTextureAddressMode = TextureAddressMode.Clamp;
                ri.VTextureAddressMode = TextureAddressMode.Clamp;

                ri.Material = effect;
                SceneRenderer.RenderManager.AddInstance(ri);
            }
        }



        public virtual object Clone()
        {
            return null;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Derived classes should add collision points to 'list' using this method, which guarantees that duplicates are not added and that the list is proprly
        /// reset when earlier collisions are detected.
        /// </summary>
        protected void _AddCollisionPoint(ref float dt, float collisionTime, Vector2 pos, Vector2 norm, Vector2 penetration, T2DSceneObject obj, int matidx, List<T2DCollisionInfo> list)
        {
            float MIN_COINCIDENT_TIME = 0.0005f;
            if (collisionTime < 0.0f)
                collisionTime = 0.0f;
            if (collisionTime < dt + MIN_COINCIDENT_TIME)
            {
                if (collisionTime < dt - MIN_COINCIDENT_TIME)
                {
                    // prior collision, restart list and decrease dt
                    list.Clear();
                    dt = collisionTime;
                }

                // add collision to list
                T2DCollisionInfo info = new T2DCollisionInfo();
                info.Position = pos;
                info.Normal = norm;
                info.Penetration = penetration;
                info.SceneObject = obj;
                info.MaterialIndex = matidx;
                list.Add(info);

                // save some debug info
                _keepCollision = 1000; // 1000 frames
                _lastCollisionPoint = pos;
                _lastCollisionNormal = norm;
            }
        }

        #endregion


        #region Private, protected, internal fields

        internal T2DSceneObject _sceneObject;

        // debug info for rendering collision point
        int _keepCollision; // how many frames
        Vector2 _lastCollisionPoint;
        Vector2 _lastCollisionNormal;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _sceneObject = null;
        }

        #endregion
    }



    /// <summary>
    /// Collision image representing a polygon.
    /// </summary>
    public class T2DPolyImage : T2DCollisionImage, IDisposable
    {

        #region Constructors

        public T2DPolyImage()
        {
        }



        public T2DPolyImage(T2DSceneObject obj)
        {
            _sceneObject = obj;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public override int Priority
        {
            get { return 0; }
        }



        /// <summary>
        /// Polygon used for collision checking.  This is CollisionPolyBasis scaled by SceneObject.Size.
        /// </summary>
        public Vector2[] CollisionPoly
        {
            get
            {
                if (_collisionPolyDirty)
                    _GenerateCollisionPoly();
                return _collisionPoly;
            }
        }



        /// <summary>
        /// Polygon with coordinates scaled between -1 and 1.  Actual polygon used for collision
        /// checking can be found using the CollisionPoly property.  That polygon is determined
        /// from this one by scaling coordinates by SceneObject.Size.
        /// </summary>
        public Vector2[] CollisionPolyBasis
        {
            get { return _collisionBasis; }
            set { _collisionBasis = value; _collisionPolyDirty = true; }
        }



        /// <summary>
        /// If true, CollisionPolyBasis rather than CollisionPoly is used for collision checking.
        /// </summary>
        internal bool UseCollisionBasisRaw
        {
            get { return _useCollisionBasisRaw; }
            set { _useCollisionBasisRaw = value; }
        }



        /// <summary>
        /// Set this property to create a collision primitive with the given
        /// number of vertices.  Setting this property will create a symmetrical
        /// poly with the given number of sides.
        /// </summary>
        public int CollisionPolyPrimitive
        {
            set
            {
                int vertexCount = value;
                if (vertexCount < 3)
                    vertexCount = 3;
                _collisionBasis = new Vector2[vertexCount];
                if (vertexCount == 4)
                {
                    // define a quad poly basis list
                    _collisionBasis[0] = new Vector2(-1.0f, -1.0f);
                    _collisionBasis[1] = new Vector2(1.0f, -1.0f);
                    _collisionBasis[2] = new Vector2(1.0f, 1.0f);
                    _collisionBasis[3] = new Vector2(-1.0f, 1.0f);
                }
                else
                {
                    float angle = (float)Math.PI / vertexCount;
                    float angleStep = (float)(2.0f * Math.PI) / vertexCount;

                    for (int n = 0; n < vertexCount; n++)
                    {
                        angle += angleStep;
                        _collisionBasis[n] = new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
                    }
                }
                _collisionPolyDirty = true;
            }
            internal get { return _collisionBasis.Length; }  // for XML deserialization purposes
        }



        /// <summary>
        /// Scale collision polygon.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1 1")]
        public Vector2 CollisionPolyScale
        {
            get { return _collisionPolyScale; }
            set
            {
                Assert.Fatal(value.X > 0.0f && value.Y > 0.0f, "Polygon Scales must be greater than zero!");
                Assert.Fatal(value.X <= 1.0f && value.Y <= 1.0f, "Polygon Scales cannot be greater than one!");
                _collisionPolyScale = value;
                _collisionPolyDirty = true;

            }
        }

        #endregion


        #region Public methods

        public override void MarkCollisionDirty()
        {
            // mark us dirty so we can recalculate our collision poly list
            _collisionPolyDirty = true;
        }



        public override void TestMove(ref float dt, Vector2 ourVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
        {
            // we'll need to make sure theirCollider is of the right type here...and maybe switch based on one of a few types
            T2DPolyImage theirPoly = theirImage as T2DPolyImage;
            Assert.Fatal(theirPoly != null, "T2DPolyImage.TestMove: don't know how to test against this image type");

            Vector2 pos = Vector2.Zero;
            float rot = 0.0f;
            if (SceneObject != null)
            {
                pos = SceneObject.Position;
                rot = SceneObject.Rotation;
            }
            Vector2 theirPos = Vector2.Zero;
            float theirRot = 0.0f;
            Vector2 theirVel = Vector2.Zero;
            if (theirImage.SceneObject != null)
            {
                theirPos = theirImage.SceneObject.Position;
                theirRot = theirImage.SceneObject.Rotation;
                if (theirImage.SceneObject.Physics != null && Vector2.Dot(theirPos - pos, theirImage.SceneObject.Physics.Velocity) > 0.0f)
                    // only use their velocity if they are moving away from us (i.e., if this can prevent a collision)
                    theirVel = theirImage.SceneObject.Physics.Velocity;
            }
            float collisionTime = 0.0f;
            Vector2 collisionNormal = Vector2.Zero;
            Vector2 collisionPoint = Vector2.Zero;
            Vector2 collisionPenetration = Vector2.Zero;
            bool collisionTest = Collision2D.IntersectMovingPolyPoly(
                    dt, CollisionPoly, theirPoly.CollisionPoly,
                    pos, theirPos,
                    rot, theirRot,
                    ourVelocity, theirVel,
                    ref collisionPoint, ref collisionNormal, ref collisionPenetration, ref collisionTime);

            if (collisionTest)
                _AddCollisionPoint(ref dt, collisionTime, collisionPoint, collisionNormal, collisionPenetration, theirImage.SceneObject, 0, list);
        }



        public override void TestMoveAgainst(ref float dt, Vector2 theirVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
        {
            // we'll need to make sure theirCollider is of the right type here...and maybe switch based on one of a few types

            // We don't know how to collide with other types, so if we get here no collision...or should we assert?
        }



        public override object Clone()
        {
            T2DPolyImage poly = new T2DPolyImage(); ;
            poly.CollisionPolyBasis = CollisionPolyBasis;
            poly.CollisionPolyScale = CollisionPolyScale;
            poly.UseCollisionBasisRaw = UseCollisionBasisRaw;
            return poly;
        }



        /// <summary>
        /// Render bounds of polygon render image.  Typically called from T2DSceneObject.Render.
        /// </summary>
        /// <param name="objToWorld">Current render transform.</param>
        /// <param name="srs">Scene render state passed in for rendering purposes.</param>
        public override void RenderBounds(Matrix objToWorld, SceneRenderState srs)
        {
            if (_collisionBasis == null)
                return;

            base.RenderBounds(objToWorld, srs);

            _RenderPolygon(objToWorld, srs, CollisionPolyBasis, Color.Blue);
        }

        #endregion


        #region Private, protected, internal methods

        // create a poly list that is scaled and flipped according to the object properties
        protected void _GenerateCollisionPoly()
        {
            if (SceneObject == null || _useCollisionBasisRaw)
            {
                // create poly directly from basis -- this is here for world limit case
                _collisionPoly = _collisionBasis;
                _collisionPolyDirty = false;
                return;
            }

            if (_collisionBasis == null || _collisionBasis.Length < 3)
                // use rectangle if nothing defined
                CollisionPolyPrimitive = 4;

            int vertexCount = _collisionBasis.Length;

            float flipx = SceneObject.FlipX ? -1.0f : 1.0f;
            float flipy = SceneObject.FlipY ? -1.0f : 1.0f;
            Vector2 polyHalfSize = new Vector2(0.5f * flipx * _collisionPolyScale.X * SceneObject.Size.X, 0.5f * flipy * _collisionPolyScale.Y * SceneObject.Size.Y);

            _collisionPoly = new Vector2[vertexCount];

            if (vertexCount > 0)
            {
                bool reversedOrder = SceneObject.FlipX != SceneObject.FlipY;

                if (reversedOrder)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        _collisionPoly[(vertexCount - 1) - i].X = _collisionBasis[i].X * polyHalfSize.X;
                        _collisionPoly[(vertexCount - 1) - i].Y = _collisionBasis[i].Y * polyHalfSize.Y;
                    }
                }
                else
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        _collisionPoly[i].X = _collisionBasis[i].X * polyHalfSize.X;
                        _collisionPoly[i].Y = _collisionBasis[i].Y * polyHalfSize.Y;
                    }
                }
            }
            _collisionPolyDirty = false;
        }



        protected void _RenderPolygon(Matrix objToWorld, SceneRenderState srs, Vector2[] vertices, Color color)
        {
            GraphicsDevice d3d = srs.Gfx.Device;
            Assert.Fatal(d3d != null, "doh");

            Assert.Fatal(vertices.Length > 0, "no vertices to render!");

            int numVerts = vertices.Length + 1;

            // make sure we have a vertex buffer
            if (_vb.IsNull)
            {
                int sizeInBytes = numVerts * GFXVertexFormat.VertexSize;
                _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);
            }

            // deal with flip
            float flipx = SceneObject != null && SceneObject.FlipX ? -1.0f : 1.0f;
            float flipy = SceneObject != null && SceneObject.FlipY ? -1.0f : 1.0f;

            // fill in vertex array 
            GFXVertexFormat.PCTTBN[] pVertices = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(numVerts);
            for (int i = 0; i < numVerts - 1; ++i)
            {
                pVertices[i] = new GFXVertexFormat.PCTTBN();
                pVertices[i].Position = new Vector3(flipx * vertices[i].X, flipy * vertices[i].Y, 0.0f);
                pVertices[i].Color = color;
            }
            pVertices[numVerts - 1] = pVertices[0];

            _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(pVertices, 0, numVerts);


            srs.World.Push();
            srs.World.MultiplyMatrixLocal(objToWorld);


            if (_effect == null)
            {
                _effect = new GarageGames.Torque.Materials.SimpleMaterial();
            }

            RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();
            ri.Type = RenderInstance.RenderInstanceType.Mesh2D;
            ri.ObjectTransform = srs.World.Top;
            ri.VertexBuffer = _vb.Instance;
            ri.PrimitiveType = PrimitiveType.LineStrip;
            ri.VertexSize = GFXVertexFormat.VertexSize;
            ri.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(d3d);
            ri.VertexCount = numVerts;
            ri.BaseVertex = 0;
            ri.PrimitiveCount = numVerts - 1;

            ri.UTextureAddressMode = TextureAddressMode.Clamp;
            ri.VTextureAddressMode = TextureAddressMode.Clamp;

            ri.Material = _effect;
            SceneRenderer.RenderManager.AddInstance(ri);

            srs.World.Pop();
        }

        #endregion


        #region Private, protected, internal fields

        protected Vector2[] _collisionBasis;
        protected Vector2[] _collisionPoly;
        protected Vector2 _collisionPolyScale = new Vector2(1.0f, 1.0f);
        protected bool _collisionPolyDirty = true;
        protected bool _useCollisionBasisRaw;
        protected Resource<DynamicVertexBuffer> _vb;
        protected GarageGames.Torque.Materials.SimpleMaterial _effect;
        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _collisionBasis = null;
            _collisionPoly = null;
            if (!_vb.IsNull)
            {
                _vb.Instance.Dispose();
                _vb.Invalidate();
            }
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// Adding this component to a T2DSceneObject allows it to collide with other
    /// scene objects.  A T2DCollisionComponent can be configured to collide with
    /// only certain other object types (see CollidesWith property), can have one or
    /// more collision images installed (see InstallImage method), and can have collision
    /// callbacks defined (see OnCollision property).
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DCollisionComponent : TorqueComponent, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The owning T2DSceneObject.  SceneObject is used to determine the extent of the collision region
        /// using the T2DSceneObject.Size property.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }



        /// <summary>
        /// If true then collision bounds are drawn by the collision images.  This is strictly a debug feature.
        /// </summary>
        public bool RenderCollisionBounds
        {
            get { return _renderCollisionBounds; }
            set { _renderCollisionBounds = value; }
        }



        /// <summary>
        /// Mask which determines which layers will be collided with.  The layer of an object is determined by
        /// the T2DSceneObject.Layer property.  Because this is a mask, each bit refers to a single layer,
        /// with bit N corresponding to layer N.  A mask of 7, for example, indicates that the object will
        /// collide with objects on layers 1, 2, and 3.
        /// </summary>
        public int CollisionLayerMask
        {
            get { return _collisionLayerMask; }
            set { _collisionLayerMask = value; }
        }



        /// <summary>
        /// Determines which object types (see T2DSceneObject.ObjectType property) to collide with.
        /// TorqueObjectTypes can be combined to form a collection of object types with which to collide.
        /// </summary>
        public TorqueObjectType CollidesWith
        {
            get { return _collidesWith; }
            set { _collidesWith = value; }
        }



        /// <summary>
        /// Determine which object types to test for early out on collision checking.
        /// </summary>
        public TorqueObjectType EarlyOutObjectType
        {
            get { return _earlyOutObjectType; }
            set { _earlyOutObjectType = value; }
        }



        /// <summary>
        /// This delegate is called whenever a collision occurs, possibly multiple times per tick.  OnCollision should not
        /// be used for physical response.  OnCollision should be used for game logic.  Physical response is handled by
        /// a T2DResolveCollisionDelegate (on T2DPhysicsComponent).
        /// </summary>
        public T2DOnCollisionDelegate OnCollision
        {
            get { return _onCollision; }
            set { _onCollision = value; }
        }



        /// <summary>
        /// Delegate used to determine how to respond to collisions.  There are a number of stock options available:
        /// BounceCollision, ClampCollision, RigidCollision, StickyCollision, and KillCollision.  Custom resolve
        /// delegates can also be used.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "GarageGames.Torque.T2D.T2DPhysicsComponent.BounceCollision", IsDefaultValueOf = true)]
        public T2DResolveCollisionDelegate ResolveCollision
        {
            get { return _resolveCollisionDelegate; }
            set { _resolveCollisionDelegate = value; }
        }



        /// <summary>
        /// If false then this object will never solve overlaps.  Overlap between two objects can occur
        /// for a variety of reasons.  Most common is that slight error was introduced during the collision
        /// checking.  Solving the overlap removes accumulated error.  But in some cases you don't want the 
        /// small displacements which can occur because of this.  E.g., projectiles which will be immediately
        /// destroyed do not need to solve the overlap and setting this property to false can get rid of the
        /// slight push that projectiles otherwise might have.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool SolveOverlap
        {
            get { return _solveOverlap; }
            set { _solveOverlap = value; }
        }



        /// <summary>
        /// Collision material to be used in collisions of this object with other objects.
        /// </summary>
        public T2DCollisionMaterial CollisionMaterial
        {
            get { return _collisionMaterial; }
            set { _collisionMaterial = value; }
        }



        /// <summary>
        /// Test whether or not a given object should be collided against.  Use this for objects which sometimes
        /// collide and sometimes don't.  If true is returned, then collision processing does not continue against 
        /// the object.  This can be used to make certain objects have collisions only under certain conditions, 
        /// e.g., when going one way through a platform but not another.  Note that this only affects collision checking
        /// when this object moves into another.  Objects moving into this object do not use our TestEarlyOut delegate.
        /// </summary>
        public T2DTestCollisionEarlyOutDelegate TestEarlyOut
        {
            get { return _testEarlyOut; }
            set { _testEarlyOut = value; }
        }



        /// <summary>
        /// Read only array of collision images.
        /// </summary>
        public ReadOnlyArray<T2DCollisionImage> Images
        {
            get { return new ReadOnlyArray<T2DCollisionImage>(_collisionImages); }
        }



        // private interface for xml deserialization, here because we cannot deserialize ReadOnlyArrays
        [TorqueXmlDeserializeInclude] // needed because this property is internal
        [XmlElement(ElementName = "Images")] // remap the element name Images to this object.
        [XmlArrayItem(Type = typeof(T2DPolyImage))]
        [XmlArrayItem(Type = typeof(GarageGames.Torque.T2D.T2DTileLayer.TileLayerCollisionImage))]
        internal List<T2DCollisionImage> XmlImages
        {
            get { return _collisionImages; }
            set { _collisionImages = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Tells all of the collision images associated with this component to regenerate any internally cached
        /// collision values such as collision poly lists.
        /// </summary>
        public void MarkCollisionDirty()
        {
            for (int i = 0; i < _collisionImages.Count; i++)
                _collisionImages[i].MarkCollisionDirty();
        }



        /// <summary>
        /// Install a new collision image.  When moving, the object checks each collision image installed against all
        /// the collision images of whatever objects it moves toward.
        /// </summary>
        /// <param name="image">The collision image to add.</param>
        public void InstallImage(T2DCollisionImage image)
        {
            image._sceneObject = SceneObject;
            _collisionImages.Add(image);
        }



        /// <summary>
        /// Remove a collision image previously installed.
        /// </summary>
        /// <param name="image">Collision image to remove.</param>
        public void RemoveImage(T2DCollisionImage image)
        {
            _collisionImages.Remove(image);
        }



        /// <summary>
        /// Test movement from current scene position to passed scene position.  Note: it is the callers responsibility to call
        /// OnCollision for each collision.  Normally, this is handled by the T2DPhysicsComponent.
        /// </summary>
        /// <param name="pos">New position.  On exit, pos will be farthest toward target position object can move without a collision.</param>
        /// <param name="collisions">List of collision information.  For each collision encountered a new T2DCollisionInfo will be added.</param>
        /// <returns>True if no collisions occur, false otherwise.</returns>
        public bool TestMoveTo(ref Vector2 pos, List<T2DCollisionInfo> collisions)
        {
            Vector2 offset = pos - SceneObject.Position;
            float dt = 1.0f;
            TestMove(ref dt, offset, collisions);
            if (collisions.Count != 0)
                pos = SceneObject.Position + dt * offset;
            return collisions.Count == 0;
        }



        /// <summary>
        /// Test movement from current scene position in the direction of velocity for given time interval.  Note: it is the callers 
        /// responsibility to call OnCollision for each collision.  Normally, this is handled by the T2DPhysicsComponent.
        /// </summary>
        /// <param name="dt">Move object for duration, in seconds.</param>
        /// <param name="velocity">Velocity to move at.</param>
        /// <param name="collisions">List of collision information.  For each collision encountered a new T2DCollisionInfo will be added.</param>
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

            if (SceneObject.WorldLimit != null)
                SceneObject.WorldLimit.TestMove(ref dt, searchBox, velocity, SceneObject.Collision, collisions);

            if (!SceneObject.CollisionsEnabled)
                return;

            // prepare container system query
            _queryData.Rectangle = searchBox;
            _queryData.LayerMask = (uint)SceneObject.Collision.CollisionLayerMask;
            _queryData.ObjectTypes = CollidesWith;
            _queryData.IgnoreObject = SceneObject;
            _queryData.IgnoreObjects = null;
            _queryData.FindInvisible = true;
            _queryData.IgnorePhysics = false;
            _queryData.ResultList = _containerQueryResults;

            _containerQueryResults.Clear();

            SceneObject.SceneGraph.Container.FindObjects(_queryData);

            int resultCount = _containerQueryResults.Count;
            if (resultCount == 0)
                return;

            T2DCollisionComponent ourCollider = SceneObject.Collision;

            foreach (ISceneObject sobj in _containerQueryResults)
            {
                T2DSceneObject obj = sobj as T2DSceneObject;

                // don't collide against non-t2d scene objects or ourself
                if (obj == null || obj == Owner || !obj.CollisionsEnabled)
                    continue;
                if (!obj.TestObjectType(_collidesWith))
                    continue;
                if (obj.TestObjectType(_earlyOutObjectType) && (TestEarlyOut != null) && TestEarlyOut(SceneObject, obj))
                    continue;

                Assert.Fatal(!obj.IsUnregistered, "Object removed from sim but not scene!!!");
                Assert.Fatal(obj.IsRegistered, "Object added to the scene but not the sim!!!");

                T2DCollisionComponent theirCollider = obj.Collision;
                if (theirCollider == null)
                    continue;

                ourCollider._TestMove(ref dt, velocity, theirCollider, collisions);
            }
        }



        /// <summary>
        /// Render the bounds of each collision image.  Typically called from T2DSceneObject.Render.
        /// </summary>
        /// <param name="srs">Scene render state passed in for rendering purposes.</param>
        public void RenderBounds(SceneRenderState srs)
        {
            Vector3 ScaleVector = new Vector3(0.5f * SceneObject.Size.X, 0.5f * SceneObject.Size.Y, 1);

            // scale, translate, rotate
            Matrix ScaleMatrix = Matrix.CreateScale(ScaleVector);
            Matrix TranslationMatrix = Matrix.CreateTranslation(new Vector3(SceneObject.Position.X, SceneObject.Position.Y, 0.0f));
            Matrix RotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(SceneObject.Rotation));
            Matrix objToWorld = ScaleMatrix * RotationMatrix * TranslationMatrix;

            // t2dTODO: flip

            for (int i = 0; i < SceneObject.Collision.Images.Count; i++)
                SceneObject.Collision.Images[i].RenderBounds(objToWorld, srs);
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DCollisionComponent obj2 = (T2DCollisionComponent)obj;
            obj2.CollisionLayerMask = CollisionLayerMask;
            obj2.CollidesWith = CollidesWith;
            obj2.EarlyOutObjectType = EarlyOutObjectType;
            obj2.RenderCollisionBounds = RenderCollisionBounds;
            obj2._collisionImages.Clear();
            for (int i = 0; i < _collisionImages.Count; i++)
                obj2._collisionImages.Add((T2DCollisionImage)_collisionImages[i].Clone());
            obj2.OnCollision = OnCollision;
            obj2.ResolveCollision = ResolveCollision;
            obj2.CollisionMaterial = CollisionMaterial;
            obj2.SolveOverlap = SolveOverlap;
            obj2.TestEarlyOut = TestEarlyOut;
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            foreach (T2DCollisionImage image in _collisionImages)
                image._sceneObject = SceneObject;

            return true;
        }



        void _TestMove(ref float dt, Vector2 ourVelocity, T2DCollisionComponent theirCollider, List<T2DCollisionInfo> collisions)
        {
            ReadOnlyArray<T2DCollisionImage> theirImages = theirCollider.Images;
            for (int i = 0; i < _collisionImages.Count; i++)
            {
                T2DCollisionImage ourImage = _collisionImages[i];
                for (int j = 0; j < theirImages.Count; j++)
                {
                    T2DCollisionImage theirImage = theirImages[j];

                    // Colliders need to know how to collide with objects with lower priority but not higher
                    if (ourImage.Priority >= theirImage.Priority)
                        ourImage.TestMove(ref dt, ourVelocity, theirImage, collisions);
                    else
                        theirImage.TestMoveAgainst(ref dt, ourVelocity, ourImage, collisions);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        int _collisionLayerMask = -1;
        TorqueObjectType _collidesWith = TorqueObjectType.AllObjects;
        TorqueObjectType _earlyOutObjectType;
        bool _renderCollisionBounds;
        protected List<T2DCollisionImage> _collisionImages = new List<T2DCollisionImage>();
        protected T2DOnCollisionDelegate _onCollision;
        protected T2DTestCollisionEarlyOutDelegate _testEarlyOut;

        protected bool _solveOverlap = true;
        protected T2DResolveCollisionDelegate _resolveCollisionDelegate;
        T2DCollisionMaterial _collisionMaterial;

        protected static T2DSceneContainerQueryData _queryData = new T2DSceneContainerQueryData();
        protected static List<ISceneContainerObject> _containerQueryResults = new List<ISceneContainerObject>();

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (_collisionImages != null)
                _collisionImages.Clear();
            _collisionImages = null;
            if (_collisionMaterial != null)
                _collisionMaterial.Dispose();
            _collisionMaterial = null;
            _onCollision = null;
            _ResetRefs();
            _resolveCollisionDelegate = null;
            _testEarlyOut = null;
            base.Dispose();
        }

        #endregion
    }
}
