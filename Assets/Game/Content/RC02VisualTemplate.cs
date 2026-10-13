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
        float arcLength;
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
                arcLength = delta.magnitude;
                DrawCarrier(0);
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
            if (shape == RC02VisualShape.Hit)
            {
                // Open, unequal contours echo the architecture. Hits are decorative, never damage bounds.
                DrawArc(line,radius,25,262);
                if(halo!=null) DrawArc(halo,radius*.73f,202,205);
            }
            else
            {
                line.loop=true;line.positionCount=64;
                for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
                SyncHalo(); // The authoritative zone perimeter stays closed and at its real radius.
            }
        }
        static void DrawArc(LineRenderer target,float radius,float start,float sweep)
        {
            target.loop=false;target.positionCount=40;
            for(int i=0;i<40;i++){float a=(start+sweep*i/39f)*Mathf.Deg2Rad;target.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        }
        void DrawCarrier(float progress)
        {
            line.positionCount=25;
            for(int i=0;i<25;i++)
            {
                float t=i/24f,envelope=Mathf.Sin(t*Mathf.PI);
                float wave=(Mathf.Sin(t*22-progress*9)*.12f+Mathf.Sin(t*43+progress*6)*.035f)*envelope;
                line.SetPosition(i,new Vector3(wave,0,arcLength*t));
            }
        }
        void SyncHalo()
        {
            if (line == null || halo == null) return;
            halo.sharedMaterial = line.sharedMaterial; halo.useWorldSpace = line.useWorldSpace;
            halo.widthMultiplier = line.widthMultiplier * .65f;
            if(shape==RC02VisualShape.Hit)return;
            halo.loop = line.loop; halo.positionCount = line.positionCount;
            for (int i = 0; i < line.positionCount; i++) halo.SetPosition(i, line.GetPosition(i));
        }
        public void Animate(float progress)
        {
            if (shape == RC02VisualShape.Hit) SetRadius(preparedRadius * Mathf.Lerp(.22f,1f,1-Mathf.Pow(1-progress,3)));
            if (shape == RC02VisualShape.Arc){DrawCarrier(progress);SyncHalo();}
            Tint((1-progress)*(1-progress));
        }
        public void Tint(float alpha)
        {
            if (renderers == null) return;
            block.Clear(); Color tint = color * intensity; tint.a = color.a * alpha;
            foreach (var r in renderers)
            {
                Color layer=tint;if(r==halo)layer.a*=.38f;
                block.SetColor(BaseColor,layer);r.SetPropertyBlock(block);
            }
        }
    }
}
