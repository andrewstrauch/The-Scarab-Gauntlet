//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A basic inventory class that allows the dynamic creation and assignment of an unlimited number of different types of items.
    /// Also has basic weight/item capacity functionality. This system works on the concept of an inventory as a container with slots.
    /// A slot has to be created for each type of item and the individual slots keep track of what they're holding. You can optionally
    /// have an inventory auto-manage its item slots, in which case when you add an item to the inventory a slot will be created for
    /// that item automatically.
    /// </summary>
    public class Inventory
    {
        //======================================================
        #region Constructors

        public Inventory()
        {
        }

        public Inventory(TorqueObject owner)
        {
            _owner = owner;
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The owner of this inventory.
        /// </summary>
        public TorqueObject Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        /// <summary>
        /// Generated list of all item slots in this inventory.
        /// </summary>
        public List<InventoryItemSlot> ItemSlotList
        {
            get
            {
                List<InventoryItemSlot> tmpList = new List<InventoryItemSlot>();

                foreach (DictionaryEntry entry in _itemSlots)
                {
                    InventoryItemSlot item = entry.Value as InventoryItemSlot;

                    if (item == null)
                        continue;

                    tmpList.Add(item);
                }

                return tmpList;
            }
        }

        /// <summary>
        /// Generated list of all non-empty item slots in this inventory.
        /// </summary>
        public List<InventoryItemSlot> ContainedItemList
        {
            get
            {
                List<InventoryItemSlot> tmpList = new List<InventoryItemSlot>();

                foreach (DictionaryEntry entry in _itemSlots)
                {
                    InventoryItemSlot item = entry.Value as InventoryItemSlot;

                    if (item == null)
                        continue;

                    if (item.NotEmpty)
                        tmpList.Add(item);
                }

                return tmpList;
            }
        }

        /// <summary>
        /// The total weight of all objects contained in this inventory as specified by the contained item slots.
        /// </summary>
        public float TotalWeight
        {
            get
            {
                float total = 0.0f;

                foreach (DictionaryEntry entry in _itemSlots)
                {
                    InventoryItemSlot item = entry.Value as InventoryItemSlot;

                    if (item == null)
                        continue;

                    if (item.WeighByCount)
                        total += item.Weight * item.Count;
                    else if (item.NotEmpty)
                        total += item.Weight;
                }

                return total;
            }
        }

        /// <summary>
        /// The total weight capacity of this inventory container.
        /// </summary>
        public float WeightCapacity
        {
            get { return _weightCapacity; }
            set { _weightCapacity = value; }
        }

        /// <summary>
        /// Specifies whether or not this inventory should automatically manage its slots. By default that entails creating new basic item 
        /// slots when an item is added without a slot, and enforcing an infinite max count on all item slots.
        /// </summary>
        public bool AutoManageSlots
        {
            get { return _autoManageSlots; }
            set { _autoManageSlots = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Registers a new item slot with the inventory.
        /// </summary>
        /// <param name="newItemSlot">The item slot to add to this inventory.</param>
        /// <param name="name">The name of the item to associate with the specified slot.</param>
        public void RegisterItemSlot(InventoryItemSlot newItemSlot, string name)
        {
            if (!HasItemSlot(name))
            {
                newItemSlot.Container = this;
                _itemSlots.Add(name, newItemSlot);
            }
        }

        /// <summary>
        /// Returns whether or not there is an item slot associated with the specified name.
        /// </summary>
        /// <param name="name">The name for which to check for an item slot.</param>
        /// <returns>True if an item slot exists associated with the specified name.</returns>
        public bool HasItemSlot(string name)
        {
            return _itemSlots.Contains(name);
        }

        /// <summary>
        /// Returns whether or not the item slot for the specified name is non-empty.
        /// </summary>
        /// <param name="name">The name of the item slot to check.</param>
        /// <returns>True if the specified item slot is not empty.</returns>
        public bool ContainsItem(string name)
        {
            return (_itemSlots[name] as InventoryItemSlot).NotEmpty;
        }

        /// <summary>
        /// Add one item of the specified type.
        /// </summary>
        /// <param name="itemName">The name of the item to add.</param>
        /// <returns>True if the item was successfully added.</returns>
        public virtual bool AddItem(string itemName)
        {
            return AddItem(itemName, 1);
        }

        /// <summary>
        /// Add the specified amount of the specified item type.
        /// </summary>
        /// <param name="itemName">The name of the item to add.</param>
        /// <param name="amount">The amount of the specified item to add.</param>
        /// <returns>True if the items were successfully added.</returns>
        public virtual bool AddItem(string itemName, int amount)
        {
            if (_autoManageSlots)
                _manageSlotFor(itemName, amount);

            if (!CanCarryMore(itemName, amount))
                return false;

            Item(itemName).Count += amount;
            return true;
        }

        /// <summary>
        /// Returns whether or not the specified amount of the specified item would take the inventory over its weight limit if added.
        /// </summary>
        /// <param name="itemName">The name of the item to check.</param>
        /// <param name="amount">The amount of the item to check.</param>
        /// <returns>True if there is room in the inventory for the specified amount of the specified item.</returns>
        public bool CanCarryMore(string itemName, int amount)
        {
            return CanCarryMore(Item(itemName), amount);
        }

        /// <summary>
        /// Returns whether or not the specified amount of the specified item would take the inventory over its weight limit if added.
        /// </summary>
        /// <param name="itemSlot">The item slot to check.</param>
        /// <param name="amount">The amount of the item to check.</param>
        /// <returns>True if there is room in the inventory for the specified amount of the specified item.</returns>
        public bool CanCarryMore(InventoryItemSlot itemSlot, int amount)
        {
            if (itemSlot == null)
                return false;

            if (itemSlot.Count + amount > itemSlot.MaxCount)
                return itemSlot.OnMaxCountReached(amount);

            if (itemSlot.WeighByCount)
            {
                if (TotalWeight + ((itemSlot.Count + amount) * itemSlot.Weight) < WeightCapacity)
                    return _onWeightCapacityReached(itemSlot, amount);
            }
            else if (itemSlot.NotEmpty || TotalWeight + itemSlot.Weight < WeightCapacity)
            {
                return true;
            }
            else
            {
                return _onWeightCapacityReached(itemSlot, amount);
            }


            return false;
        }

        /// <summary>
        /// Returns the item slot associated with the specified item name.
        /// </summary>
        /// <param name="name">The name of the item for which to retrieve an item slot.</param>
        /// <returns>The item slot associated with the specified name.</returns>
        public InventoryItemSlot Item(string name)
        {
            return _itemSlots[name] as InventoryItemSlot;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Callback called when the collection of an item would take the inventory over its weight limit.
        /// </summary>
        /// <param name="itemSlot">The item whos collectoin would brought the container over its weight capacity.</param>
        /// <param name="amount">The amount of the item in question.</param>
        /// <returns>True if collecting the specified amount of the specified item should be allowed.</returns>
        protected virtual bool _onWeightCapacityReached(InventoryItemSlot itemSlot, int amount)
        {
            return false;
        }

        /// <summary>
        /// Callback called if AutoManageSlots is true, allowing the inventory to do specific tasks depending on the item type.
        /// Override this if you wish to elaborate on the AutoManageSlots functionality.
        /// </summary>
        /// <param name="itemName">The name of the item slot to be managed.</param>
        /// <param name="amount">The amount of the specified type of item to be added.</param>
        protected virtual void _manageSlotFor(string itemName, int amount)
        {
            if (Item(itemName) == null)
                RegisterItemSlot(new InventoryItemSlot(amount), itemName);

            Item(itemName).MaxCount = Item(itemName).Count + amount;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected TorqueObject _owner;
        private Hashtable _itemSlots = new Hashtable();
        protected float _weightCapacity = 1.0f;
        protected bool _autoManageSlots = false;

        #endregion
    }

    /// <summary>
    /// A basic inventory slot for use with the Inventory class. 
    /// </summary>
    public class InventoryItemSlot
    {
        //======================================================
        #region Constructors

        public InventoryItemSlot()
        {
        }

        public InventoryItemSlot(int maxCount)
        {
            MaxCount = maxCount;
        }

        public InventoryItemSlot(int count, int maxCount)
            : this(maxCount)
        {
            Count = count;
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The Inventory that this slot belongs to.
        /// </summary>
        public Inventory Container
        {
            get { return _container; }
            set { _container = value; }
        }

        /// <summary>
        /// The current number of this item contained in the slot.
        /// </summary>
        public int Count
        {
            get { return _count; }
            set
            {
                _count = value;
                _count = (int)MathHelper.Clamp(_count, 0, _maxCount);
            }
        }

        /// <summary>
        /// The total amount this slot is able to hold.
        /// </summary>
        public int MaxCount
        {
            get { return _maxCount; }
            set { _maxCount = value; }
        }

        /// <summary>
        /// True if the Count of this slot is greater than zero.
        /// </summary>
        public bool NotEmpty
        {
            get { return _count > 0; }
        }

        /// <summary>
        /// The weight of this slot. If WeighByCount is true, then this will be scaled by Count.
        /// </summary>
        public float Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }

        /// <summary>
        /// Specifies whether or not the weight of this slot will be based on the current Count.
        /// </summary>
        public bool WeighByCount
        {
            get { return _weighByCount; }
            set { _weighByCount = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Virtual function to allow derived classes to have Use methods that can be called via an Inventory.
        /// </summary>
        public virtual void Use() { }

        /// <summary>
        /// Virtual function to allow derived classes to have Equip methods that can be called via an Inventory.
        /// </summary>
        public virtual void Equip() { }

        /// <summary>
        /// Virtual function to allow derived classes to have Unequip methods that can be called via an Inventory.
        /// </summary>
        public virtual void Unequip() { }

        /// <summary>
        /// Reduces the Count by the specified amount.
        /// </summary>
        /// <param name="amount">The number of items to drop.</param>
        public virtual void Drop(int amount)
        {
            Count -= amount;
        }

        /// <summary>
        /// Forces the slot to drop all items.
        /// </summary>
        public void DropAll()
        {
            Drop(_count);
        }

        /// <summary>
        /// Callback called when the Inventory wants to add an amount of items that would put this slot over its MaxCount.
        /// Default is to always deny the items.
        /// </summary>
        /// <param name="amount">The amount of items that the Inventory wants to put in this slot to put it over MaxCount.</param>
        /// <returns>True if the Inventory should be allowed to add the amount to the slot.</returns>
        public virtual bool OnMaxCountReached(int amount)
        {
            return false;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        protected Inventory _container;
        protected int _count = 0;
        protected int _maxCount = 1;
        protected float _weight = 0.0f;
        protected bool _weighByCount = false;

        #endregion
    }
}
