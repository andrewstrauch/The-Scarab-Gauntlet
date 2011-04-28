//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Creates a copy of the SpawnTemplate object and adds it to the scene. Can
    /// be used in one of two ways. If SpawnOnce is specified, on OnRegister it
    /// will remove itself after creating one object from the SpawnTemplate. This
    /// can be used to place many copies of the same object in the scene at startup.
    /// Otherwise T2DSpawnObject will continue to place copies of the SpawnTemplate
    /// into the scene based on the given SpawnTime and SpawnVariance. Position,
    /// Rotation, and Size of the spawned object will be over-written with the 
    /// Position, Rotation, and Size of the T2DSpawnObject.
    /// </summary>
    public class T2DSpawnObject : T2DSceneObject, IDisposable
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The object template to be spawned in the scene.
        /// </summary>
        public T2DSceneObject SpawnTemplate
        {
            get { return _spawnTemplate; }
            set { _spawnTemplate = value; }
        }



        /// <summary>
        /// If SpawnEnabled is true along with this property, spawner will spawn a single object when registered
        /// with the object database. If SpawnEnabled is not true, spawner will not remove itself on registered
        /// and will not spawn an object. DoSpawn method can be invoked to spawn a single object at a time. This
        /// property ignores MinSpawnTime and MaxSpawnTime.
        /// </summary>
        public bool SpawnOnce
        {
            get { return _spawnOnce; }
            set { _spawnOnce = value; }
        }



        /// <summary>
        /// Enable/Disable automaticically spawning copies of the SpawnTemplate.
        /// </summary>
        public bool SpawnEnabled
        {
            get { return _spawnEnabled; }
            set { _spawnEnabled = value; }
        }



        /// <summary>
        /// In milliseconds, the minimum time that may pass between object spawns. Used in
        /// conjunction with MaxSpawnTime as a range of allowable time that is randomly
        /// rechosen after each object spawn.
        /// </summary>
        public float MinSpawnTime
        {
            get { return _minSpawnTime; }
            set
            {
                _minSpawnTime = value;

                Assert.Fatal(_minSpawnTime >= 0.0f, "T2DSpawnObject::MinSpawnTime: property was assigned a negative value!");
                if (_minSpawnTime < 0.0f)
                    _minSpawnTime = 0.0f;
            }
        }



        /// <summary>
        /// In milliseconds, the maximum time that may pass between object spawns. Used in
        /// conjunction with MinSpawnTime as a range of allowable time that is randomly
        /// rechosen after each object spawn.
        /// </summary>
        public float MaxSpawnTime
        {
            get { return _maxSpawnTime; }
            set
            {
                _maxSpawnTime = value;

                Assert.Fatal(_maxSpawnTime >= 0.0f, "T2DSpawnObject::MaxSpawnTime: property was assigned a negative value!");
                if (_maxSpawnTime < 0.0f)
                    _maxSpawnTime = 0.0f;
            }
        }

        #endregion


        #region Public methods

        public bool UseRandomStartup
        {
            get { return _useRandomStartup; }
            set { _useRandomStartup = value; }
        }

        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            if (_spawnOnce)
            {
                if (_spawnEnabled)
                {
                    DoSpawn();
                    MarkForDelete = true;
                }
            }
            else
            {
                if (_useRandomStartup)
                {
                    if (_maxSpawnTime < _minSpawnTime)
                        _maxSpawnTime = _minSpawnTime;
                    // generate a new spawn time  
                    _nextSpawn = TorqueUtil.GetRandomFloat(_minSpawnTime, _maxSpawnTime);
                }

                SetTicking();
            }
            return true;
        }


        public volatile T2DSceneObject sceneObject = null;

        public void DoSpawn()
        {
            if (_spawnTemplate != null)
            {
                // clone the template and register it with the object database
                sceneObject = (T2DSceneObject)_spawnTemplate.Clone();
                sceneObject.Position = Position;
                sceneObject.Rotation = Rotation;
                sceneObject.Size = Size;
                if (Manager != null)
                    Manager.Register(sceneObject);
                else
                    TorqueObjectDatabase.Instance.Register(sceneObject);
            }
        }



        public override void ProcessTick(Move move, float elapsed)
        {
            base.ProcessTick(move, elapsed);

            // don't allow spawning if we're not enabled
            if (!_spawnEnabled)
                return;

            _currentTime += (elapsed * 1000.0f);

            if (_currentTime >= _nextSpawn)
            {
                DoSpawn();

                Assert.Fatal(_maxSpawnTime >= _minSpawnTime, "T2DSpawnObject::ProcessTick: MaxSpawnTime is less than MinSpawnTime!");
                if (_maxSpawnTime < _minSpawnTime)
                    _maxSpawnTime = _minSpawnTime;

                // generate a new spawn time
                _nextSpawn = TorqueUtil.GetRandomFloat(_minSpawnTime, _maxSpawnTime);

                // make sure we didn't generate a negative time
                Assert.Fatal(_nextSpawn >= 0.0f, "T2DSpawnObject::ProcessTick: spawn rate is negative!");
                if (_nextSpawn < 0.0f)
                    _nextSpawn = 0.0f;

                _nextSpawn += _currentTime;
            }
        }

        #endregion


        #region Private, protected, internal fields

        float _currentTime = 0.0f;
        float _nextSpawn = 0.0f;

        T2DSceneObject _spawnTemplate;
        bool _spawnEnabled = true;
        bool _useRandomStartup = true;
        bool _spawnOnce = true;

        float _minSpawnTime = 0.5f;
        float _maxSpawnTime = 0.5f;


        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            this.SpawnTemplate = null;
            base.Dispose();
        }

        #endregion
    }
}
