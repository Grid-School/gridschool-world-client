using System.Collections.Generic;

namespace Core.Networking
{
    [System.Serializable]
    public struct PlayerPosition
    {
        public float X;
        public float Y;
        public float Z;
        public float Angle;
    }

    [System.Serializable]
    public struct PlayerRotation
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
    }

    [System.Serializable]
    public struct PlayerVelocity
    {
        public float X;
        public float Y;
        public float Z;
    }

    [System.Serializable]
    public struct PlayerCollision
    {
        public string ColliderId;
    }

    [System.Serializable]
    public struct PlayerAnimation
    {
        public float Speed;
        public float MotionSpeed;
        public bool Jump;
        public bool Grounded;
        public bool FreeFall;
    }

    [System.Serializable]
    public struct JsonPosition
    {
        public float X;
        public float Y;
        public float Z;
        public float Angle;
    }

    [System.Serializable]
    public struct JsonRotation
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
    }

    [System.Serializable]
    public struct JsonVelocity
    {
        public float X;
        public float Y;
        public float Z;
    }

    [System.Serializable]
    public struct JsonCollision
    {
        public string ColliderId;
    }

    [System.Serializable]
    public struct JsonAnimation
    {
        public float Speed;
        public float MotionSpeed;
        public bool Jump;
        public bool Grounded;
        public bool FreeFall;
    }

    [System.Serializable]
    public class PositionEntry
    {
        public string PlayerId;
        public JsonPosition Position;
    }

    [System.Serializable]
    public class RotationEntry
    {
        public string PlayerId;
        public JsonRotation Rotation;
    }

    [System.Serializable]
    public class VelocityEntry
    {
        public string PlayerId;
        public JsonVelocity Velocity;
    }

    [System.Serializable]
    public class CollisionEntry
    {
        public string PlayerId;
        public JsonCollision Collision;
    }

    [System.Serializable]
    public class AnimationEntry
    {
        public string PlayerId;
        public JsonAnimation Animation;
    }

    [System.Serializable]
    public class PlayerDataWrapperSerialized
    {
        public string type;
        public long Timestamp;
        public string clientId;
        public string playerId;
        public List<PositionEntry> Positions;
        public List<RotationEntry> Rotations;
        public List<VelocityEntry> Velocities;
        public List<CollisionEntry> Collisions;
        public List<AnimationEntry> Animations;
    }
}