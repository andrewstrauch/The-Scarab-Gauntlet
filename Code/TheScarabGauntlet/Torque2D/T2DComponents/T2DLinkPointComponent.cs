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
    /// Add this component to a T2DSceneObject in order to add named link points
    /// to the object.
    /// </summary>
    [TorqueXmlSchemaType]
    public class T2DLinkPointComponent : TorqueComponent
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The T2DSceneObject which owns this component.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Add a named link point with given position and rotation.  Position is in unscaled
        /// object space, so -1 to 1 ranges over the entire object (irrespective of Size).
        /// </summary>
        /// <param name="name">Name of new link point.</param>
        /// <param name="position">Link point position.</param>
        /// <param name="rotation">Link point rotation.</param>
        public void AddLinkPoint(string name, Vector2 position, float rotation)
        {
            // if we already have this one, just update it
            SList<LinkPoint> walk = _linkPoints;
            while (walk != null)
            {
                if (walk.Val.Name == name)
                {
                    walk.Val.Position.Value = position;
                    walk.Val.Rotation.Value = rotation;
                    return;
                }
                walk = walk.Next;
            }
            LinkPoint link = new LinkPoint();
            link.Name = name;
            link.Position = new ValueInPlaceInterface<Vector2>(position);
            link.Rotation = new ValueInPlaceInterface<float>(rotation);
            if (Owner != null)
            {
                Owner.RegisterInterface(this, link.Position);
                Owner.RegisterInterface(this, link.Rotation);
            }

            SList<LinkPoint>.InsertFront(ref _linkPoints, link);
        }



        /// <summary>
        /// Determine whether named link point exists.
        /// </summary>
        /// <param name="name">Name of link point to query.</param>
        /// <returns>True if link point exists.</returns>
        public bool HasLinkPoint(string name)
        {
            SList<LinkPoint> walk = _linkPoints;
            while (walk != null)
            {
                if (walk.Val.Name == name)
                    return true;
                walk = walk.Next;
            }
            return false;
        }



        /// <summary>
        /// Get position and rotation of named link point, if it exists.
        /// </summary>
        /// <param name="name">Name of link point to query.</param>
        /// <param name="position">Link point position.</param>
        /// <param name="rotation">Link point rotation.</param>
        /// <returns>True if link point exists.</returns>
        public bool GetLinkPoint(string name, out Vector2 position, out float rotation)
        {
            Assert.Fatal(name != null && name != String.Empty, "link points must have a name");
            SList<LinkPoint> walk = _linkPoints;
            while (walk != null)
            {
                if (walk.Val.Name == name)
                {
                    position = walk.Val.Position.Value;
                    rotation = walk.Val.Rotation.Value;
                    return true;
                }
                walk = walk.Next;
            }
            position = Vector2.Zero;
            rotation = 0.0f;
            return false;
        }



        /// <summary>
        /// Get interfaces for position and rotation of named link point, if link point exists.
        /// </summary>
        /// <param name="name">Name of link point to query.</param>
        /// <param name="position">TorqueInterface for link point position.</param>
        /// <param name="rotation">TorqueInterface for link point rotation.</param>
        /// <returns>True if link point exists.</returns>
        public bool GetLinkPoint(string name, out ValueInterface<Vector2> position, out ValueInterface<float> rotation)
        {
            Assert.Fatal(name != null && name != String.Empty, "link points must have a name");
            SList<LinkPoint> walk = _linkPoints;
            while (walk != null)
            {
                if (walk.Val.Name == name)
                {
                    position = walk.Val.Position;
                    rotation = walk.Val.Rotation;
                    return true;
                }
                walk = walk.Next;
            }
            position = null;
            rotation = null;
            return false;
        }



        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            T2DLinkPointComponent obj2 = (T2DLinkPointComponent)obj;
            SList<LinkPoint> walk = _linkPoints;
            while (walk != null)
            {
                LinkPoint mp = new LinkPoint();
                mp.Name = walk.Val.Name;
                mp.Position = new ValueInPlaceInterface<Vector2>(walk.Val.Position.Value);
                mp.Rotation = new ValueInPlaceInterface<float>(walk.Val.Rotation.Value);
                SList<LinkPoint>.InsertFront(ref obj2._linkPoints, mp);
                walk = walk.Next;
            }
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            return true;
        }



        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }



        protected override void _RegisterInterfaces(TorqueObject owner)
        {
            Owner.RegisterCachedInterface("float", null, this, null);
            Owner.RegisterCachedInterface("vector2", null, this, null);
            base._RegisterInterfaces(owner);
        }



        protected override void _GetInterfaces(PatternMatch typeMatch, PatternMatch nameMatch, List<TorqueInterface> list)
        {
            if (typeMatch.TestMatch("float"))
            {
                SList<LinkPoint> walk = _linkPoints;
                while (walk != null)
                {
                    if (walk.Val.Name != null && walk.Val.Rotation != null && nameMatch.TestMatch(walk.Val.Name))
                    {
                        if (walk.Val.Rotation.Owner == null)
                            Owner.RegisterInterface(this, walk.Val.Rotation);
                        list.Add(walk.Val.Rotation);
                    }
                    walk = walk.Next;
                }
            }
            else if (typeMatch.TestMatch("vector2"))
            {
                SList<LinkPoint> walk = _linkPoints;
                while (walk != null)
                {
                    if (walk.Val.Name != null && walk.Val.Position != null && nameMatch.TestMatch(walk.Val.Name))
                    {
                        if (walk.Val.Position.Owner == null)
                            Owner.RegisterInterface(this, walk.Val.Position);
                        list.Add(walk.Val.Position);
                    }
                    walk = walk.Next;
                }
            }

            base._GetInterfaces(typeMatch, nameMatch, list);
        }



        // private interface for xml deserialization, here because we cannot deserialize the internal 
        // fields used by LinkPoints (SLists, ValueInPlace objects)
        [TorqueXmlDeserializeInclude] // needed because this property is internal
        [XmlElement(ElementName = "LinkPoints")]
        internal List<XmlLinkPoint> _XmlLinkPoints
        {
            get { return null; }
            set
            {
                Assert.Fatal(value != null, "Attempt to pass null to link point deserialzation interface");

                foreach (XmlLinkPoint l in value)
                    _AddLinkPoint(l);

                value.Clear();
            }
        }



        void _AddLinkPoint(XmlLinkPoint lp)
        {
            this.AddLinkPoint(lp.Name, lp.Position, lp.Rotation);
        }


        #endregion


        #region Private, protected, internal fields

        public struct LinkPoint
        {
            public String Name;
            public ValueInPlaceInterface<Vector2> Position;
            public ValueInPlaceInterface<float> Rotation;
        }



        [TorqueXmlSchemaType(Name = "LinkPoint")]
        internal class XmlLinkPoint
        {
            public String Name = null;
            public Vector2 Position = Vector2.Zero;
            public float Rotation = 0.0f;
        }



        SList<LinkPoint> _linkPoints;

        #endregion
    }
}
