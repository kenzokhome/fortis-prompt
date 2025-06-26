using Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utils
{
    public static class Collisions
    {
        public static bool CheckIntersection(float x1, float y1, float z1, float x2, float y2, float z2, Player player)
        {
            if(player.isDead) return false;

            var pos = player._position;
            var radius = 0.5f;

            Vector3 p1 = new Vector3(x1, y1, z1);
            Vector3 p2 = new Vector3(x2, y2, z2);
            Vector3 dir = p2 - p1;
            Vector3 toCenter = pos - p1;

            float t = Math.Clamp(Vector3.Dot(toCenter, dir) / Vector3.Dot(dir, dir), 0, 1);
            Vector3 closestPoint = p1 + dir * t;

            float distSqr = Vector3.DistanceSquared(closestPoint, pos);
            return distSqr <= radius * radius;
        }
    }
}
