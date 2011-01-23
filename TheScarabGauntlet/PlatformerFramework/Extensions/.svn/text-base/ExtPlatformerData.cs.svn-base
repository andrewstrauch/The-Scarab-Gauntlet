using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    public class ExtPlatformerData
    {

        static private TorqueObjectType damageRegionObjectType;
        static private TorqueObjectType meleeDamageObjectType;
        static private TorqueObjectType enemyDamageObjectType;
        static private TorqueObjectType projectileObjectType;

        static public TorqueObjectType DamageRegionObjectType
        {
            get
            {
                if (!damageRegionObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    damageRegionObjectType = TorqueObjectDatabase.Instance.GetObjectType("DamageRegion");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return damageRegionObjectType;
            }
        }

        static public TorqueObjectType MeleeDamageObjectType
        {
            get
            {
                if (!meleeDamageObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    meleeDamageObjectType = TorqueObjectDatabase.Instance.GetObjectType("MeleeDamage");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return meleeDamageObjectType;
            }
        }

        static public TorqueObjectType EnemyDamageObjectType
        {
            get
            {
                if (!enemyDamageObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    enemyDamageObjectType = TorqueObjectDatabase.Instance.GetObjectType("EnemyDamage");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return enemyDamageObjectType;
            }
        }

        static public TorqueObjectType ProjectileObjectType
        {
            get
            {
                if (!projectileObjectType.Valid)
                {
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = false;
                    projectileObjectType = TorqueObjectDatabase.Instance.GetObjectType("EnemyDamage");
                    TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
                }

                return projectileObjectType;
            }
        }
    }
}
