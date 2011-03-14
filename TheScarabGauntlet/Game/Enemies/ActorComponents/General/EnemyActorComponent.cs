using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using GarageGames.Torque.T2D;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    [TorqueXmlSchemaType]
    public abstract class EnemyActorComponent : ActorComponent, IEnemyActor
    {
        protected List<IBehavior> actorBehavior;
        protected bool readyToAttack;
        private int numDroppedCrystals;
        private T2DSceneObject crystalTemplate;

        #region Properties
        public List<IBehavior> AIComponent
        {
            get { return actorBehavior; }
            set { actorBehavior = value; }
        }
        [System.Xml.Serialization.XmlIgnore]
        public bool ReadyToAttack
        {
            get { return readyToAttack; }
            set { readyToAttack = value; }
        }
        public int NumDroppedCrystals
        {
            get { return numDroppedCrystals; }
            set { numDroppedCrystals = value; }
        }
        public T2DSceneObject CrystalTemplate
        {
            get { return crystalTemplate; }
            set { crystalTemplate = value; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            EnemyActorComponent obj2 = obj as EnemyActorComponent;

            obj2.AIComponent = AIComponent;
            obj2.CrystalTemplate = CrystalTemplate;
            obj2.NumDroppedCrystals = NumDroppedCrystals;
        }

        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            // update the melee timer
            TimerManager.Instance.Update(TorqueEngineComponent.Instance.GameTime);
        }

        protected Vector2 _getDistanceToPlayer()
        {
            T2DSceneCamera camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>("Camera");

            // get the distance of this drill to from the player
            return (_actor.Position - camera.Position);
        }

        public abstract void Attack();

        public override bool TakeDamage(float damage, T2DSceneObject sourceObject)
        {
            if(!Alive)
                return false;

            float startHealth = _health;

            _health -= damage;
            _health = MathHelper.Clamp(_health, 0, _maxHealth);

            if (_health == 0)
                _die(startHealth - _health, sourceObject);
            else
                _tookDamage(startHealth - _health, sourceObject);

            return true;
        }

        protected override void _die(float damage, T2DSceneObject sourceObject)
        {
            base._die(damage, sourceObject);

            // if this object was spawned, tell it's spawned controller not to recover
            if (_actor.TestObjectType(PlatformerData.SpawnedObjectType))
            {
                CheckpointSystemSpawnedObjectComponent spawned = _actor.Components.FindComponent<CheckpointSystemSpawnedObjectComponent>();

                if (spawned != null)
                    spawned.Recover = false;
            }

            DropCrystals();
        }

        private void DropCrystals()
        {
            if (crystalTemplate != null)
            {
                T2DSceneObject crystal;
                Random rand = new Random();

                for (int i = 0; i < numDroppedCrystals; ++i)
                {
                    crystal = crystalTemplate.Clone() as T2DSceneObject;
                    crystal.Position = SceneObject.Position + new Vector2(rand.Next(-10, 11), 0);
                    TorqueObjectDatabase.Instance.Register(crystal);
                }
            }
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // give them a big enough ground y threshold so they don't jitter
            _groundCheckYThreshold = 2.0f;

            foreach (IBehavior behavior in actorBehavior)
                behavior.Initialize(owner);

            if (actorBehavior.Count > 0)
            {
                actorBehavior[0].Controller.PossessMover(this);
                actorBehavior[0].Controller.ActorSpawned(this);
            }

            SceneObject.SetObjectType(PlatformerData.EnemyObjectType, true);

            SceneObject.Collision.CollidesWith += ExtPlatformerData.MeleeDamageObjectType;//.DamageRegionObjectType;
            readyToAttack = true;

            return true;
        }
    }
}
