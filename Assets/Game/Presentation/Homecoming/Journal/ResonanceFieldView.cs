using UnityEngine;
using UnityEngine.Rendering;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation.Homecoming.Journal
{
    // Only exposes rule positions; open arcs are landmarks, never independent damage zones.
    public sealed class ResonanceFieldView : MonoBehaviour
    {
        public Material markerMaterial;
        GameLoop loop;
        Material material;
        readonly LineRenderer[] arcs = new LineRenderer[3];
        MaterialPropertyBlock tint;
        void Awake()
        {
            tint = new MaterialPropertyBlock();
            loop = GetComponent<GameLoop>();
            if (markerMaterial == null) { enabled = false; Debug.LogError("Resonance marker material missing"); return; }
            material = new Material(markerMaterial);
            material.SetColor("_BaseColor", Color.white);
            for (int i = 0; i < arcs.Length; i++)
            {
                var line = new GameObject("Homecoming resonance mark " + i).AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false); line.sharedMaterial = material;
                line.useWorldSpace = true; line.positionCount = 36; line.widthMultiplier = .065f;
                line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; arcs[i] = line;
            }
        }
        void LateUpdate()
        {
            var sim = loop?.Sim; if (sim == null) return;
            for (int i = 0; i < arcs.Length; i++) arcs[i].gameObject.SetActive(false);
            if (sim.State.Phase.IsTerminal()) return;
            if (sim.ReturnNeedleActive) Draw(0, sim.ReturnNeedle.X, sim.ReturnNeedle.Y, 1.1f, new Color(.95f,.78f,.45f), sim.State.Now);
            if (sim.Combat.HomecomingLureActive) Draw(1, sim.Combat.HomecomingLurePosition.X, sim.Combat.HomecomingLurePosition.Y, 1.5f, new Color(.5f,.85f,.9f), sim.State.Now);
            if (sim.CampSignalUntil > sim.State.Now) Draw(2, sim.LastCampPosition.X, sim.LastCampPosition.Y, 1 + (2 - sim.CampSignalUntil + sim.State.Now), new Color(.95f,.72f,.4f), sim.State.Now);
        }
        void Draw(int index, float x, float z, float radius, Color color, float time)
        {
            var line = arcs[index]; line.gameObject.SetActive(true); line.startColor = line.endColor = color;
            tint.Clear(); tint.SetColor("_BaseColor", color); line.SetPropertyBlock(tint);
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = time * .35f + i / (line.positionCount - 1f) * Mathf.PI * 1.55f;
                line.SetPosition(i, new Vector3(x + Mathf.Cos(angle) * radius, .13f, z + Mathf.Sin(angle) * radius));
            }
        }
        void OnDestroy()
        { foreach (var line in arcs) if (line != null) Destroy(line.gameObject); if (material != null) Destroy(material); }
        void OnDisable() { foreach (var line in arcs) if (line != null) line.gameObject.SetActive(false); }
    }
}
