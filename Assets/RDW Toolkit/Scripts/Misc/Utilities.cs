using UnityEngine;
using System.Collections;


namespace Redirection
{
    public static class Utilities
    {

        public static Vector3 FlattenedPos3D(Vector3 vec, float height = 0)
        {
            return new Vector3(vec.x, height, vec.z);
        }

        public static Vector2 FlattenedPos2D(Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        public static Vector3 FlattenedDir3D(Vector3 vec)
        {
            return (new Vector3(vec.x, 0, vec.z)).normalized;
        }

        public static Vector2 FlattenedDir2D(Vector3 vec)
        {
            return new Vector2(vec.x, vec.z).normalized;
        }

        public static Vector3 UnFlatten(Vector2 vec, float height = 0)
        {
            return new Vector3(vec.x, height, vec.y);
        }

        /// <summary>
        /// Gets angle from prevDir to currDir in degrees, assuming the vectors lie in the xz plane
        /// (with left handed coordinate system).
        /// </summary>
        /// <param name="currDir"></param>
        /// <param name="prevDir"></param>
        /// <returns></returns>
        public static float GetSignedAngle(Vector3 prevDir, Vector3 currDir)
        {
            return Mathf.Sign(Vector3.Cross(prevDir, currDir).y) * Vector3.Angle(prevDir, currDir);
        }

        public static Vector3 GetRelativePosition(Vector3 pos, Transform origin)
        {
            return Quaternion.Inverse(origin.rotation) * (pos - origin.position);
        }

        public static Vector3 GetRelativeDirection(Vector3 dir, Transform origin)
        {
            return Quaternion.Inverse(origin.rotation) * dir;
        }

        // Based on: http://stackoverflow.com/questions/4780119/2d-euclidean-vector-rotations
        // FORCED LEFT HAND ROTATION AND DEGREES
        public static Vector2 RotateVector(Vector2 fromOrientation, float thetaInDegrees)
        {
            Vector2 ret = Vector2.zero;
            float cos = Mathf.Cos(-thetaInDegrees * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-thetaInDegrees * Mathf.Deg2Rad);
            ret.x = fromOrientation.x * cos - fromOrientation.y * sin;
            ret.y = fromOrientation.x * sin + fromOrientation.y * cos;
            return ret;
        }

        public static bool Approximately(Vector2 v0, Vector2 v1)
        {
            return Mathf.Approximately(v0.x, v1.x) && Mathf.Approximately(v0.y, v1.y);
        }

        /// <summary>
        /// Get the intersection point of a ray and a segment
        /// </summary>
        /// <param name="rayPos"></param> the starting position of the ray
        /// <param name="rayDir"></param> the direction normal of the ray
        /// <param name="segmentStart"></param> starting point of a segment
        /// <param name="segmentEnd"></param> ending point of a segment
        /// <returns>The intersection point, zero if there is no intersection</returns>
        public static Vector2 GetIntersection(Vector2 rayPos, Vector2 rayDir, Vector2 segmentStart, Vector2 segmentEnd)
        {
            Vector2 segmentDir = (segmentEnd - segmentStart).normalized;
            //Debug.Log("Get intersection");
            //Debug.Log(segmentStart);
            //Debug.Log(segmentEnd);
            //Debug.Log(rayPos);
            //Debug.Log(rayDir);

            // They are parallel
            if (Mathf.Approximately(rayDir.y*segmentDir.x, rayDir.x*segmentDir.y))
            {
                //Debug.Log("They are parallel");
                return Vector2.zero;
            }
            else
            {
                float t1 = (segmentDir.x * (rayPos.y - segmentStart.y) + segmentDir.y * (segmentStart.x - rayPos.x))
                    / (rayDir.x * segmentDir.y - rayDir.y * segmentDir.x);
                float t2 = (rayDir.y * (rayPos.x - segmentStart.x) + rayDir.x * (segmentStart.y - rayPos.y))
                    / (rayDir.y * segmentDir.x - rayDir.x * segmentDir.y);
                // the intersection is outside the segment range, or the range of ray
                if (t1<0 || t2 < 0 || t2 > (segmentEnd - segmentStart).magnitude)
                {
                    return Vector2.zero;
                }
                else
                {
                    // or: rayPos + t1 * rayDir;
                    return segmentStart + t2 * segmentDir;
                }
            }

        }
    }
}