using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    public sealed class WindCloth : MonoBehaviour
    {
        public GameLoop loop;
        public float stiffness = .7f;
        Mesh mesh;
        Vector3[] rest, vertices;
        Vector2[] uv;
        void Start()
        {
            if (loop == null) loop = FindAnyObjectByType<GameLoop>();
            var filter = GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) { enabled = false; return; }
            mesh = Instantiate(filter.sharedMesh); mesh.MarkDynamic(); filter.sharedMesh = mesh;
            rest = mesh.vertices; vertices = new Vector3[rest.Length]; uv = mesh.uv;
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 2f);
        }
        void LateUpdate()
        {
            if (mesh == null || loop?.Sim == null || loop.Sim.State.Phase != RunPhase.Playing) return;
            float time = loop.Sim.State.Now;
            Vector4 wind = Shader.GetGlobalVector("_ReverbWind");
            float gust = (.3f + wind.z) * stiffness;
            for (int i = 0; i < rest.Length; i++)
            {
                float weight = uv[i].x * uv[i].x;
                float phase = time * 2.1f - uv[i].x * 4.8f + transform.position.x;
                vertices[i] = rest[i] + new Vector3(-weight * .12f, Mathf.Sin(phase * .65f) * weight * .07f,
                    (Mathf.Sin(phase) * .3f + Mathf.Sin(phase * 2.7f + uv[i].y * 6) * .07f) * weight * gust);
            }
            mesh.vertices = vertices; mesh.RecalculateNormals();
        }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
