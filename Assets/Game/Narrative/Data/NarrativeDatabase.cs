using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Narrative
{
    [CreateAssetMenu(fileName = "NarrativeDatabase", menuName = "Reverb/Narrative Database")]
    public class NarrativeDatabase : ScriptableObject
    {
        public List<NarrativeCueDefinition> cues = new List<NarrativeCueDefinition>();
        public List<CassetteArtifact> artifacts = new List<CassetteArtifact>();

        public NarrativeCueDefinition FindCue(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return null;
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].cueId == cueId) return cues[i];
            }
            return null;
        }

        public CassetteArtifact FindArtifact(string tapeId)
        {
            if (string.IsNullOrEmpty(tapeId)) return null;
            for (int i = 0; i < artifacts.Count; i++)
            {
                if (artifacts[i].tapeId == tapeId) return artifacts[i];
            }
            return null;
        }
    }
}
