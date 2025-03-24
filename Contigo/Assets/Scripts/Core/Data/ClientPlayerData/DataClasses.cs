using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.ClientPlayerData
{
    [Serializable]
    public class InputMessage
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Angle { get; set; }
        public float Speed { get; set; }
        public float MotionSpeed { get; set; } // Added for animation synchronization
        public bool Jump { get; set; }
        public bool Grounded { get; set; }
        public bool FreeFall { get; set; }
    }

    [Serializable]
    public class PositionData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Angle { get; set; } // Added to store the player's yaw angle
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    [Serializable]
    public class RotationData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public Quaternion ToQuaternion() => new Quaternion(X, Y, Z, W);
    }

    [Serializable]
    public class InkaAnimationState
    {
        public float Speed { get; set; }
        public float MotionSpeed { get; set; } // Added for animation synchronization
        public bool Jump { get; set; }
        public bool Grounded { get; set; }
        public bool FreeFall { get; set; }
    }

    [Serializable]
    public class Vector3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    [Serializable]
    public class CollisionData
    {
        public string OtherPlayerId { get; set; }
        public float DirectionX { get; set; }
        public float DirectionZ { get; set; }
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
        public long Timestamp { get; set; }
        public Dictionary<string, PositionData> Positions { get; set; }
        public Dictionary<string, RotationData> Rotations { get; set; }
        public Dictionary<string, Vector3Data> Velocities { get; set; }
        public Dictionary<string, CollisionData> Collisions { get; set; }
        public Dictionary<string, InkaAnimationState> Animations { get; set; }
    }
}