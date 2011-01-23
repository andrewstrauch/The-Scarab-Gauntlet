//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Methods used for 2D based collision determination.
    /// </summary>
    public class Collision2D
    {
        public static bool IntersectMovingPolyPoly(
                float elapsedTime, Vector2[] srcVertexList, Vector2[] dstVertexList,
                Vector2 srcPosition, Vector2 dstPosition,
                float srcRotation, float dstRotation,
                Vector2 srcVelocity, Vector2 dstVelocity,
                ref Vector2 collisionPosition, ref Vector2 collisionNormal, ref Vector2 collisionPenetration, ref float time)
        {
            if (srcVertexList == null || dstVertexList == null)
                return false;

            int srcVertexCount = srcVertexList.Length;
            int dstVertexCount = dstVertexList.Length;

            //src
            Rotation2D srcRot = new Rotation2D(MathHelper.ToRadians(srcRotation));

            //dst
            Rotation2D dstInverseRot = new Rotation2D(MathHelper.ToRadians(-dstRotation));
            Rotation2D refLocalRot = srcRot * dstInverseRot;

            Vector2 offset = srcPosition - dstPosition;
            Vector2 refLocalOffset = dstInverseRot.Rotate(offset);

            Vector2 velocity = srcVelocity - dstVelocity;
            Vector2 refLocalVelocity = dstInverseRot.Rotate(velocity);

            float refVelSqr = Vector2.Dot(refLocalVelocity, refLocalVelocity);

            int axesCount = 0;

            float fullTimeStep = elapsedTime;

            float Epsilon = 0.0001f;

            // Ignore small velocities!
            if (refVelSqr > Epsilon)
            {
                // Set Axis.
                _vertexAxis[axesCount] = new Vector2(-refLocalVelocity.Y, refLocalVelocity.X);
                // Check Interval Intersection.
                if (!_CheckIntervalIntersection(
                        srcVertexList,
                        dstVertexList,
                        ref _vertexAxis[axesCount],
                        ref refLocalOffset, ref refLocalVelocity, ref refLocalRot,
                        ref _timeAxis[axesCount], fullTimeStep))
                    // No Collision!
                    return false;

                // Next Axes.
                axesCount++;
            }

            // Test Seperation Axes for Source Object.
            // NOTE:- We ignore if it's a point!
            if (srcVertexCount > 1)
            {
                for (int j = srcVertexCount - 1, i = 0; i < srcVertexCount; j = i, i++)
                {
                    // fetch the edge
                    Vector2 dP = srcVertexList[i] - srcVertexList[j];
                    Vector2 dP2 = new Vector2(-dP.Y, dP.X);
                    _vertexAxis[axesCount] = refLocalRot.Rotate(dP2);

                    // check interval intersection
                    if (!_CheckIntervalIntersection(srcVertexList, dstVertexList, ref _vertexAxis[axesCount], ref refLocalOffset, ref refLocalVelocity, ref refLocalRot, ref _timeAxis[axesCount], fullTimeStep))
                        return false;

                    //next axes
                    axesCount++;
                }
            }

            // Test Seperation Axes for Destination Object.
            // NOTE:- We ignore if it's a point!
            if (dstVertexCount > 1)
            {
                for (int j = dstVertexCount - 1, i = 0; i < dstVertexCount; j = i, i++)
                {
                    // fetch the edge
                    Vector2 dP = dstVertexList[i] - dstVertexList[j];

                    // set the axis
                    _vertexAxis[axesCount] = new Vector2(-dP.Y, dP.X);

                    if (!_CheckIntervalIntersection(srcVertexList, dstVertexList, ref _vertexAxis[axesCount], ref refLocalOffset, ref refLocalVelocity, ref refLocalRot, ref _timeAxis[axesCount], fullTimeStep))
                        return false;

                    //next axes
                    axesCount++;
                }
            }

            // Test Special-Case for Segments for Destination Object.
            if (dstVertexCount == 2)
            {
                // Set Axis.
                _vertexAxis[axesCount] = (dstVertexList[1] - dstVertexList[0]);

                // Check Interval Intersection.
                if (!_CheckIntervalIntersection(srcVertexList, dstVertexList, ref _vertexAxis[axesCount], ref refLocalOffset, ref refLocalVelocity, ref refLocalRot, ref _timeAxis[axesCount], fullTimeStep))
                    // No Collision!
                    return false;

                // Next Axes.
                axesCount++;
            }

            // Test Special-Case for Segments for Source Object.
            if (srcVertexCount == 2)
            {
                // Set Axis.

                Vector2 segment = (srcVertexList[1] - srcVertexList[0]);
                _vertexAxis[axesCount] = refLocalRot.Rotate(segment);

                // Check Interval Intersection.
                if (!_CheckIntervalIntersection(srcVertexList, dstVertexList, ref _vertexAxis[axesCount], ref refLocalOffset, ref refLocalVelocity, ref refLocalRot, ref _timeAxis[axesCount], fullTimeStep))
                    // No Collision!
                    return false;

                // Next Axes.
                axesCount++;
            }

            // find minimum seperation distance
            if (!_FindCollisionTime(_vertexAxis, _timeAxis, axesCount, ref collisionNormal, ref time))
                return false;

            // Respace Normal.      
            collisionNormal = dstInverseRot.Unrotate(collisionNormal);

            // need this below (simple transpose)
            Rotation2D dstRot = dstInverseRot.Invert();

            // compute offset between poly centers for following calculation
            Vector2 polyCenter = new Vector2();
            foreach (Vector2 v in srcVertexList)
                polyCenter += v;
            polyCenter *= 1.0f / (float)srcVertexList.Length;
            Vector2 polyOffset = srcPosition + srcRot.Rotate(polyCenter);
            polyCenter = new Vector2();
            foreach (Vector2 v in dstVertexList)
                polyCenter += v;
            polyCenter *= 1.0f / (float)dstVertexList.Length;
            polyOffset -= dstPosition + dstRot.Rotate(polyCenter);

            // Make sure the collision polygons are pushed away.
            // NOTE: This is the overlap case.
            if (Vector2.Dot(collisionNormal, polyOffset) < 0.0f)
                collisionNormal = -collisionNormal;

            int contactCount = 0;
            if (!_FindContactPoints(
                    srcVertexList, srcPosition, srcVelocity, ref srcRot,
                    dstVertexList, dstPosition, dstVelocity, ref dstRot,
                    collisionNormal, time, _srcContacts, _dstContacts, out contactCount))
                // No Support Points!
                return false;

            collisionPosition = 0.5f * (_srcContacts[0] + _dstContacts[0]);
            collisionPenetration = _dstContacts[0] - _srcContacts[0];
            if (contactCount == 2)
            {
                collisionPosition = 0.5f * collisionPosition + 0.25f * (_srcContacts[1] + _dstContacts[1]);
                Vector2 collisionPenetration2 = _dstContacts[1] - _srcContacts[1];
                if (collisionPenetration2.LengthSquared() > collisionPenetration.LengthSquared())
                    collisionPenetration = collisionPenetration2;
            }

            // return true to indicate collision            
            return true;
        }

        public static float PerpDot(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        static bool _FindCollisionTime(Vector2[] vertexAxis, float[] timeAxis, int axesCount, ref Vector2 collisionNormal, ref float collisionTime)
        {
            Assert.Fatal(axesCount > 0, "_FindCollisionTime - no axes for object!");

            // reset minimum index
            int minimumIndex = -1;

            // Note: this handles two cases:
            // 1) Overlap case where every separating axis shows overlap (negative "time").
            // By the separating axis theorem, this means the polys overlap since all the needed
            // axes are tested here.  This code path occurs at the bottom of this method.
            // In this case we return true and a negative collision time (for separation amount).
            // 2) Non-overlap case (for loop below) in which case at least one axis shows separation.
            // In this case we want to find the axis which goes negative _LAST_.  This will find the
            // time at which the collision will actually occur.  This means that we are searching for
            // the largest axis time in order to find the collision time.

            collisionTime = 0.0f;

            for (int i = 0; i < axesCount; i++)
            {
                // check for future collision
                if (timeAxis[i] > collisionTime)
                {
                    // note index
                    minimumIndex = i;
                    // note time
                    collisionTime = timeAxis[i];
                }
            }

            // found a collision?
            if (minimumIndex != -1)
            {
                collisionNormal = vertexAxis[minimumIndex];
                collisionNormal.Normalize();
                return true;
            }

            // nothing found so find overlaps

            for (int i = 0; i < axesCount; i++)
            {
                // Note: When time is negative it is actually the dot product of overlap vector and
                // vertex axis, so we divide time by vertex axis length to get actual distance.
                float length = vertexAxis[i].Length();
                timeAxis[i] /= length;

                // check for collision
                if (timeAxis[i] > collisionTime || minimumIndex == -1)
                {
                    // note index
                    minimumIndex = i;
                    // note time
                    collisionTime = timeAxis[i];
                }
            }

            // note normal
            collisionNormal = vertexAxis[minimumIndex];
            collisionNormal.Normalize();

            // return status
            return minimumIndex != -1;

        }
        static bool _CheckIntervalIntersection(Vector2[] srcVertexList, Vector2[] dstVertexList, ref Vector2 vertexAxis, ref Vector2 refLocalOffset, ref Vector2 refLocalVelocity, ref Rotation2D refLocalRot, ref float timeAxis, float collisionTime)
        {
            // calculate intervals for source/destination
            Vector2 vertexAxisRotated = refLocalRot.Unrotate(vertexAxis);

            // Get the interval of the polygons projected onto the axis.  Min returned in x, max in y.
            Vector2 srcProj = _CalculateInterval(srcVertexList, ref vertexAxisRotated);
            Vector2 dstProj = _CalculateInterval(dstVertexList, ref vertexAxis);

            // add reference offset
            float srcOffset = Vector2.Dot(refLocalOffset, vertexAxis);

            srcProj += new Vector2(srcOffset, srcOffset);

            // calculate intervals

            // Calculate delta of min of one interval with max of the other.
            // Note that if the intervals overlap then both deltas must be negative.
            // I.e., delta>0 --> intervals don't overlap because one min greater than
            // the other max.
            float delta0 = srcProj.X - dstProj.Y;
            float delta1 = dstProj.X - srcProj.Y;

            // Are we seperated?
            if (delta0 > 0.0f || delta1 > 0.0f)
            {
                // Yes, so test the dynamic intervals

                // calculate speed along a particular axis
                float speed = Vector2.Dot(refLocalVelocity, vertexAxis);

                float epsilon = 0.0001f;
                // ignore small speeds
                if (Math.Abs(speed) < epsilon)
                    return false;

                // The smallest absolute value must be the separation.  The reason is that one
                // delta measures separation between intervals, the other measures the span of
                // the two intervals (we want separation, which is always smaller).  Convert separation
                // into time by dividing by the speed (adjust sign depending on whether we are going
                // from source to dest or not).
                timeAxis = delta0 * delta0 < delta1 * delta1 ? -delta0 / speed : delta1 / speed;

                // successful collision?
                return timeAxis >= 0.0f && timeAxis <= collisionTime;
            }
            else
            {
                // No, so we're overlapped.
                //
                // Let's get the interval of the smallest |delta0| and |delta1| and
                // encode it as a negative value to signify an overlap rather than
                // a disjoint collision in forward-time.

                timeAxis = (delta0 > delta1) ? delta0 : delta1;

                // Successful Collision!
                return true;
            }
        }

        // calculate the projection of a polygon onto an axii
        static Vector2 _CalculateInterval(Vector2[] vertexList, ref Vector2 axii)
        {
            float minProj = Vector2.Dot(vertexList[0], axii);
            float maxProj = minProj;
            int vertexCount = vertexList.Length;
            for (int i = 1; i < vertexCount; i++)
            {
                // calc projection
                float proj = Vector2.Dot(vertexList[i], axii);

                // adjust projection interval
                if (proj < minProj)
                    minProj = proj;
                else if (proj > maxProj)
                    maxProj = proj;
            }

            return new Vector2(minProj, maxProj);
        }

        static bool _FindContactPoints(
            Vector2[] srcPoly, Vector2 srcPosition, Vector2 srcVelocity, ref Rotation2D srcRotation,
            Vector2[] dstPoly, Vector2 dstPosition, Vector2 dstVelocity, ref Rotation2D dstRotation,
            Vector2 collisionNormal, float collisionTime, Vector2[] srcContacts, Vector2[] dstContacts, out int contactCount)
        {
            // Reset Contact Count.
            contactCount = 0;

            // Find Source Support Points.
            int srcSupportCount = _FindSupportPoints(srcPoly, srcPosition, srcVelocity, ref srcRotation, collisionNormal, collisionTime, _srcSupportPoints);

            // No contacts without support-points!
            if (srcSupportCount == 0)
                return false;

            // Find Destination Support Points.
            int dstSupportCount = _FindSupportPoints(dstPoly, dstPosition, dstVelocity, ref dstRotation, -collisionNormal, collisionTime, _dstSupportPoints);

            // No contacts without support-points!
            if (dstSupportCount == 0)
                return false;

            // Trivial Contact Check.
            if (srcSupportCount == 1 && dstSupportCount == 1)
            {
                // Simple Contact.
                srcContacts[contactCount] = _srcSupportPoints[0];
                dstContacts[contactCount] = _dstSupportPoints[0];
                // Increase Contact Count.
                contactCount++;
                // Return Conversion.
                return true;
            }

            // Calculate Perpendicular Normal.
            Vector2 perpNormal = new Vector2(-collisionNormal.Y, collisionNormal.X);

            // Calculate Source/Destination Points.
            float srcMin = Vector2.Dot(_srcSupportPoints[0], perpNormal);
            float srcMax = srcMin;
            float dstMin = Vector2.Dot(_dstSupportPoints[0], perpNormal);
            float dstMax = dstMin;

            // Check for Two support-points for source.
            if (srcSupportCount == 2)
            {
                // Set Max.
                srcMax = Vector2.Dot(_srcSupportPoints[1], perpNormal);

                // Reoder (if needed).
                if (srcMax < srcMin)
                {
                    // Swap.
                    TorqueUtil.Swap(ref srcMin, ref srcMax);
                    // Swap Support Points.
                    TorqueUtil.Swap(ref _srcSupportPoints[0], ref _srcSupportPoints[1]);
                }
            }

            // Check for Two support-points for destination.
            if (dstSupportCount == 2)
            {
                // Set Max.
                dstMax = Vector2.Dot(_dstSupportPoints[1], perpNormal);

                // Reoder (if needed).
                if (dstMax < dstMin)
                {
                    // Swap.
                    TorqueUtil.Swap(ref dstMin, ref dstMax);
                    // Swap Support Points.
                    TorqueUtil.Swap(ref _dstSupportPoints[0], ref _dstSupportPoints[1]);
                }
            }

            // Contacts?
            if (srcMin > dstMax || dstMin > srcMax)
            {
                // if collision time is negative then we are fully overlapped, in which
                // case we don't do this test (this test is whether or not two oncoming edges
                // will pass through each other).
                if (collisionTime >= 0.0f)
                {
                    // Nope!
                    return false;
                }
            }

            // Projected Segment.
            Vector2 projSeg;

            if (srcMin > dstMin)
            {
                // Project Src->Dst.
                _ProjectPointToSegment(_srcSupportPoints[0], _dstSupportPoints[0], _dstSupportPoints[1], out projSeg);

                // Note Contacts.
                srcContacts[contactCount] = _srcSupportPoints[0];
                dstContacts[contactCount] = projSeg;

                // Increase Contact Count.
                contactCount++;
            }
            else
            {
                // Project Dst->Src.
                _ProjectPointToSegment(_dstSupportPoints[0], _srcSupportPoints[0], _srcSupportPoints[1], out projSeg);

                // Note Contacts.
                srcContacts[contactCount] = projSeg;
                dstContacts[contactCount] = _dstSupportPoints[0];

                // Increase Contact Count.
                contactCount++;
            }

            // Other Variants.
            if (srcMin != srcMax && dstMin != dstMax)
            {
                if (srcMax < dstMax)
                {
                    // Project.
                    _ProjectPointToSegment(_srcSupportPoints[1], _dstSupportPoints[0], _dstSupportPoints[1], out projSeg);

                    // Note Contacts.
                    srcContacts[contactCount] = _srcSupportPoints[1];
                    dstContacts[contactCount] = projSeg;

                    // Increase Contact Count.
                    contactCount++;
                }
                else
                {
                    // Project.
                    _ProjectPointToSegment(_dstSupportPoints[1], _srcSupportPoints[0], _srcSupportPoints[1], out projSeg);

                    // Note Contacts.
                    srcContacts[contactCount] = projSeg;
                    dstContacts[contactCount] = _dstSupportPoints[1];

                    // Increase Contact Count.
                    contactCount++;
                }
            }

            // Return Contacts.
            return true;
        }

        //----------------------------------------------------------------------------------------------- 
        // Find Support Points.
        // NOTE:-   This is a convex shape along a specified direction.
        //----------------------------------------------------------------------------------------------- 
        static int _FindSupportPoints(
            Vector2[] poly, Vector2 position, Vector2 velocity, ref Rotation2D rotation,
            Vector2 collisionNormal, float collisionTime, Vector2[] supportPoints)
        {
            // Calculate Normal.
            Vector2 normal = rotation.Unrotate(collisionNormal);

            // Reset Direction.
            float supportMin = _support[0] = Vector2.Dot(poly[0], normal);

            // Interate Polygon.
            for (int i = 1; i < poly.Length; i++)
            {
                // Calculate.
                _support[i] = Vector2.Dot(poly[i], normal);

                // Check For Minimum.
                if (_support[i] < supportMin)
                    supportMin = _support[i];
            }

            // The limit here is two support-points only.
            // If we find more then we use the extremums.
            int supportCount = 0;

            // Set Threshold.
            float threshold = 1.0e-3f;

            // Reset Sign Flag.
            bool sign = false;

            // Calculate Perpendicular Normal.
            Vector2 perpNormal = new Vector2(-collisionNormal.Y, collisionNormal.X);

            // Interate Polygon.
            for (int i = 0; i < poly.Length; i++)
            {
                // Check Contact.
                if (_support[i] < supportMin + threshold)
                {
                    // Transform Contact to World-Space.
                    Vector2 contact;
                    _TransformContact(poly[i], position, velocity, ref rotation, collisionTime, out contact);
                    // Contact Dot.
                    float contactDot = Vector2.Dot(contact, perpNormal);
                    // Less than two supports?
                    if (supportCount < 2)
                    {
                        // Yes, so note contact.
                        _supportMinMax[supportCount] = contactDot;
                        supportPoints[supportCount] = contact;
                        // Increase Support Count.
                        supportCount++;

                        // Note Sign for two contacts.
                        if (supportCount > 1)
                            sign = (_supportMinMax[1] > _supportMinMax[0]);
                    }
                    else
                    {
                        int idx0 = 0;
                        int idx1 = 1;
                        if (!sign)
                            TorqueUtil.Swap(ref idx0, ref idx1);
                        if (contactDot < _supportMinMax[idx0])
                        {
                            _supportMinMax[idx0] = contactDot;
                            supportPoints[idx0] = contact;

                        }
                        else if (contactDot > _supportMinMax[idx1])
                        {
                            _supportMinMax[idx1] = contactDot;
                            supportPoints[idx1] = contact;
                        }

                    }
                }
            }

            // Return Support Count.
            return supportCount;
        }

        //----------------------------------------------------------------------------------------------- 
        // Transform Contact-Point into World-Space Collision.
        //----------------------------------------------------------------------------------------------- 
        static void _TransformContact(Vector2 vertex, Vector2 position, Vector2 velocity, ref Rotation2D rotation, float collisionTime, out Vector2 contact)
        {
            // Do Transformation.
            contact = position + rotation.Rotate(vertex);

            // Check Time.
            if (collisionTime > 0.0f)
                contact += velocity * collisionTime;
        }

        //----------------------------------------------------------------------------------------------- 
        // Find closest point on a segment to a vertex
        //----------------------------------------------------------------------------------------------- 
        static float _ProjectPointToSegment(Vector2 V, Vector2 A, Vector2 B, out Vector2 W)
        {
            // Deltas.
            Vector2 aV = V - A;
            Vector2 aB = B - A;

            // Time.
            float time = MathHelper.Clamp(Vector2.Dot(aV, aB) / Vector2.Dot(aB, aB), 0.0f, 1.0f);

            // Finish Projection.
            W = A + time * aB;

            return time;
        }

        #region Static workspace for poly collisions
        static Vector2[] _vertexAxis = new Vector2[64];
        static float[] _timeAxis = new float[64];
        static Vector2[] _srcContacts = new Vector2[2];
        static Vector2[] _dstContacts = new Vector2[2];
        static Vector2[] _srcSupportPoints = new Vector2[2];
        static Vector2[] _dstSupportPoints = new Vector2[2];
        static float[] _support = new float[64];
        static float[] _supportMinMax = new float[2];
        #endregion
    }
}
