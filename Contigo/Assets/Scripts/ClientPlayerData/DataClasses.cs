using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClientPlayerData
{
    [Serializable]
    public class InputMessage
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    [Serializable]
    public class PositionData
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    [Serializable]
    public class Vector3Data
    {
        public float X;
        public float Y;
        public float Z;
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    [Serializable]
    public class CollisionData
    {
        public string OtherPlayerId;
        public float DirectionX;
        public float DirectionZ;
        public Vector3 ToDirection() => new Vector3(DirectionX, 0, DirectionZ).normalized;
    }

    [Serializable]
    public struct SnapshotEntry
    {
        public long Timestamp;
        public Vector3 Position;
        public Vector3 Velocity;
    }

    [Serializable]
    public class Snapshot
    {
        public long Timestamp;
        public Dictionary<string, PositionData> Positions;
        public Dictionary<string, Vector3Data> Velocities;
        public Dictionary<string, CollisionData> Collisions;
    }
}