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
    /// The core of the checkpoint system. This should be accessed via the Instance property, as it is meant to be a singleton. All 
    /// CheckpointSystemSpawnPointComponents register themselves with the CheckpointManager when they initialize. The basic concept
    /// behind the default use of this system is that when a checkpoint is reached, all the CheckpointManager's spawn points that 
    /// have been marked as "used" are removed from the CheckpointManager's list. At any point after that, resetting all spawnpoints
    /// left in the list will essentially roll the world back to the state it was in when that checkpoint was reached (at least the
    /// peices of the world that were spawned by checkpoint system spawn points). This works mostly because of how spawn points reset.
    /// </summary>
    public class CheckpointManager
    {
        //======================================================
        #region Static methods, fields, constructors

        /// <summary>
        /// The static singleton instance of CheckpointManager.
        /// </summary>
        public static CheckpointManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CheckpointManager();

                return _instance;
            }
        }

        private static CheckpointManager _instance;

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not to reset CheckpointSystemSpawnPoints regardless of whether their spawned objects' 
        /// Recover property has been set to false. Default is false. If set to true, all checkpoint system
        /// spawn points will reset when a check point is loaded, rather than just the spawn points whos objects 
        /// haven't been marked with Recover = false.
        /// </summary>
        public bool ResetRemovedObjects
        {
            get { return _resetRemovedObjects; }
            set { _resetRemovedObjects = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Adds the specified CheckpointSystemSpawnPointComponent to the CheckpointManager's list of spawn points.
        /// </summary>
        /// <param name="spawnPoint">The spawn point component to be registered.</param>
        public void RegisterSpawnPoint(CheckpointSystemSpawnPointComponent spawnPoint)
        {
            if (spawnPoint != null && !_spawnPoints.Contains(spawnPoint))
                _spawnPoints.Add(spawnPoint);
        }

        /// <summary>
        /// If ResetRemovedObjects is set to false, this will remove all spawn points which have spawned objects flagged to
        /// not recover.
        /// </summary>
        public void CheckpointReached()
        {
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                // if this spawn point is null for whatever reason, remove it
                if (_spawnPoints[i] == null)
                {
                    _spawnPoints.Remove(_spawnPoints[i]);
                    i--;
                    continue;
                }

                // if we have consumed to the limit of this spawn point, disable and remove it
                if (_spawnPoints[i] == null || _spawnPoints[i].SpawnLimit <= _spawnPoints[i].Consumed)
                {
                    _spawnPoints[i].SpawnLimit = 0;
                    _spawnPoints[i].ResetSpawnPoint();
                    _spawnPoints.Remove(_spawnPoints[i]);
                    i--;
                    continue;
                }

                // otherwise, decrement it's spawn limit
                _spawnPoints[i].SpawnLimit -= _spawnPoints[i].Consumed;
                _spawnPoints[i].Consumed = 0;
            }
        }

        /// <summary>
        /// Reses all spawn points that are currently in the list of spawn points. This will cause all objects that were spawned by those
        /// objects to be deleted and all those spawn points to ready themselves to begin spawning again.
        /// </summary>
        public void LoadCheckPoint()
        {
            foreach (CheckpointSystemSpawnPointComponent spawnPoint in _spawnPoints)
                if (spawnPoint != null)
                    spawnPoint.ResetSpawnPoint();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private List<CheckpointSystemSpawnPointComponent> _spawnPoints = new List<CheckpointSystemSpawnPointComponent>();
        private bool _resetRemovedObjects = false;

        #endregion
    }

    /// <summary>
    /// A component to be added to a scene object. When the player enters the boundaries of the scene object, this component will
    /// call CheckpointReached on the CheckpointManager if all specified conditions are met.
    /// </summary>
    [TorqueXmlSchemaType]
    public class CheckpointComponent : DirectionalTriggerComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not the checkpoint is enabled.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// The object that the respawn position of the Actor that enters this object will be modified based on.
        /// If none is specified, the scene object that owns this component will be used.
        /// </summary>
        [TorqueCloneIgnore]
        public T2DSceneObject RespawnPositionObject
        {
            get { return _respawnPositionObject != null ? _respawnPositionObject : SceneObject; }
            set { _respawnPositionObject = value; }
        }

        /// <summary>
        /// The offset in world coordinates from the center of the RespawnPositionObject that the Actor should respawn.
        /// Default is (0, 0).
        /// </summary>
        public Vector2 RespawnOffset
        {
            get { return _respawnOffset; }
            set { _respawnOffset = value; }
        }

        /// <summary>
        /// Specifies whether or not an Actor will be allowed to use this checkpoint more than once. Default is false.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public bool AllowRecheck
        {
            get { return _allowRecheck; }
            set { _allowRecheck = value; }
        }

        /// <summary>
        /// Specifies the time in milliseconds that the checkpoint will wait before allowing a recheck. This assumes AllowRecheck
        /// has been set to true.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1000")]
        public float RecheckTimeout
        {
            get { return _recheckTimeout; }
            set { _recheckTimeout = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            CheckpointComponent obj2 = obj as CheckpointComponent;

            obj2.Enabled = Enabled;
            obj2.RespawnOffset = RespawnOffset;
            obj2.AllowRecheck = AllowRecheck;
            obj2.RecheckTimeout = RecheckTimeout;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            if (!_enabled || !theirObject.TestObjectType(PlatformerData.ActorObjectType))
                return;

            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            if (actor == null || !_confirmCheckpoint(actor))
                return;

            float currTime = TorqueEngineComponent.Instance.TorqueTime;

            if (_allowRecheck && currTime - _lastCheckTime < _recheckTimeout)
                return;

            actor.RespawnPosition = RespawnPositionObject.Position + RespawnOffset;

            CheckpointManager.Instance.CheckpointReached();

            if (_allowRecheck)
                _lastCheckTime = currTime;
            else
                _enabled = false;
        }

        /// <summary>
        /// Confirms whether or not an Actor should be allowed to use this checkpoint at the moment.
        /// </summary>
        /// <param name="actor">The Actor that's currently trying to use the checkpoint.</param>
        /// <returns>True if the Actor should be allowed to use the checkpoint at the moment.</returns>
        protected virtual bool _confirmCheckpoint(ActorComponent actor)
        {
            // this should be overridden by derived classes
            // a return value of false will result in the checkpoint not being counted
            // a return value of true will result in the checkpoint being counted and progress being saved
            return true;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            SceneObject.SetObjectType(PlatformerData.PlayerTriggerObjectType, true);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected T2DSceneObject _respawnPositionObject;
        protected Vector2 _respawnOffset = Vector2.Zero;
        protected bool _enabled = true;
        protected bool _allowRecheck = false;
        protected float _recheckTimeout = 1000;
        protected float _lastCheckTime = 0;

        #endregion
    }

    /// <summary>
    /// A specific type of SpawnedObjectComponent that can interface with a CheckpointSystemSpawnPointComponent.
    /// </summary>
    [TorqueXmlSchemaType]
    public class CheckpointSystemSpawnedObjectComponent : SpawnedObjectComponent
    {
        //======================================================
        #region Constructors

        // default constructor
        public CheckpointSystemSpawnedObjectComponent() : base() { }

        // constructor just passes spawn point to base class
        public CheckpointSystemSpawnedObjectComponent(SpawnPointComponent spawnPoint) : base(spawnPoint) { }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not this object wants to spawn again if the spawn point is reset.
        /// The spawn point can choose whether or not to obey this property. This will also increment
        /// or decrement the Consumed property of the spawn point that spawned this object, assuming
        /// it's a CheckpointSystemSpawnPointComponent.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public bool Recover
        {
            get { return _recover; }
            set
            {
                if (_recover != value && SpawnPoint is CheckpointSystemSpawnPointComponent)
                    (SpawnPoint as CheckpointSystemSpawnPointComponent).Consumed += value ? -1 : 1;

                _recover = value;
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods



        #endregion

        //======================================================
        #region Private, protected, internal fields

        private bool _recover = true;

        #endregion
    }

    /// <summary>
    /// Essentially a ProximitySpawnPointComponent that registers itself with the CheckpointManager once it's initialized.
    /// The CheckpointManager keeps a list of all CheckpointSystemSpawnPointComponents and handles resetting them when neccesary.
    /// </summary>
    [TorqueXmlSchemaType]
    public class CheckpointSystemSpawnPointComponent : ProximitySpawnPointComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies the number of objects spawned from this spawn point that have been consumed and no longer wish to respawn.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public int Consumed
        {
            get { return _consumed; }
            set { _consumed = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void Despawn(T2DSceneObject obj)
        {
            // make sure the object has the spawned object type
            if (obj.TestObjectType(PlatformerData.SpawnedObjectType))
            {
                // check for checkpoint system spawned object component
                CheckpointSystemSpawnedObjectComponent spawned = obj.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

                // if the spawned object is marked to not recover, modify the spawn count
                if (spawned != null && !spawned.Recover)
                    _spawnCount++;
            }

            // call base despawn
            base.Despawn(obj);
        }

        public override void ResetSpawnPoint()
        {
            base.ResetSpawnPoint();

            // reset consumed to zero
            _consumed = 0;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // register this spawn point with the spawn point manager
            CheckpointManager.Instance.RegisterSpawnPoint(this);

            return true;
        }

        /// <summary>
        /// Overriden _getSpawnPointCoponent passes back a CheckPointSystemSpawnedObjectComponent
        /// </summary>
        /// <returns>A brand new CheckpointSystemSpawnedObjectComponent!</returns>
        protected override SpawnedObjectComponent _getSpawnedComponent()
        {
            return new CheckpointSystemSpawnedObjectComponent(this);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private int _consumed = 0;

        #endregion
    }
}
