using System.Collections.Generic;
using UnityEngine;
using Waffle;
using Waffle.Combat;
using Waffle.Experiments.Mechanism.Presentation;

namespace Reverb.Drafts.RC01.Presentation
{
    // TEST_ONLY shapes read the authoritative weapon/zone/pickup snapshots. They never deal damage.
    public sealed class RC01BattleView : MonoBehaviour
    {
        public GameLoop loop;
        public M01DerivedFx derivedFx;
        public Material lineMaterial;
        readonly List<M01EchoCue> cues = new List<M01EchoCue>(32);
        readonly List<Transform> orbs = new List<Transform>();
        readonly List<Transform> pickupViews = new List<Transform>();
        readonly List<LineRenderer> zoneViews = new List<LineRenderer>();
        Material orbMaterial, pickupMaterial;
        MaterialPropertyBlock block;
        public int EchoEvents { get; private set; }
        public int PropagationEvents { get; private set; }
        public int EvolutionEvents { get; private set; }
        void Awake()
        {
            loop = GetComponent<GameLoop>(); derivedFx = GetComponent<M01DerivedFx>();
            block = new MaterialPropertyBlock();
            orbMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            orbMaterial.SetColor("_BaseColor", new Color(.35f,.9f,.72f)); orbMaterial.SetFloat("_Smoothness", .22f);
            pickupMaterial = new Material(orbMaterial);
            for (int i = 0; i < 16; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(orb.GetComponent<Collider>()); orb.name = "TEST_ONLY_Orbit";
                orb.GetComponent<Renderer>().sharedMaterial = orbMaterial;
                orb.transform.SetParent(transform); orb.transform.localScale = Vector3.one * .5f;
                orb.SetActive(false); orbs.Add(orb.transform);
                var line = new GameObject("TEST_ONLY_Zone").AddComponent<LineRenderer>();
                line.transform.SetParent(transform); line.sharedMaterial = lineMaterial;
                line.loop = true; line.positionCount = 48; line.widthMultiplier = .065f;
                line.gameObject.SetActive(false); zoneViews.Add(line);
            }
            for (int i = 0; i < 14; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(p.GetComponent<Collider>()); p.GetComponent<Renderer>().sharedMaterial = pickupMaterial;
                p.name = "TEST_ONLY_Pickup"; p.transform.SetParent(transform); p.SetActive(false); pickupViews.Add(p.transform);
            }
        }
        void OnEnable() { if (loop != null) loop.PresentationDispatched += OnEvents; }
        void OnDisable() { if (loop != null) loop.PresentationDispatched -= OnEvents; }
        void OnDestroy() { Destroy(orbMaterial); Destroy(pickupMaterial); }
        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            foreach (var e in events)
            {
                if (e.Type == PresentationEventType.EchoHit)
                { EchoEvents++; derivedFx.PlayEcho(e.X, e.Z, e.Value); }
                else if (e.Type == PresentationEventType.PropagationHit || e.Type == PresentationEventType.EchoPropagationHit)
                { PropagationEvents++; derivedFx.PlayPropagation(e.FromX, e.FromZ, e.X, e.Z, e.Value); }
                else if (e.Type == PresentationEventType.Evolved) EvolutionEvents++;
                else if (e.Type == PresentationEventType.Victory || e.Type == PresentationEventType.Defeat || e.Type == PresentationEventType.Restart) derivedFx.Clear();
            }
        }
        void LateUpdate()
        {
            if (loop?.Sim?.Combat == null) return;
            var sim = loop.Sim; var combat = sim.Combat;
            cues.Clear();
            if (!sim.State.Phase.IsTerminal())
                foreach (var c in combat.EchoCues)
                {
                    if (!combat.TryGetEnemy(c.Serial,c.Generation,out var e)) continue;
                    cues.Add(new M01EchoCue(c.Serial,c.Generation,e.Position.X,e.Position.Y,
                        Mathf.InverseLerp(c.ScheduledAt,c.DueAt,sim.State.Now)));
                }
            derivedFx.SyncEchoCues(cues);
            int n = 0;
            if (!sim.State.Phase.IsTerminal())
                foreach (var w in combat.RunWeapons)
                {
                    if (w.Id != "W03" && w.Id != "E03") continue;
                    for (int i = 0; i < (int)w.RankData.Get("count",2) && n < orbs.Count; i++)
                    {
                        Vec2 p = combat.OrbitPosition(w,i,sim.State.Now,new Vec2(sim.PlayerX,sim.PlayerZ));
                        var t = orbs[n++]; t.gameObject.SetActive(true); t.position = new Vector3(p.X,.65f,p.Y);
                    }
                }
            for (;n < orbs.Count;n++) orbs[n].gameObject.SetActive(false);
            n = 0;
            foreach (var z in combat.Zones)
            {
                if (n >= zoneViews.Count) break;
                var line = zoneViews[n++]; line.gameObject.SetActive(true);
                Color color = sim.State.Now < z.StartsAt ? new Color(.4f,.65f,.75f,.45f) : new Color(.45f,.9f,1,.9f);
                line.startColor = line.endColor = color;
                for (int i = 0;i < 48;i++)
                { float a = i * Mathf.PI * 2 / 48; line.SetPosition(i,new Vector3(z.Position.X+Mathf.Cos(a)*z.Radius,.12f,z.Position.Y+Mathf.Sin(a)*z.Radius)); }
            }
            for (;n < zoneViews.Count;n++) zoneViews[n].gameObject.SetActive(false);
            n = 0;
            foreach (var p in combat.Pickups)
            {
                if (n >= pickupViews.Count) break;
                var t = pickupViews[n++]; t.gameObject.SetActive(true);
                t.position = new Vector3(p.Position.X,.55f+Mathf.Sin(sim.State.Now*3)*.08f,p.Position.Y);
                t.rotation = Quaternion.Euler(0,sim.State.Now*30, p.Id == "ELITE" ? 0 : 45);
                t.localScale = p.Id == "ELITE" ? new Vector3(.8f,.5f,.6f) : Vector3.one*.45f;
                Color c = p.Id == "D02" ? new Color(.9f,.35f,.4f) : p.Id == "D03" ? new Color(.35f,.65f,1) : p.Id == "D04" ? new Color(1,.65f,.2f) : new Color(.9f,.8f,.4f);
                block.SetColor("_BaseColor",c); t.GetComponent<Renderer>().SetPropertyBlock(block);
            }
            for (;n < pickupViews.Count;n++) pickupViews[n].gameObject.SetActive(false);
        }
    }
}
