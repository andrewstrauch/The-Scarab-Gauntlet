//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Utilities for generating and working with the view frustum.
    /// </summary>
    public class Frustum
    {
#if EXTRA_DIAGNOSTICS
        struct Stats
        {
            public int totalChecks;

            public int sphereFarNearReject;
            public int sphereConeReject;
            public int sphereConeAccept;

            public int checkLeft;
            public int checkRight;
            public int checkTop;
            public int checkBottom;
            public int checkNear;
            public int checkFar;

            public int outsideLeft;
            public int outsideRight;
            public int outsideTop;
            public int outsideBottom;
            public int outsideNear;
            public int outsideFar;

            public int passAllChecks;
        }
#endif


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Bounding box of the frustum.
        /// </summary>
        public Box3F Bounds
        {
            get { return _bounds; }
        }

        /// <summary>
        /// Distance of the far plane.
        /// </summary>
        public float FarDistance
        {
            get { return _farDist; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Returns whether or not a box intersects with the frustum.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="mat">The object to world space matrix. Use identity if the box is already in world space.</param>
        /// <param name="radius">The radius of the box.</param>
        /// <returns>True if the box intersects the frustum.</returns>
        public bool Intersects(Box3F box, Matrix mat, float radius)
        {
            // Note: use the long form of vector operations because they get inlined whereas operator version does not

#if EXTRA_DIAGNOSTICS
            _stats.totalChecks++;
#endif

            // Vector3 objPos = 0.5f * (box.Max + box.Min) - _cameraPos;
            Vector3 objPos;
            Vector3.Add(ref box.Max, ref box.Min, out objPos);
            Vector3.Multiply(ref objPos, 0.5f, out objPos);
            Vector3.Subtract(ref objPos, ref _cameraPos, out objPos);

            float dist;
            Vector3.Dot(ref objPos, ref _cameraY, out dist);
            if (dist < -radius || dist > _farDist + radius)
            {
                // sphere behind near plane or beyond far plane
#if EXTRA_DIAGNOSTICS
                _stats.sphereFarNearReject++;
#endif
                return false;
            }

            // Short form of code which doesn't get inlined:
            // Vector3 conePos = -radius * _rejectInvSin * _camearY;
            // Vector3 offset = objPos - conePos;
            // float dy = Vector3.Dot(offset,_cameraY);
            // offset -= dy * _cameraY;
            // float dxSq = offset.LengthSquared();
            // Long form of code which does get inlined:
            float dy, dxSq;
            Vector3 conePos, offset, offsetY;
            Vector3.Multiply(ref _cameraY, -radius * _rejectInvSin, out conePos);
            Vector3.Subtract(ref objPos, ref conePos, out offset);
            Vector3.Dot(ref offset, ref _cameraY, out dy);
            Vector3.Multiply(ref _cameraY, dy, out offsetY);
            Vector3.Subtract(ref offset, ref offsetY, out offset);
            dxSq = offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;
            if (dy * dy < _rejectCos * _rejectCos * (dxSq + dy * dy))
            {
                // early rejection, outside outer cone...
#if EXTRA_DIAGNOSTICS
                _stats.sphereConeReject++;
#endif
                return false;
            }

            Vector3.Multiply(ref _cameraY, -radius * _acceptInvSin, out conePos);
            Vector3.Subtract(ref objPos, ref conePos, out offset);
            Vector3.Dot(ref offset, ref _cameraY, out dy);
            Vector3.Multiply(ref _cameraY, dy, out offsetY);
            Vector3.Subtract(ref offset, ref offsetY, out offset);
            dxSq = offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;
            if (dy * dy > _acceptCos * _acceptCos * (dxSq + dy * dy))
            {
                // early accept, inside inner cone...check far plane or near plane against box?
#if EXTRA_DIAGNOSTICS
                _stats.sphereConeAccept++;
#endif
                return true;
            }

            // Compare bounds to each of the frustum planes in turn. 
            float boxDistance;
#if EXTRA_DIAGNOSTICS
            _stats.checkLeft++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, _leftPlaneNormal, _leftPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideLeft++;
#endif
                return false;
            }

#if EXTRA_DIAGNOSTICS
            _stats.checkRight++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, _rightPlaneNormal, _rightPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideRight++;
#endif
                return false;
            }

#if EXTRA_DIAGNOSTICS
            _stats.checkTop++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, _topPlaneNormal, _topPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideTop++;
#endif
                return false;
            }

#if EXTRA_DIAGNOSTICS
            _stats.checkBottom++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, _bottomPlaneNormal, _bottomPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideBottom++;
#endif
                return false;
            }

#if EXTRA_DIAGNOSTICS
            _stats.checkNear++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, -_cameraY, _nearPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideNear++;
#endif
                return false;
            }

#if EXTRA_DIAGNOSTICS
            _stats.checkFar++;
#endif
            if (!MathUtil.Collision.TestOBBPlane(box, mat, _cameraY, _farPlaneDist, out boxDistance) && boxDistance > 0.0f)
            {
                // fully outside this plane
#if EXTRA_DIAGNOSTICS
                _stats.outsideFar++;
#endif
                return false;
            }

            // passed all the tests, so must be inside frustum
#if EXTRA_DIAGNOSTICS
            _stats.passAllChecks++;
#endif
            return true;
        }

        /// <summary>
        /// Unused
        /// </summary>
        /// <param name="center"></param>
        /// <param name="extent"></param>
        /// <param name="planeFlags"></param>
        public void IntersectCubeFrustums(ref Vector3 center, ref Vector3 extent, ref int planeFlags)
        {
            int XYPosOutside = 1 << 0;
            int XYPosInside = 1 << 1;
            int XYPosOverlap = 1 << 2;

            int XYNegOutside = 1 << 3;
            int XYNegInside = 1 << 4;
            int XYNegOverlap = 1 << 5;

            int XZPosOutside = 1 << 6;
            int XZPosInside = 1 << 7;
            int XZPosOverlap = 1 << 8;

            int XZNegOutside = 1 << 9;
            int XZNegInside = 1 << 10;
            int XZNegOverlap = 1 << 11;

            int YZPosOutside = 1 << 12;
            int YZPosInside = 1 << 13;
            int YZPosOverlap = 1 << 14;

            int YZNegOutside = 1 << 15;
            int YZNegInside = 1 << 16;
            int YZNegOverlap = 1 << 17;

            // xy planes
            float rad = extent.X + extent.Y;

            float d = center.Y - center.X;
            if (d > rad)
                planeFlags |= XYPosOutside;
            else if (d < -rad)
                planeFlags |= XYPosInside;
            else
                planeFlags |= XYPosOverlap;

            d = center.X + center.Y;
            if (d > rad)
                planeFlags |= XYNegOutside;
            else if (d < -rad)
                planeFlags |= XYNegInside;
            else
                planeFlags |= XYNegOverlap;

            // xz planes
            rad = extent.X + extent.Z;

            d = center.Z - center.X;
            if (d > rad)
                planeFlags |= XZPosOutside;
            else if (d < -rad)
                planeFlags |= XZPosInside;
            else
                planeFlags |= XZPosOverlap;

            d = center.X + center.Z;
            if (d > rad)
                planeFlags |= XZNegOutside;
            else if (d < -rad)
                planeFlags |= XZNegInside;
            else
                planeFlags |= XZNegOverlap;

            // yz planes
            rad = extent.Y + extent.Z;

            d = center.Z - center.Y;
            if (d > rad)
                planeFlags |= YZPosOutside;
            else if (d < -rad)
                planeFlags |= YZPosInside;
            else
                planeFlags |= YZPosOverlap;

            d = center.Y + center.Z;
            if (d > rad)
                planeFlags |= YZNegOutside;
            else if (d < -rad)
                planeFlags |= YZNegInside;
            else
                planeFlags |= YZNegOverlap;
        }

        /// <summary>
        /// Generate the frustum.
        /// </summary>
        /// <param name="nearDist">The distance to the near plane.</param>
        /// <param name="farDist">The distance to the far plane.</param>
        /// <param name="fov">The field of view of the frustum.</param>
        /// <param name="aspectRatio">The aspect ratio.</param>
        /// <param name="camToWorld">The camera transform.</param>
        public void SetFrustum(float nearDist, float farDist, float fov, float aspectRatio, Matrix camToWorld)
        {
            _nearDist = nearDist;
            _farDist = farDist;
            float left = -farDist * (float)Math.Tan(fov * 0.5f) * aspectRatio;
            float right = -left;
            float bottom = left / aspectRatio;
            float top = -bottom;

            Vector3 farPosLeftUp = new Vector3(left, farDist, top);
            Vector3 farPosLeftDown = new Vector3(left, farDist, bottom);
            Vector3 farPosRightUp = new Vector3(right, farDist, top);
            Vector3 farPosRightDown = new Vector3(right, farDist, bottom);
            _cameraPos = camToWorld.Translation;
            _cameraY = MatrixUtil.MatrixGetRow(1, ref camToWorld);

            farPosLeftUp = MatrixUtil.MatMulP(ref farPosLeftUp, ref camToWorld);
            farPosLeftDown = MatrixUtil.MatMulP(ref farPosLeftDown, ref camToWorld);
            farPosRightUp = MatrixUtil.MatMulP(ref farPosRightUp, ref camToWorld);
            farPosRightDown = MatrixUtil.MatMulP(ref farPosRightDown, ref camToWorld);

            _bounds = new Box3F(_cameraPos, _cameraPos);
            _bounds.Extend(farPosLeftUp);
            _bounds.Extend(farPosLeftDown);
            _bounds.Extend(farPosRightUp);
            _bounds.Extend(farPosRightDown);

            _leftPlaneNormal = Vector3.Cross(farPosLeftUp - _cameraPos, farPosLeftDown - _cameraPos);
            _rightPlaneNormal = Vector3.Cross(farPosRightDown - _cameraPos, farPosRightUp - _cameraPos);
            _topPlaneNormal = Vector3.Cross(farPosRightUp - _cameraPos, farPosLeftUp - _cameraPos);
            _bottomPlaneNormal = Vector3.Cross(farPosLeftDown - _cameraPos, farPosRightDown - _cameraPos);
            _leftPlaneNormal.Normalize();
            _rightPlaneNormal.Normalize();
            _topPlaneNormal.Normalize();
            _bottomPlaneNormal.Normalize();
            _leftPlaneDist = Vector3.Dot(_leftPlaneNormal, _cameraPos);
            _rightPlaneDist = Vector3.Dot(_rightPlaneNormal, _cameraPos);
            _topPlaneDist = Vector3.Dot(_topPlaneNormal, _cameraPos);
            _bottomPlaneDist = Vector3.Dot(_bottomPlaneNormal, _cameraPos);
            _nearPlaneDist = -nearDist - Vector3.Dot(_cameraY, _cameraPos);
            _farPlaneDist = farDist + Vector3.Dot(_cameraY, _cameraPos);

            // precompute sin for quick object accept
            float minr = Math.Min(right, top);
            Assert.Fatal(minr > 0.0f, "doh!");
            _acceptCos = _farDist / (float)Math.Sqrt(_farDist * _farDist + minr * minr);
            _acceptInvSin = 1.0f / (float)Math.Sqrt(1.0f - _acceptCos * _acceptCos);

            // precompute sin for object culling
            float maxr = (float)Math.Sqrt(right * right + bottom * bottom); // anything outside this cone rejected
            _rejectCos = _farDist / (float)Math.Sqrt(_farDist * _farDist + maxr * maxr);
            _rejectInvSin = 1.0f / (float)Math.Sqrt(1.0f - _rejectCos * _rejectCos);
        }

#if EXTRA_DIAGNOSTICS

        void DumpStats()
        {
            TorqueConsole.Echo("{0} total frustum checks of which {1} required all tests", _stats.totalChecks, _stats.passAllChecks);
            TorqueConsole.Echo("Near/far plane quick rejects: {0}, outside cone quick reject: {1}, inside cone quick accept: {2}", _stats.sphereFarNearReject, _stats.sphereConeReject, _stats.sphereConeAccept);
            TorqueConsole.Echo("Left plane checks: {0}, outside: {1}", _stats.checkLeft, _stats.outsideLeft);
            TorqueConsole.Echo("Right plane checks: {0}, outside: {1}", _stats.checkRight, _stats.outsideRight);
            TorqueConsole.Echo("Top plane checks: {0}, outside: {1}", _stats.checkTop, _stats.outsideTop);
            TorqueConsole.Echo("Bottom plane checks: {0}, outside: {1}", _stats.checkBottom, _stats.outsideBottom);
            TorqueConsole.Echo("Near plane checks: {0}, outside: {1}", _stats.checkNear, _stats.outsideNear);
            TorqueConsole.Echo("Far plane checks: {0}, outside: {1}", _stats.checkFar, _stats.outsideFar);
        }

#endif

        #endregion


        #region Private, protected, internal fields

        float _nearDist;
        float _farDist;
        float _rejectInvSin;
        float _rejectCos;
        float _acceptInvSin;
        float _acceptCos;
        Vector3 _cameraY;
        Vector3 _cameraPos;

        Vector3 _leftPlaneNormal;
        Vector3 _rightPlaneNormal;
        Vector3 _topPlaneNormal;
        Vector3 _bottomPlaneNormal;
        float _leftPlaneDist;
        float _rightPlaneDist;
        float _topPlaneDist;
        float _bottomPlaneDist;
        float _nearPlaneDist;
        float _farPlaneDist;

        Box3F _bounds;

#if EXTRA_DIAGNOSTICS

        Stats _stats;

#endif

        #endregion

    }
}
