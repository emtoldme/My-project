using System;
using UnityEngine;

namespace Reverb.Drafts.RC01.Pipeline
{
    [Serializable]
    public class RC02EncounterBand
    {
        public string id;
        public float minuteStart;
        public float minuteEnd;
        public int aliveCap;
        public GameObject enemyVisual;
        public string note;
    }

    public class RC02EncounterSheet : ScriptableObject
    {
        public string mapId = "HarvestRoad";
        public string status = "planned";
        public RC02EncounterBand[] bands = Array.Empty<RC02EncounterBand>();
    }
}
