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
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A component automatically added to every spawned object to allow spawned objects to
    /// interface with their spawn point of origin.
    /// </summary>
    [TorqueXmlSchemaType]
    public class SpawnedObjectComponent : TorqueComponent
    {
        //======================================================
        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SpawnedObjectComponent()
        {
        }

        /// <summary>
        /// Constructor. Requires a spawn point to be specified.
        /// </summary>
        /// <param name="spawnPoint">The SpawnPointComponent that spawned this object.</param>
        public SpawnedObjectComponent(SpawnPointComponent spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The SpawnPointComponent that spawned this object.
        /// </summary>
        [XmlIgnore]
        public SpawnPointComponent SpawnPoint
        {
            get { return _spawnPoint; }
            set { _spawnPoint = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            SpawnedObjectComponent obj2 = obj as SpawnedObjectComponent;

            obj2.SpawnPoint = SpawnPoint;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // set this object's type to include "spawned object"
            owner.SetObjectType(PlatformerData.SpawnedObjectType, true);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private SpawnPointComponent _spawnPoint;

        #endregion
    }

    /// <summary>
    /// Base spawn point component. Use this for the most basic spawning of objects on command.
    /// </summary>
    [TorqueXmlSchemaType]
    public class SpawnPointComponent : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object that owns this SpawnPointComponent.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return _sceneObject; }
        }

        /// <summary>
        /// The object to be spawned at this spawn point.
        /// </summary>
        public T2DSceneObject SpawnObject
        {
            get { return _spawnObject; }
            set { _spawnObject = value; }
        }

        /// <summary>
        /// The number of objects that have been spawned from this spawn point since the last reset.
        /// </summary>
        public int SpawnCount
        {
            get { return _spawnCount; }
        }

        /// <summary>
        /// The maximum SpawnCount for this spawn point.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public int SpawnLimit
        {
            get { return _spawnLimit; }
            set { _spawnLimit = value; }
        }

        /// <summary>
        /// The minimum time to wait when spawning multiple objects.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1000")]
        public float SpawnTimeout
        {
            get { return _spawnTimeout; }
            set
            {
                _spawnTimeout = value;
                _lastSpawnTime = -value;
            }
        }

        /// <summary>
        /// A list of all objects spawned from this spawn point since the last reset.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public List<T2DSceneObject> SpawnedObjects
        {
            get { return _spawnedObjects; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Attempt to spawn an instance of SpawnObject.
        /// </summary>
        public virtual void Spawn()
        {
            // make sure there's an object to spawn
            if (_spawnObject == null)
                return;

            // get the current time
            float currTime = TorqueEngineComponent.Instance.TorqueTime;

            // make sure the spawn count is within the spawn limit
            // and we aren't within our spawn timeout
            if (_spawnCount < _spawnLimit && currTime - _lastSpawnTime > _spawnTimeout)
            {
                // record last spawn time and increment spawn count
                _lastSpawnTime = currTime;
                _spawnCount++;

                // create a clone of SpawnObject and set its position
                T2DSceneObject newObject = (_spawnObject as T2DSceneObject).Clone() as T2DSceneObject;
                newObject.Position = SceneObject.Position;

                // get the current spawned object component, if one exists
                SpawnedObjectComponent currComp = newObject.Components.FindComponent<SpawnedObjectComponent>();

                // if PoolWithComponents is turned on, the object should already have a SpawnedObjectComponent on it
                // this needs to be done on the template aswell to maintain consistancy
                if (newObject.PoolWithComponents || currComp != null)
                {
                    // make sure it exists
                    Assert.Fatal(currComp != null, "When using spawn points to spawn objects with PoolWithComponents turned on, the SpawnedObjectComponent must already be present on the temlate object.");

                    // set recover to true on the component
                    if (currComp is CheckpointSystemSpawnedObjectComponent)
                        (currComp as CheckpointSystemSpawnedObjectComponent).Recover = true;

                    // set the spawn point of the spawned object to this
                    currComp.SpawnPoint = this;
                }
                else
                {
                    // PoolWithComponents is off on this object, create a new spawnedObjectComponent for the object
                    SpawnedObjectComponent spawnedComp = _getSpawnedComponent();

                    // add the new component to the new object
                    newObject.Components.AddComponent(spawnedComp);
                }

                // add the newly spawned object to our SpawnedObjects list
                _spawnedObjects.Add(newObject);

                newObject.SetObjectType(PlatformerData.SpawnedObjectType, true);

                // register the new object
                TorqueObjectDatabase.Instance.Register(newObject);
            }
        }

        /// <summary>
        /// Despawn the specified object. Removes it from the world and decrements the spawned object counter.
        /// </summary>
        public virtual void Despawn(T2DSceneObject obj)
        {
            _spawnedObjects.Remove(obj);

            SpawnedObjectComponent spawned = obj.Components.FindComponent<SpawnedObjectComponent>();

            if (obj.IsRegistered)
                TorqueObjectDatabase.Instance.Unregister(obj);

            _spawnCount--;
        }

        /// <summary>
        /// Roll SpawnCount back to zero and delete all remaining spawned objects.
        /// </summary>
        public virtual void ResetSpawnPoint()
        {
            _lastSpawnTime = -_spawnTimeout;

            for (int i = 0; i < _spawnedObjects.Count; i++)
                Despawn(_spawnedObjects[i]);

            _spawnCount = 0;

            _spawnedObjects.Clear();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            SpawnPointComponent obj2 = obj as SpawnPointComponent;

            obj2.SpawnLimit = SpawnLimit;
            obj2.SpawnObject = SpawnObject;
            obj2.SpawnTimeout = SpawnTimeout;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // store the scene object that owns this spawn point
            _sceneObject = owner as T2DSceneObject;

            // set this spawn point invisible
            // (this allows the use of Static/Animated Sprites for spawn points in the editor,
            // which makes level design a whole lot easier)
            _sceneObject.Visible = false;

            return true;
        }

        /// <summary>
        /// This exists to allow you to create new spawned object components derived from the base and return those instead.
        /// If PoolWithComponents is enabled on the SpawnObject, this will not be called. In that case you must put the desired
        /// type of SpawnedObjectComponent onto the SpawnObject itself.
        /// </summary>
        /// <returns>A new SpawnedObjectComponent to be attached to the new spawned object.</returns>
        protected virtual SpawnedObjectComponent _getSpawnedComponent()
        {
            // returns a new base spawned object component
            return new SpawnedObjectComponent(this);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DSceneObject _sceneObject;
        private T2DSceneObject _spawnObject;
        protected int _spawnCount = 0;
        protected int _spawnLimit = 1;
        private float _spawnTimeout = 500;
        private float _lastSpawnTime = -100000;
        private List<T2DSceneObject> _spawnedObjects = new List<T2DSceneObject>();

        #endregion
    }

    /// <summary>
    /// A spawn point that will automatically spawn it's object(s) when two scene objects are within a certain range.
    /// </summary>
    [TorqueXmlSchemaType]
    public class ProximitySpawnPointComponent : SpawnPointComponent, ITickObject
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object to check the distance of. If no ProximityBasisObject is specified, 
        /// the distance checked will be from the Target to the scene object that owns this component.
        /// In most cases, the Target will be the scene camera and the ProximityBasisObject will be null.
        /// </summary>
        public T2DSceneObject Target
        {
            get { return _proximityTarget; }
            set { _proximityTarget = value; }
        }

        /// <summary>
        /// An optional scene object from which to check the distance of Target. If no ProximityBasisObject
        /// is specified, the distance will checked will be from the Target to the scene object that owns this
        /// component. In most cases, you would not want to specify a ProximityBasisObject. One case where it
        /// would prove useful would be a series of moving platforms that are precisely timed and need to sync up.
        /// You would set the ProximityBasisObject of the spawn points for all the moving platforms to a scene
        /// object near the first platform the player will encounter so they are all created at the same time.
        /// </summary>
        public T2DSceneObject ProximityBasisObject
        {
            get { return _proximityBasisObject != null ? _proximityBasisObject : SceneObject; }
            set { _proximityBasisObject = value; }
        }

        /// <summary>
        /// The minimum distance from the Target to attempt to spawn our SpawnObject. Use this to prevent spawn points
        /// from spawning objects while in view of the camera.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public float MinSpawnDistance
        {
            get { return _minSpawnDistance; }
            set { _minSpawnDistance = value; }
        }

        /// <summary>
        /// The maximum distance from the Target to attempt to spawn our SpawnObject. Use this to limit the range in 
        /// which objects are spawned.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "150")]
        public float MaxSpawnDistance
        {
            get { return _maxSpawnDistance; }
            set { _maxSpawnDistance = value; }
        }

        /// <summary>
        /// Specifies whether or not this spawn point will automatically despawn objects after they are outside their spawn limits.
        /// This will only occur if the target is not between the spawn point and the spawned object.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool AutoDespawnObjects
        {
            get { return _autoDespawn; }
            set { _autoDespawn = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float elapsed)
        {
            // check distance and spawn objects if appropriate
            if (_proximityTarget == null)
                return;

            // check for objects to spawn
            if (SpawnCount < SpawnLimit)
            {
                float distance = Vector2.Distance(ProximityBasisObject.Position, _proximityTarget.Position);

                if (distance <= _maxSpawnDistance && distance >= _minSpawnDistance)
                    Spawn();
            }

            // check for objects to despawn
            if (_autoDespawn && SpawnedObjects.Count > 0)
            {
                for (int i = 0; i < SpawnedObjects.Count; i++)
                {
                    T2DSceneObject obj = SpawnedObjects[i];

                    if (obj == null)
                        continue;

                    // if they are not the same direction away, skip
                    if ((obj.Position.X - Target.Position.X < 0) != (ProximityBasisObject.Position.X - Target.Position.X < 0))
                        continue;

                    float distance = Vector2.Distance(ProximityBasisObject.Position, _proximityTarget.Position);
                    float objDistance = Vector2.Distance(obj.Position, _proximityTarget.Position);

                    // if they are far enough away and the spawn point is far enough away, despawn!
                    if (distance > _maxSpawnDistance * 1.5 && objDistance > _maxSpawnDistance * 1.5)
                    {
                        Despawn(obj);

                        // decrement the counter
                        // (one fewer object)
                        i--;
                    }
                }
            }
        }

        public virtual void InterpolateTick(float k) { }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ProximitySpawnPointComponent obj2 = obj as ProximitySpawnPointComponent;

            obj2.Target = Target;
            obj2.ProximityBasisObject = ProximityBasisObject;
            obj2.MinSpawnDistance = MinSpawnDistance;
            obj2.MaxSpawnDistance = MaxSpawnDistance;
            obj2.AutoDespawnObjects = AutoDespawnObjects;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // add a tick callback for this spawn point
            ProcessList.Instance.AddTickCallback(owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DSceneObject _proximityTarget;
        private T2DSceneObject _proximityBasisObject;
        private float _minSpawnDistance = 0.0f;
        private float _maxSpawnDistance = 70.0f;
        private bool _autoDespawn = true;

        #endregion
    }
}
