//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// Contains object types and physics materials used by the platformer framework.
    /// </summary>
    public class PlatformerData
    {
        //======================================================
        #region Static methods, fields, constructors

        static private TorqueObjectType _actorObjectType;
        static private TorqueObjectType _actorPuppetObjectType;
        static private TorqueObjectType _playerObjectType;
        static private TorqueObjectType _enemyObjectType;

        static private TorqueObjectType _platformObjectType;
        static private TorqueObjectType _oneWayPlatformObjectType;
        static private TorqueObjectType _ladderObjectType;

        static private TorqueObjectType _collectibleObjectType;
        static private TorqueObjectType _damageTriggerObjectType;
        static private TorqueObjectType _actorTriggerObjectType;
        static private TorqueObjectType _playerTriggerObjectType;
        static private TorqueObjectType _enemyTriggerObjectType;
        static private TorqueObjectType _platformTriggerObjectType;

        static private TorqueObjectType _spawnedObjectType;


        static private T2DCollisionMaterial _normalSurfacePhysMat;
        static private T2DCollisionMaterial _slipperySurfacePhysMat;


        /// <summary>
        /// The object type to be used for all Actors.
        /// </summary>
        static public TorqueObjectType ActorObjectType
        {
            get
            {
                if (!_actorObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _actorObjectType = TorqueObjectDatabase.Instance.GetObjectType("Actor");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _actorObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for all Actor's "puppets" (ActorPuppetComponent).
        /// </summary>
        static public TorqueObjectType ActorPuppetObjectType
        {
            get
            {
                if (!_actorPuppetObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _actorPuppetObjectType = TorqueObjectDatabase.Instance.GetObjectType("ActorPuppet");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _actorPuppetObjectType;
            }
        }


        /// <summary>
        /// The object type to be used for players.
        /// </summary>
        static public TorqueObjectType PlayerObjectType
        {
            get
            {
                if (!_playerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _playerObjectType = TorqueObjectDatabase.Instance.GetObjectType("Player");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _playerObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for enemies.
        /// </summary>
        static public TorqueObjectType EnemyObjectType
        {
            get
            {
                if (!_enemyObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _enemyObjectType = TorqueObjectDatabase.Instance.GetObjectType("Enemy");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _enemyObjectType;
            }
        }



        /// <summary>
        /// The object type to be used for all platforms (solid and one-way).
        /// </summary>
        static public TorqueObjectType PlatformObjectType
        {
            get
            {
                if (!_platformObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _platformObjectType = TorqueObjectDatabase.Instance.GetObjectType("Platform");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _platformObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for all one-way platforms.
        /// </summary>
        static public TorqueObjectType OneWayPlatformObjectType
        {
            get
            {
                if (!_oneWayPlatformObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _oneWayPlatformObjectType = TorqueObjectDatabase.Instance.GetObjectType("OneWayPlatform");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _oneWayPlatformObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for all one-way platforms.
        /// </summary>
        static public TorqueObjectType LadderObjectType
        {
            get
            {
                if (!_ladderObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _ladderObjectType = TorqueObjectDatabase.Instance.GetObjectType("Ladder");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _ladderObjectType;
            }
        }



        /// <summary>
        /// The object type to be used for collectibles.
        /// </summary>
        static public TorqueObjectType CollectibleObjectType
        {
            get
            {
                if (!_collectibleObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _collectibleObjectType = TorqueObjectDatabase.Instance.GetObjectType("Collectible");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _collectibleObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for Hazards and Kill Triggers.
        /// </summary>
        static public TorqueObjectType DamageTriggerObjecType
        {
            get
            {
                if (!_damageTriggerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _damageTriggerObjectType = TorqueObjectDatabase.Instance.GetObjectType("DamageTrigger");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _damageTriggerObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for generic triggers that all actors will register with.
        /// </summary>
        static public TorqueObjectType ActorTriggerObjectType
        {
            get
            {
                if (!_actorTriggerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _actorTriggerObjectType = TorqueObjectDatabase.Instance.GetObjectType("ActorTrigger");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _actorTriggerObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for triggers that Players will register with.
        /// </summary>
        static public TorqueObjectType PlayerTriggerObjectType
        {
            get
            {
                if (!_playerTriggerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _playerTriggerObjectType = TorqueObjectDatabase.Instance.GetObjectType("PlayerTrigger");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _playerTriggerObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for triggers that enemies will register with.
        /// </summary>
        static public TorqueObjectType EnemyTriggerObjectType
        {
            get
            {
                if (!_enemyTriggerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _enemyTriggerObjectType = TorqueObjectDatabase.Instance.GetObjectType("EnemyTrigger");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _enemyTriggerObjectType;
            }
        }

        /// <summary>
        /// The object type to be used for triggers that platforms will register with.
        /// </summary>
        static public TorqueObjectType PlatformTriggerObjectType
        {
            get
            {
                if (!_platformTriggerObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _platformTriggerObjectType = TorqueObjectDatabase.Instance.GetObjectType("PlatformTrigger");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _platformTriggerObjectType;
            }
        }


        /// <summary>
        /// The object type to be used for all spawned objects.
        /// </summary>
        static public TorqueObjectType SpawnedObjectType
        {
            get
            {
                if (!_spawnedObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    _spawnedObjectType = TorqueObjectDatabase.Instance.GetObjectType("SpawnedObject");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return _spawnedObjectType;
            }
        }



        /// <summary>
        /// The physics material to be used for all platforms that have normal friction.
        /// </summary>
        static public T2DCollisionMaterial NormalSurfacePhysMat
        {
            get
            {
                if (_normalSurfacePhysMat == null)
                    _normalSurfacePhysMat = new T2DCollisionMaterial(0.3f, 0.3f, 0.6f);

                return _normalSurfacePhysMat;
            }
        }

        /// <summary>
        /// The physics material to be used for all platforms that are slippery.
        /// </summary>
        static public T2DCollisionMaterial SlipperySurfacePhysMat
        {
            get
            {
                if (_slipperySurfacePhysMat == null)
                    _slipperySurfacePhysMat = new T2DCollisionMaterial(0.3f, 0.1f, 0.7f);

                return _slipperySurfacePhysMat;
            }
        }

        #endregion
    }
}