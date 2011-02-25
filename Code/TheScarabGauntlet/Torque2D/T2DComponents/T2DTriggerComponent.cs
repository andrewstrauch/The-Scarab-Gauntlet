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
    /// Called when an object enters the trigger.
    /// </summary>
    /// <param name="ourObject">The scene object owner of the trigger.</param>
    /// <param name="theirObject">The scene object that entered the trigger.</param>
    public delegate void T2DTriggerComponentOnEnterDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject);



    /// <summary>
    /// Called each tick that an object stays in a trigger after the initial entry.
    /// </summary>
    /// <param name="ourObject">The scene object owner of the trigger.</param>
    /// <param name="theirObject">The scene object that's staying in the trigger.</param>
    public delegate void T2DTriggerComponentOnStayDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject);



    /// <summary>
    /// Called when an object leaves the trigger.
    /// </summary>
    /// <param name="ourObject">The scene object owner of the trigger.</param>
    /// <param name="theirObject">The scene object that's leaving the trigger.</param>
    public delegate void T2DTriggerComponentOnLeaveDelegate(T2DSceneObject ourObject, T2DSceneObject theirObject);



    /// <summary>
    /// Adding this component to a T2DSceneObject allows you to receive callbacks when an object enters, 
    /// stays in, and exits the scene object's bounding box. You can specify the object types to check for
    /// with the CollidesWith property, or what layers to check on with the LayerMask property.
    /// You can also optionally specify a T2DCollisionImage for the trigger to use in addition to the 
    /// initial bounding box check. Note that the initial check is still limited to the bounding box 
    /// of the owning scene object.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DTriggerComponent : TorqueComponent, ITickObject
    {
        #region Public properties, operators, constants, and enums
        /// <summary>
        /// The owning T2DSceneObject. SceneObject is used to determine the extent of the trigger region
        /// using the T2DSceneObject.Size property.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }



        /// <summary>
        /// Specifies whether or not the trigger will check for objects. Disabling and re-enabling a trigger 
        /// clears the trigger's object list, meaning that you will get enter callbacks for all objects that
        /// were still in the trigger.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value && !_enabled)
                {
                    _enabled = value;
                }
                else if (_enabled && !value)
                {
                    _enabled = value;
                    _objectList.Clear();
                }
            }
        }



        /// <summary>
        /// Specifies whether or not to include objects without collision components in the case that the trigger
        /// has a collision image. A value of true results in the trigger ignoring such objects. A value of false
        /// results in the trigger including them without performing a collision image check against the object.
        /// This field is ignored if the trigger doesn't have any collision images.
        /// </summary>
        [XmlIgnore]
        public bool IgnoreCollCompNullWhenImgs
        {
            get { return _ignoreCollCompNullWhenImgs; }
            set { _ignoreCollCompNullWhenImgs = value; }
        }



        /// <summary>
        /// Mask which determines which layers will be checked by the trigger.  The layer of an object is determined by
        /// the T2DSceneObject.Layer property.  Because this is a mask, each bit refers to a single layer,
        /// with bit N corresponding to layer N.  A mask of 7, for example, indicates that the object will
        /// collide with objects on layers 1, 2, and 3.
        /// </summary>
        [XmlIgnore]
        public int LayerMask
        {
            get { return _layerMask; }
            set { _layerMask = value; }
        }



        /// <summary>
        /// Determines which object types (see T2DSceneObject.ObjectType property) to include in the check.
        /// TorqueObjectTypes can be combined to form a collection of object types with which to collide.
        /// </summary>
        public TorqueObjectType CollidesWith
        {
            get { return _collidesWith; }
            set { _collidesWith = value; }
        }



        /// <summary>
        /// This delegate is called when an object enters the trigger.
        /// </summary>
        public T2DTriggerComponentOnEnterDelegate OnEnter
        {
            get { return _onEnter; }
            set { _onEnter = value; }
        }



        /// <summary>
        /// This delegate is called every tick that an object stays in the trigger after the initial entry.
        /// </summary>
        public T2DTriggerComponentOnStayDelegate OnStay
        {
            get { return _onStay; }
            set { _onStay = value; }
        }



        /// <summary>
        /// This delegate is called when an object leaves the trigger.
        /// </summary>
        public T2DTriggerComponentOnLeaveDelegate OnLeave
        {
            get { return _onLeave; }
            set { _onLeave = value; }
        }



        /// <summary>
        /// Read only array of collision images.
        /// </summary>
        public ReadOnlyArray<T2DCollisionImage> Images
        {
            get { return new ReadOnlyArray<T2DCollisionImage>(_collisionImages); }
        }



        protected Vector2 SceneObjectVelocity
        {
            get
            {
                if (SceneObject.Physics != null)
                    return SceneObject.Physics.Velocity;

                return Vector2.Zero;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Installs a collision image on the trigger.
        /// </summary>
        /// <param name="image">Reference to the T2DCollisionImage to be installed.</param>
        public void InstallImage(T2DCollisionImage image)
        {
            image._sceneObject = SceneObject;
            _collisionImages.Add(image);
        }



        /// <summary>
        /// Removes a collision image from the trigger.
        /// </summary>
        /// <param name="image">Reference to the T2DCollisionImage to be removed.</param>
        public void RemoveImage(T2DCollisionImage image)
        {
            _collisionImages.Remove(image);
        }



        public virtual void ProcessTick(Move move, float elapsed)
        {
            // skip out if inactive
            if (!_enabled)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("T2DTriggerComponent.ProcessTick");
#endif
            // perform the container query
            _doContainerQuery();

            // do optional collision image check
            if (_collisionImages.Count > 0)
                _doCollisionImageCheck();

            // check results, call delegates,  and modify list
            foreach (T2DSceneObject sobj in _containerQueryResults)
            {
                // check if the object was already in the container and call the OnStay delegate
                if (_objectList.Contains(sobj))
                {
                    if (_onStay != null)
                        _onStay(SceneObject, sobj);
                }
                else
                {
                    // new object, call the OnEnter delegate
                    if (_onEnter != null)
                        _onEnter(SceneObject, sobj);

                    // add the object to the out list
                    _objectList.Add(sobj);
                }
            }

            // check for missing scene objects, call delegates, and modify list
            for (int i = 0; i < _objectList.Count; i++)
            {
                if (!_containerQueryResults.Contains(_objectList[i]))
                {
                    // call onLeave and remove the object
                    if (_onLeave != null)
                        _onLeave(SceneObject, _objectList[i]);

                    _objectList.Remove(_objectList[i]);

                    // decrement i to make up for the object now missing from list
                    i--;
                }
            }

#if DEBUG
            Profiler.Instance.EndBlock("T2DTriggerComponent.ProcessTick");
#endif
        }



        public virtual void InterpolateTick(float k) { }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            T2DTriggerComponent obj2 = (obj as T2DTriggerComponent);

            obj2.LayerMask = LayerMask;
            obj2.CollidesWith = CollidesWith;
            obj2.OnEnter = OnEnter;
            obj2.OnStay = OnStay;
            obj2.OnLeave = OnLeave;
            obj2.Enabled = Enabled;

            for (int i = 0; i < _collisionImages.Count; i++)
                obj2._collisionImages.Add((T2DCollisionImage)_collisionImages[i].Clone());
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || SceneObject == null)
                return false;

            // add this object to the process list with priority 0
            ProcessList.Instance.AddTickCallback(owner, this, 1.0f);

            // init the query data that doesn't change
            _initQueryData();

            // init collision images
            foreach (T2DCollisionImage image in _collisionImages)
                image._sceneObject = SceneObject;

            return true;
        }



        private void _initQueryData()
        {
            // init query data for ground pick
            _queryData.IgnoreObject = Owner as ISceneContainerObject;
            _queryData.IgnoreObjects = null;
            _queryData.FindInvisible = false;
            _queryData.IgnorePhysics = false;
            _queryData.ResultList = _containerQueryResults;
        }



        private void _doContainerQuery()
        {
            // create a rect extended by velocity
            // stolen from collision component testmove
            RectangleF searchBox = SceneObject.WorldCollisionClipRectangle;

            float vx = SceneObjectVelocity.X * 0.001f;
            float vy = SceneObjectVelocity.Y * 0.001f;

            if (vx > 0.0f)
                searchBox.Width += vx;
            else
            {
                searchBox.X += vx;
                searchBox.Width -= vx;
            }

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

            // assign the search box to the query data
            _queryData.Rectangle = searchBox;

            // set the group and layer masks
            _queryData.LayerMask = (uint)_layerMask;
            _queryData.ObjectTypes = _collidesWith;

            // clear results from previous container query
            _containerQueryResults.Clear();

            // do the query
            SceneObject.SceneGraph.Container.FindObjects(_queryData);
        }



        private void _doCollisionImageCheck()
        {
            // do collision checks on images
            float dt = 1.0f;

            // iterate through all the objects in our query results
            for (int x = 0; x < _containerQueryResults.Count; x++)
            {
                // clear collision info list for each object
                _collisions.Clear();

                // grab their collision component
                T2DCollisionComponent theirCollider = (_containerQueryResults[x] as T2DSceneObject).Collision;

                // if they don't have a collision component respond based on _ignoreCollCompNullWithImgs flag
                if (theirCollider == null)
                {
                    // if ignoring null collision components, remove the object from the list
                    if (_ignoreCollCompNullWhenImgs)
                    {
                        _containerQueryResults.Remove(_containerQueryResults[x]);
                        x--;
                    }

                    // skip to the next object
                    continue;
                }

                // iterate through and test collisions across all images
                for (int i = 0; i < _collisionImages.Count; i++)
                {
                    for (int j = 0; j < theirCollider.Images.Count; j++)
                    {
                        // Colliders need to know how to collide with objects with lower priority but not higher
                        if (_collisionImages[i].Priority >= theirCollider.Images[j].Priority)
                            _collisionImages[i].TestMove(ref dt, SceneObjectVelocity, theirCollider.Images[j], _collisions);
                        else
                            theirCollider.Images[j].TestMoveAgainst(ref dt, SceneObjectVelocity, _collisionImages[i], _collisions);

                        // found a collision on this object - break now
                        if (_collisions.Count > 0)
                            break;
                    }

                    // found a collision on this object - break now
                    if (_collisions.Count > 0)
                        break;
                }

                // found a collision on this object, leave it in the query results
                if (_collisions.Count > 0)
                    continue;

                // if there are no collisions, remove this object from the query results
                _containerQueryResults.Remove(_containerQueryResults[x]);
                x--;
            }
        }

        #endregion


        #region Private, protected, internal fields

        private bool _enabled = true;
        private int _layerMask = -1;
        private TorqueObjectType _collidesWith = TorqueObjectType.AllObjects;
        private bool _ignoreCollCompNullWhenImgs = false;
        protected List<T2DSceneObject> _objectList = new List<T2DSceneObject>();
        protected List<T2DCollisionImage> _collisionImages = new List<T2DCollisionImage>();

        private T2DSceneContainerQueryData _queryData = new T2DSceneContainerQueryData();
        private List<ISceneContainerObject> _containerQueryResults = new List<ISceneContainerObject>();
        private List<T2DCollisionInfo> _collisions = new List<T2DCollisionInfo>();

        private T2DTriggerComponentOnEnterDelegate _onEnter;
        private T2DTriggerComponentOnStayDelegate _onStay;
        private T2DTriggerComponentOnLeaveDelegate _onLeave;

        #endregion
    }
}
