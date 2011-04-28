//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Methods used for generic collision determination.
    /// </summary>
    public class Collision
    {
        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 229.
        public static bool IntersectMovingSphereAABB(Vector3 startCenter, Vector3 endCenter, float radius, Vector3 minExtent, Vector3 maxExtent, out float t)
        {
            // Expand AABB by sphere radius.
            Vector3 rad = new Vector3(radius, radius, radius);
            Vector3 minExpanded = minExtent - rad;
            Vector3 maxExpanded = maxExtent + rad;

            // Intersect ray against expanded AABB -- if no intersection, no intersection of moving sphere with original AABB.
            Vector3 p;
            if (!IntersectRayAABB(startCenter, endCenter - startCenter, minExpanded, maxExpanded, out t, out p) || t > 1.0f)
                return false;

            // Compute which min/max faces of AABB the intersection of p lies outside of.
            int u = 0;
            int v = 0;
            if (p.X < minExtent.X)
                u |= 1;
            if (p.X > maxExtent.X)
                v |= 1;
            if (p.Y < minExtent.Y)
                u |= 2;
            if (p.Y > maxExtent.Y)
                v |= 2;
            if (p.Z < minExtent.Z)
                u |= 4;
            if (p.Z > maxExtent.Z)
                v |= 4;

            Assert.Fatal((u + v) == (u | v), "Doh");
            int m = u + v;
            Vector3 e0, e1;

            // cafTODO: routine should return normal instead of
            // modifying return value here
            // quick and dirty check for direction of movement
            Vector3 normal = new Vector3();
            if ((m & 1) != 0)
                normal.X = (u & 1) != 0 ? -1.0f : 1.0f;
            if ((m & 2) != 0)
                normal.Y = (u & 2) != 0 ? -1.0f : 1.0f;
            if ((m & 4) != 0)
                normal.Z = (u & 4) != 0 ? -1.0f : 1.0f;
            if (Vector3.Dot(normal, endCenter - startCenter) > -0.00001f)
                return false;

            float tCap;

            // bits all set means we are in a vertex region
            if (m == 7)
            {
                // intersect sphere center path with the vertex capsules
                float tmin = 10E10f;

                e0 = _Corner(minExtent, maxExtent, v);
                e1 = _Corner(minExtent, maxExtent, v ^ 1);
                if (IntersectSegmentCapsule(startCenter, endCenter, e0, e1, radius, out t, out tCap))
                    tmin = Math.Min(t, tmin);

                e0 = _Corner(minExtent, maxExtent, v);
                e1 = _Corner(minExtent, maxExtent, v ^ 2);
                if (IntersectSegmentCapsule(startCenter, endCenter, e0, e1, radius, out t, out tCap))
                    tmin = Math.Min(t, tmin);

                e0 = _Corner(minExtent, maxExtent, v);
                e1 = _Corner(minExtent, maxExtent, v ^ 4);
                if (IntersectSegmentCapsule(startCenter, endCenter, e0, e1, radius, out t, out tCap))
                    tmin = Math.Min(t, tmin);
                if (tmin > 1.0f)
                    return false;
                t = tmin;
                return true;
            }
            // if only one bit set in m then p is in a face region
            if ((m & (m - 1)) == 0)
            {
                // t still set from ray/AABB test
                return true;
            }
            // p is in an edge region.  Intersect against the capsule at the edge
            e0 = _Corner(minExtent, maxExtent, u ^ 7);
            e1 = _Corner(minExtent, maxExtent, v);
            return IntersectSegmentCapsule(startCenter, endCenter, e0, e1, radius, out t, out tCap);
        }

        // Based on description from the book "Real Time Collision Detection" by Christer Ericson, pp. 227.
        public static bool IntersectMovingSphereTriangle(Vector3 startCenter, Vector3 endCenter, float radius, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal, out float t, out Vector3 closest)
        {
            t = 10.0f;
            closest = new Vector3();

            float startDot = Vector3.Dot(startCenter, normal);
            float endDot = Vector3.Dot(endCenter, normal);
            float triDot = Vector3.Dot(p0, normal);
            if ((startDot - triDot) * (endDot - triDot) > 0.0f && Math.Abs(startDot - triDot) > radius && Math.Abs(endDot - triDot) > radius)
                // sphere starts and ends completely on one side of the plane
                return false;
            if (endDot - startDot > -0.001f)
                // sphere is moving away from plane (or moving very slowly into it)
                return false;

            if (startDot < triDot + radius)
            {
                // start on other side of plane
                closest = startCenter;
                t = 0.0f;
            }
            else
            {
                // sphere starts on + side and intersects plane
                t = (triDot + radius - startDot) / (endDot - startDot);
                closest = startCenter + t * (endCenter - startCenter);
            }
            closest += (triDot - Vector3.Dot(closest, normal)) * normal;
            if (PointInTriangle(closest, p0, p1, p2))
                return true;

            float tTest;
            float tCap;
            t = 10.0f;
            if (IntersectSegmentCapsule(startCenter, endCenter, p0, p1, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p0 + tCap * (p1 - p0);
                    t = tTest;
                }
            }
            if (IntersectSegmentCapsule(startCenter, endCenter, p1, p2, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p1 + tCap * (p2 - p1);
                    t = tTest;
                }
            }
            if (IntersectSegmentCapsule(startCenter, endCenter, p2, p0, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p2 + tCap * (p0 - p2);
                    t = tTest;
                }
            }
            return t <= 1.0f;
        }

        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 179.
        public static bool IntersectRayAABB(Vector3 rayStart, Vector3 rayDir, Vector3 minExtent, Vector3 maxExtent, out float t, out Vector3 p)
        {
            t = 0.0f;
            p = new Vector3();
            float tmax = 10E10f;

            if (!_IntersectSlab1D(rayStart.X, rayDir.X, minExtent.X, maxExtent.X, ref t, ref tmax))
                return false;
            if (!_IntersectSlab1D(rayStart.Y, rayDir.Y, minExtent.Y, maxExtent.Y, ref t, ref tmax))
                return false;
            if (!_IntersectSlab1D(rayStart.Z, rayDir.Z, minExtent.Z, maxExtent.Z, ref t, ref tmax))
                return false;

            // Ray intersects all 3 slabs, return time & point of intersection
            p = rayStart + t * rayDir;
            return true;
        }

        public static bool IntersectSegmentCapsule(Vector3 segStart, Vector3 segEnd, Vector3 capStart, Vector3 capEnd, float radius, out float tSeg, out float tCap)
        {
            Vector3 p1, p2;
            return ClosestPtSegmentSegment(segStart, segEnd, capStart, capEnd, out tSeg, out tCap, out p1, out p2) < radius * radius;
        }

        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 149.  Returns squared distance.
        public static float ClosestPtSegmentSegment(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,
                                                   out float s, out float t, out Vector3 c1, out Vector3 c2)
        {
            float Epsilon = 0.0001f;

            Vector3 d1 = q1 - p1; // Direction vector of segment S1
            Vector3 d2 = q2 - p2; // Direction vector of segment S2
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);

            // Check if either or both segments degenerate into points
            if (a <= Epsilon && e <= Epsilon)
            {
                // Both segments degenerate into points
                s = t = 0.0f;
                c1 = p1;
                c2 = p2;
                return Vector3.Dot(c1 - c2, c1 - c2);
            }
            if (a <= Epsilon)
            {
                // First segment degenerates into a point
                s = 0.0f;
                t = f / e; // s = 0 => t = (b*s + f) / e = f / e
                t = MathHelper.Clamp(t, 0.0f, 1.0f);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= Epsilon)
                {
                    // Second segment degenerates into a point
                    t = 0.0f;
                    s = MathHelper.Clamp(-c / a, 0.0f, 1.0f); // t = 0 => s = (b*t - c) / a = -c / a
                }
                else
                {
                    // The general nondegenerate case starts here
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b; // Always nonnegative

                    // If segments not parallel, compute closest point on L1 to L2, and
                    // clamp to segment S1. Else pick arbitrary s (here 0)
                    if (denom != 0.0f)
                    {
                        s = MathHelper.Clamp((b * f - c * e) / denom, 0.0f, 1.0f);
                    }
                    else
                        s = 0.0f;

                    // Compute point on L2 closest to S1(s) using
                    // t = Dot((P1+D1*s)-P2,D2) / Dot(D2,D2) = (b*s + f) / e
                    t = (b * s + f) / e;

                    // If t in [0,1] done. Else clamp t, recompute s for the new value
                    // of t using s = Dot((P2+D2*t)-P1,D1) / Dot(D1,D1)= (t*b - c) / a
                    // and clamp s to [0, 1]
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = MathHelper.Clamp(-c / a, 0.0f, 1.0f);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = MathHelper.Clamp((b - c) / a, 0.0f, 1.0f);
                    }
                }
            }

            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
            return Vector3.Dot(c1 - c2, c1 - c2);
        }

        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 130.
        public static Vector3 ClosestPtPointAABB(Vector3 p, Vector3 minExtent, Vector3 maxExtent, out Vector3 normal)
        {
            normal = new Vector3();

            if (p.X < minExtent.X)
            {
                p.X = minExtent.X;
                normal.X = -1.0f;
            }
            else if (p.X > maxExtent.X)
            {
                p.X = maxExtent.X;
                normal.X = 1.0f;
            }
            else
                normal.X = 0.0f;

            if (p.Y < minExtent.Y)
            {
                p.Y = minExtent.Y;
                normal.Y = -1.0f;
            }
            else if (p.Y > maxExtent.Y)
            {
                p.Y = maxExtent.Y;
                normal.Y = 1.0f;
            }
            else
                normal.Y = 0.0f;

            if (p.Z < minExtent.Z)
            {
                p.Z = minExtent.Z;
                normal.Z = -1.0f;
            }
            else if (p.Z > maxExtent.Z)
            {
                p.Z = maxExtent.Z;
                normal.Z = 1.0f;
            }
            else
                normal.Z = 0.0f;
            normal.Normalize();
            return p;
        }

        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 130.
        public static Vector3 ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
                return a;

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
                return b;

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
                return c;

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            float denom = 1.0f / (va + vb + vc);
            float vv = vb * denom;
            float ww = vc * denom;
            return a + ab * vv + ac * ww;
        }

        // Second implementation of this routine, this one based on moving sphere code -- use this one if you want it to agree with moving sphere.
        public static bool ClosestPtPointTriangle(Vector3 pt, float radius, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 normal, out Vector3 closest)
        {
            closest = new Vector3();

            float ptDot = Vector3.Dot(pt, normal);
            float triDot = Vector3.Dot(p0, normal);
            if (Math.Abs(ptDot - triDot) > radius * 1.1f)
                return false;

            closest = pt + (triDot - ptDot) * normal;
            if (PointInTriangle(closest, p0, p1, p2))
            {
                return true;
            }

            float tTest;
            float tCap;
            float t = 10.0f;
            if (IntersectSegmentCapsule(pt, pt, p0, p1, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p0 + tCap * (p1 - p0);
                    t = tTest;
                }
            }
            if (IntersectSegmentCapsule(pt, pt, p1, p2, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p1 + tCap * (p2 - p1);
                    t = tTest;
                }
            }
            if (IntersectSegmentCapsule(pt, pt, p2, p0, radius, out tTest, out tCap))
            {
                if (tTest < t)
                {
                    closest = p2 + tCap * (p0 - p2);
                    t = tTest;
                }
            }
            return t < 1.0f;
        }



        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 204.
        public static bool PointInTriangle(Vector3 pnt, Vector3 a, Vector3 b, Vector3 c)
        {
            a -= pnt;
            b -= pnt;
            c -= pnt;
            Vector3 u = Vector3.Cross(b, c);
            Vector3 v = Vector3.Cross(c, a);
            if (Vector3.Dot(u, v) < 0.0f)
                return false;
            Vector3 w = Vector3.Cross(a, b);
            return !(Vector3.Dot(u, w) < 0.0f);
        }

        // From the book "Real Time Collision Detection" by Christer Ericson, pp. 164.
        public static bool TestAABBPlane(Box3F b, Vector3 planeNormal, float planeDist, out float boxDistance)
        {
            // These two lines not necessary with a (center, extents) AABB representation
            Vector3 c = (b.Max + b.Min) * 0.5f; // Compute AABB center
            Vector3 e = b.Max - c; // Compute positive extents

            // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
            float r = e.X * Math.Abs(planeNormal.X) + e.Y * Math.Abs(planeNormal.Y) + e.Z * Math.Abs(planeNormal.Z);
            // Compute distance of box center from plane
            boxDistance = Vector3.Dot(planeNormal, c) - planeDist;
            // Intersection occurs when distance s falls within [-r,+r] interval
            if (Math.Abs(boxDistance) <= r)
            {
                boxDistance = 0.0f;
                return true;
            }
            if (boxDistance < 0.0f)
                boxDistance += r;
            else
                boxDistance -= r;
            return false;
        }

        // Test OBB against plane.  Return 0.0f if collide, + dist if outside plane, - dist if inside plane
        public static bool TestOBBPlane(Box3F b, Matrix mat, Vector3 planeNormal, float planeDist, out float boxDistance)
        {
            // Compute OBB center and extents
            Vector3 c0 = new Vector3(0.5f * (b.Max.X + b.Min.X), 0.5f * (b.Max.Y + b.Min.Y), 0.5f * (b.Max.Z + b.Min.Z));
            Vector3 c;
            Vector3.Transform(ref c0, ref mat, out c);
            Vector3 e = new Vector3(0.5f * (b.Max.X - b.Min.X), 0.5f * (b.Max.Y - b.Min.Y), 0.5f * (b.Max.Z - b.Min.Z));

            // Project box extents onto planeNormal
            Vector3 x, y, z;
            MatrixUtil.GetX(ref mat, out x);
            MatrixUtil.GetY(ref mat, out y);
            MatrixUtil.GetZ(ref mat, out z);
            float xdot = planeNormal.X * x.X + planeNormal.Y * x.Y + planeNormal.Z * x.Z;
            float ydot = planeNormal.X * y.X + planeNormal.Y * y.Y + planeNormal.Z * y.Z;
            float zdot = planeNormal.X * z.X + planeNormal.Y * z.Y + planeNormal.Z * z.Z;
            float cdot = planeNormal.X * c.X + planeNormal.Y * c.Y + planeNormal.Z * c.Z;
            float rad = e.X * Math.Abs(xdot) + e.Y * Math.Abs(ydot) + e.Z * Math.Abs(zdot);
            boxDistance = cdot - planeDist;
            if (Math.Abs(boxDistance) <= rad)
            {
                boxDistance = 0.0f;
                return true;
            }
            if (boxDistance < 0.0f)
                boxDistance += rad;
            else
                boxDistance -= rad;
            return false;
        }

        // Helper method for IntersectRayAABB.
        static bool _IntersectSlab1D(float rayStart, float rayDir, float minExtent, float maxExtent, ref float tmin, ref float tmax)
        {
            float Epsilon = 0.0001f;

            if (Math.Abs(rayDir) < Epsilon)
            {
                // parallel to slab
                if (rayStart < minExtent || rayStart > maxExtent)
                    return false;
            }
            else
            {
                // Compute intersection t value of aray with near and far plane of slab
                float ood = 1.0f / rayDir;
                float t1 = (minExtent - rayStart) * ood;
                float t2 = (maxExtent - rayStart) * ood;
                // Make t1 be intersection with near plane, t2 with far plane
                if (t1 > t2)
                {
                    float t12 = t1;
                    t1 = t2;
                    t2 = t12;
                }
                // Intersect time of intersection
                if (t1 > tmin)
                    tmin = t1;
                if (t2 < tmax)
                    tmax = t2;
                if (tmin > tmax)
                    // empty interval
                    return false;
            }
            return true;
        }

        // Helper method for IntersectMovingSphereAABB.
        static Vector3 _Corner(Vector3 minExtent, Vector3 maxExtent, int n)
        {
            Vector3 ret = new Vector3();
            ret.X = (n & 1) != 0 ? maxExtent.X : minExtent.X;
            ret.Y = (n & 2) != 0 ? maxExtent.Y : minExtent.Y;
            ret.Z = (n & 4) != 0 ? maxExtent.Z : minExtent.Z;
            return ret;
        }
    }
}