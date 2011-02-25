/*******
 * GarageGames.com 
 * Basic code for moving paths taken from
 * Platformer Framework
 */

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

namespace GarageGames.Torque.PlatformerFramework.InteractiveObjects
{
    [TorqueXmlSchemaType] //This is so it can be exported into the schema of components used by TXB
    class PathComponent : TorqueComponent, IT2DForceGenerator
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not this platform should start when it is registered. If set to false, Start must be called
        /// at some point to make the platform move.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool RunOnInit
        {
            get { return _runOnInit; }
            set { _runOnInit = value; }
        }

        /// <summary>
        /// Specifies whether the platform should repeat along the path when it reaches the end.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool Loop
        {
            get { return _loop; }
            set { _loop = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return sceneObject; }
            set { sceneObject = value; }
        }

        /// <summary>
        /// Specifies whether or not this platform is currently running, or whatever.
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
        }

        /// <summary>
        /// Specifies the speed that object moves at
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "5")]
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Start the platform moving from its start position to the first node on its path.
        /// </summary>
        public void Start()
        {
            if (path.Count == 0)
                return;

            myOwner.Position = _startPosition;
            _running = true;
            _currentPathNode = 0;
            _destination = _startPosition;
            _travelToCurrentNode();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            PathComponent obj2 = obj as PathComponent;

            obj2.Loop = Loop;
            obj2.RunOnInit = RunOnInit;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// IT2DForceGenerator interface method. Called by the Physics component before the current move is processed.
        /// </summary>
        public virtual void PreUpdateForces(Move move, float elapsed)
        {
            if (!_running)
                return;

            if (Vector2.Distance(_destination, myOwner.Position) < myOwner.Physics.Velocity.Length() * elapsed)
                _arrived = true;
        }

        /// <summary>
        /// IT2DForceGenerator interface method. Called by the Physics component after the current move is processed.
        /// </summary>
        public virtual void PostUpdateForces(Move move, float elapsed)
        {
            if (_arrived)
                _reachedNode();
        }

        /// <summary>
        /// Sets the velocity of the platform towards the current target node on the path.
        /// </summary>
        protected void _travelToCurrentNode()
        {
            _arrived = false;
            _source = _destination;
            _destination = _currentNodePosition();
            Vector2 temp1 = (_destination - _source); //Vector2.Multiply(_destination - _source, Speed);
            temp1.Normalize();
            myOwner.Physics.Velocity = temp1 * Speed;
            Vector2 temp2 = myOwner.Physics.Velocity;
            temp2.Normalize();
        }

        /// <summary>
        /// Returns the current target node position.
        /// </summary>
        /// <returns>The current target node position in world coordinates.</returns>
        protected Vector2 _currentNodePosition()
        {
            if (_currentPathNode >= path.Count)
                return Vector2.Zero;

            return sceneObject.Position + path[_currentPathNode];
        }

        /// <summary>
        /// Swaps out current and previous nodes and loops the path, if specified.
        /// </summary>
        protected virtual void _reachedNode()
        {
            myOwner.Physics.Velocity = Vector2.Zero;
            myOwner.Position = _destination;

            if (_currentPathNode < path.Count - 1)
            {
                _currentPathNode++;
                _travelToCurrentNode();
            }
            else if (_loop && _currentPathNode >= path.Count - 1)
            {
                _currentPathNode = 0;
                _travelToCurrentNode();
            }
            else
            {
                _running = false;
            }
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            Assert.Fatal(sceneObject != null, "No scene object defined for the pathComponent");

            myOwner = (T2DSceneObject)owner;
            //go through the link points
            GenerateLinkPaths();

            // record the starting position
            _startPosition = myOwner.Position;

            myOwner.Physics.Velocity = Vector2.Zero;

            // start up the platform if it's ready
            if (_runOnInit)
                Start();

            return true;
        }

        /// <summary>
        /// Goes through all the link points and add them to a List.
        /// </summary>
        protected void GenerateLinkPaths()
        {
            //The only thing it requires it the SceneObject that has all the link points attached to it
            //Returns an order list of points. That can be used for paths and other stuff
            bool endSearch = false;
            int i = 1;
            float linkRotation;
            Vector2 linkPosition;
            while (!endSearch)
            {
                String linkPointName = "LinkPoint" + i;
                i++;
                if (sceneObject.LinkPoints.HasLinkPoint(linkPointName))
                {
                    sceneObject.LinkPoints.GetLinkPoint(linkPointName, out linkPosition, out linkRotation);
                    float x = (sceneObject.WorldClipRectangle.Width / 2) * linkPosition.X;
                    float y = (sceneObject.WorldClipRectangle.Height / 2) * linkPosition.Y;
                    path.Add(new Vector2(x, y));
                }
                else
                {
                    endSearch = true;
                }
            }
        }

        /// <summary>
        /// This method allows us to register iterfaces. IT2DForceGenerator is an example of an interface
        /// </summary>
        /// <param name="owner">Owner of the component</param>
        protected override void _RegisterInterfaces(TorqueObject owner)
        {
            // register the force generator interface to allow us to get
            // pre- and post- update physics callbacks
            Owner.RegisterCachedInterface("force", String.Empty, this, _forceInterface);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private List<Vector2> path = new List<Vector2>();
        private Vector2 _source = Vector2.Zero;
        private Vector2 _destination = Vector2.Zero;
        private Vector2 _startPosition = Vector2.Zero;
        private int _currentPathNode;
        private bool _running;
        private bool _arrived;
        private bool _runOnInit = true;
        private bool _loop = true;
        // force interface
        TorqueInterfaceWrap<IT2DForceGenerator> _forceInterface = new TorqueInterfaceWrap<IT2DForceGenerator>();
        // scene object (owner)
        protected T2DSceneObject myOwner;
        float speed;
        //The object that will be created and moved around
        private T2DSceneObject sceneObject;

        #endregion
    }
}