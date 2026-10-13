using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Pipeline
{
    public enum RC02VisualShape { Projectile, Arc, Ring, Hit }
    // Passive template; the pooled owner advances it using the rule clock.
    public sealed class RC02VisualTemplate : MonoBehaviour
    {
        public RC02VisualShape shape;
        public Color color = Color.white;
        public LineRenderer line;
        public LineRenderer halo;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        float intensity;
        float preparedRadius;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public void Prepare(RC02CatalogEntry entry, PresentationEvent item)
        {
            if (renderers == null) { renderers = GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock(); }
            transform.SetPositionAndRotation(new Vector3(item.X, .12f, item.Z), Quaternion.identity);
            transform.localScale = Vector3.one;
            intensity = entry.Parameter("intensity", 1);
            var material = entry.Asset<Material>("material");
            foreach (var r in renderers) if (material != null) r.sharedMaterial = material;
            if (shape == RC02VisualShape.Arc)
            {
                Vector3 from = new Vector3(item.FromX, .65f, item.FromZ), to = new Vector3(item.X, .65f, item.Z);
                Vector3 delta = to - from; transform.position = from;
                if (delta.sqrMagnitude > .00001f) transform.rotation = Quaternion.LookRotation(delta);
                line.positionCount = 7;
                for (int i = 0; i < 7; i++) line.SetPosition(i, new Vector3(i == 0 || i == 6 ? 0 : (i % 2 == 0 ? -.09f : .09f), 0, delta.magnitude * i / 6));
            }
            else if (shape == RC02VisualShape.Projectile)
            {
                Vector3 direction = new Vector3(item.DirectionX, 0, item.DirectionZ);
                if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(direction);
                transform.position += Vector3.up * .55f;
                transform.localScale = Vector3.one * entry.Parameter("scale", 1);
            }
            else
            {
                float radius = item.Radius > 0 ? item.Radius : entry.Parameter("scale", .5f);
                if (shape == RC02VisualShape.Hit) transform.position += Vector3.up * .3f;
                SetRadius(radius);
                preparedRadius = radius;
            }
            if (line != null) line.widthMultiplier = Mathf.Clamp(entry.Parameter("width", .065f), .025f, .2f);
            SyncHalo();
            Tint(1);
        }
        public void SetRadius(float radius)
        {
            if (line == null || (shape != RC02VisualShape.Ring && shape != RC02VisualShape.Hit)) return;
            line.positionCount = 48;
            for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48; line.SetPosition(i, new Vector3(Mathf.Cos(a)*radius, 0, Mathf.Sin(a)*radius)); }
            SyncHalo();
        }
        void SyncHalo()
        {
            if (line == null || halo == null) return;
            halo.sharedMaterial = line.sharedMaterial; halo.useWorldSpace = line.useWorldSpace;
            halo.loop = line.loop; halo.positionCount = line.positionCount; halo.widthMultiplier = line.widthMultiplier * 3.5f;
            for (int i = 0; i < line.positionCount; i++) halo.SetPosition(i, line.GetPosition(i));
        }
        public void Animate(float progress)
        {
            if (shape == RC02VisualShape.Hit) SetRadius(preparedRadius * Mathf.Lerp(.35f,1f,Mathf.SmoothStep(0,1,progress)));
            Tint(1-progress);
        }
        public void Tint(float alpha)
        {
            if (renderers == null) return;
            block.Clear(); Color tint = color * intensity; tint.a = color.a * alpha;
            block.SetColor(BaseColor, tint);
            foreach (var r in renderers) r.SetPropertyBlock(block);
        }
    }
}
