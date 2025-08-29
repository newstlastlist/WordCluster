using System;
using UnityEngine;

namespace UI.Game
{
    [Serializable]
    public class ClusterPrefabRule
    {
        [Min(1)]
        public int ClusterTextLength;
        public GameObject ClusterPrefab;
    }
}