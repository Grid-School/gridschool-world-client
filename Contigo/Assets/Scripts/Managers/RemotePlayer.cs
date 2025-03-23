using UnityEngine;

namespace Managers
{
    public class RemotePlayer
    {
        public GameObject GameObject { get; private set; }

        public RemotePlayer(GameObject gameObject)
        {
            GameObject = gameObject;
        }
    }
}
