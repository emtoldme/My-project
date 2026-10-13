using UnityEngine;
using Waffle;
using Waffle.GameplayNext;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    // Reads the active objective; the marker never controls channel progress or rewards.
    public sealed class TuningMarkerView : MonoBehaviour
    {
        public GameLoop loop;
        public Material ringMaterial;
        public GameObject resonatorPrefab;
        LineRenderer ring;
        GameObject marker;
        readonly Vector3[] points = new Vector3[49];

        void Start()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            ring = new GameObject("TuningRange").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.useWorldSpace = true; ring.loop = true; ring.positionCount = points.Length;
            ring.widthMultiplier = .13f; ring.sharedMaterial = ringMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            if (resonatorPrefab != null)
            {
                marker = Instantiate(resonatorPrefab, transform);
                marker.name = "ActiveTuningResonator";
                foreach (var collider in marker.GetComponentsInChildren<Collider>()) Destroy(collider);
                marker.transform.localScale = Vector3.one * 1.1f;
            }
            Show(false);
        }

        void LateUpdate()
        {
            if (ring == null) return;
            var view = loop?.ReadGameplayView();
            var objective = view != null ? view.Objective : default;
            bool visible = view != null && !view.Phase.IsTerminal()
                && (objective.Status == ObjectiveStatus.Available || objective.Status == ObjectiveStatus.Channeling);
            Show(visible); if (!visible) return;
            Vector3 center = Ground(new Vector3(objective.Position.X, 0f, objective.Position.Y));
            Color color = Color.Lerp(new Color(1f, .72f, .25f), new Color(.4f, 1f, .84f), objective.Progress);
            ring.startColor = ring.endColor = color;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / (points.Length - 1);
                points[i] = Ground(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * objective.Radius);
            }
            ring.SetPositions(points);
            if (marker != null)
            {
                marker.transform.position = center + Vector3.up * .75f;
                marker.transform.rotation = Quaternion.Euler(0f, view.Now * 35f, 0f);
            }
        }
        static Vector3 Ground(Vector3 p)
        {
            var terrain = Terrain.activeTerrain;
            p.y = (terrain != null ? terrain.SampleHeight(p) + terrain.transform.position.y : 0f) + .09f;
            return p;
        }
        void Show(bool value) { if (ring != null) ring.gameObject.SetActive(value); if (marker != null) marker.SetActive(value); }
    }
}
