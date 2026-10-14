using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Waffle;
using Reverb.Drafts.RC01.Presentation.GameplayNext;

namespace Reverb.Drafts.RC01.Presentation
{
    [DefaultExecutionOrder(220)]
    public sealed class AtmosphereDirector : MonoBehaviour
    {
        public GameLoop loop;
        public GardenWeather weather;
        public CameraFollow cameraRig;
        public Material volumeMaterial, splashMaterial, sparkMaterial;
        public bool scenicView;
        public bool humanAndGhosts;
        public int FootstepsEmitted { get; private set; }
        public int ImpactsEmitted { get; private set; }
        public int LightVolumeCount => cones.Count;
        sealed class Cone { public Transform root; public Light light; public Material material; }
        readonly List<Cone> cones = new List<Cone>(3);
        readonly System.Random random = new System.Random(8187);
        Mesh coneMesh;
        ParticleSystem feet, dust, sparks;
        ParticleSystem[] particles;
        Light[] flashes;
        float[] flashUntil;
        Transform visual;
        Vector3 visualOrigin, previous, recoil;
        float distanceCarry;
        int footSide, sparkBudget;
        bool initialized;

        void Start()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (weather == null) weather = FindAnyObjectByType<GardenWeather>();
            if (cameraRig == null) cameraRig = FindAnyObjectByType<CameraFollow>();
            if (loop == null || volumeMaterial == null) { enabled = false; return; }
            coneMesh = ConeMesh();
            if (weather?.searchlights != null) foreach (var light in weather.searchlights)
            {
                if (light == null || cones.Count == 3) continue;
                var root = new GameObject("Rain haze · local light volume"); root.transform.SetParent(transform, false);
                var material = new Material(volumeMaterial); material.SetFloat("_Mode", 1f); material.SetFloat("_Cull", 1f);
                root.AddComponent<MeshFilter>().sharedMesh = coneMesh;
                var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                cones.Add(new Cone { root = root.transform, light = light, material = material });
            }
            feet = Particles("Wet footsteps", splashMaterial, 96, .38f, .3f, true);
            dust = Particles("Footstep drift", volumeMaterial, 80, .65f, .55f, false);
            sparks = Particles("Surface impact fragments", sparkMaterial, 128, .28f, .045f, false);
            particles = new[] {feet,dust,sparks};
            var gravity = sparks.main; gravity.gravityModifier = .6f;
            var sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch; sparkRenderer.lengthScale = 2; sparkRenderer.velocityScale = .08f;
            flashes = new Light[2]; flashUntil = new float[2];
            for (int i = 0; i < flashes.Length; i++)
            {
                var go = new GameObject("Brief impact light " + i); go.transform.SetParent(transform, false);
                var light = go.AddComponent<Light>(); light.type = LightType.Point; light.range = 2.2f;
                light.color = humanAndGhosts?new Color(1,.13f,.09f):new Color(1f,.72f,.38f); light.intensity = 0; light.shadows = LightShadows.None; flashes[i] = light;
            }
            visual = loop.playerView != null ? loop.playerView.Find("Visual") : null;
            if (visual != null) visualOrigin = visual.localPosition;
            previous = new Vector3(loop.Sim.PlayerX, 0, loop.Sim.PlayerZ);
            initialized = true; loop.PresentationDispatched += OnEvents;
        }

        ParticleSystem Particles(string label, Material material, int cap, float lifetime, float size, bool ground)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = true; main.maxParticles = cap;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0;
            main.startLifetime = lifetime; main.startSize = size;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,.12f), new GradientAlphaKey(0,1) }); color.color = gradient;
            var scale = ps.sizeOverLifetime; scale.enabled = true; scale.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0,.25f,1,1.5f));
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            if (ground && weather?.splashes != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = weather.splashes.GetComponent<ParticleSystemRenderer>().mesh;
                renderer.alignment = ParticleSystemRenderSpace.World;
            }
            ps.Play(); ps.Pause(); return ps;
        }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            if (!initialized || loop.Sim.State.Phase != RunPhase.Playing) return;
            foreach (var item in events)
            {
                Vector3 direction = new Vector3(item.DirectionX,0,item.DirectionZ);
                if (item.Type == PresentationEventType.Shot)
                {
                    recoil = Vector3.ClampMagnitude(recoil - direction * .035f,.075f);
                    cameraRig?.AddVisualImpulse(-direction * .018f);
                }
                if (item.Type == PresentationEventType.PlayerHurt) cameraRig?.AddVisualImpulse(cameraRig.transform.right * .16f);
                if (item.Type == PresentationEventType.Dodge)
                {
                    Vector3 movement = new Vector3(loop.Sim.PlayerX,0,loop.Sim.PlayerZ) - previous;
                    cameraRig?.AddVisualImpulse(-movement.normalized * .12f);
                }
                if ((item.Type != PresentationEventType.EnemyHit && item.Type != PresentationEventType.EnemyKilled) || sparkBudget >= 24) continue;
                Vector3 position = new Vector3(item.X,.7f,item.Z);
                if ((position - new Vector3(loop.Sim.PlayerX,0,loop.Sim.PlayerZ)).sqrMagnitude > 225f) continue;
                int count = item.Type == PresentationEventType.EnemyKilled ? 7 : 3;
                for (int i = 0; i < count && sparkBudget < 24; i++,sparkBudget++)
                    sparks.Emit(new ParticleSystem.EmitParams { position = position, velocity = new Vector3(RandomSigned()*2,1.2f+Next()*2,RandomSigned()*2),
                        startSize = .03f+Next()*.025f, startLifetime = .18f+Next()*.2f, startColor = humanAndGhosts?new Color(.85f,.065f,.075f,.7f):new Color(1f,.63f,.26f,.8f) },1);
                ImpactsEmitted++;
                int index = ImpactsEmitted % 2; flashes[index].transform.position = position;
                flashUntil[index] = loop.Sim.State.Now + .07f;
            }
        }

        void LateUpdate()
        {
            if (!initialized || loop?.Sim == null) return;
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) scenicView = !scenicView;
            var hud = loop.GetComponent<RC01GrowthHud>(); if (hud != null) hud.scenicView = scenicView;
            var feedback = loop.GetComponent<GameplayFeedbackView>(); if (feedback != null) feedback.scenicView = scenicView;
            bool playing = loop.Sim.State.Phase == RunPhase.Playing;
            float now = loop.Sim.State.Now;
            foreach (var cone in cones)
            {
                bool visible = cone.light != null && cone.light.enabled && !loop.Sim.State.Phase.IsTerminal();
                cone.root.gameObject.SetActive(visible); if (!visible) continue;
                float length = Mathf.Min(20f,cone.light.range);
                float radius = Mathf.Tan(cone.light.spotAngle * Mathf.Deg2Rad * .5f) * length;
                cone.root.SetPositionAndRotation(cone.light.transform.position, cone.light.transform.rotation);
                cone.root.localScale = new Vector3(radius,radius,length);
                cone.material.SetColor("_BaseColor",new Color(cone.light.color.r,cone.light.color.g,cone.light.color.b,.8f));
                cone.material.SetFloat("_Density",.055f * Mathf.Clamp01(cone.light.intensity / 20f));
                cone.material.SetFloat("_PatternedLight",cone.light.cookie!=null?1:0);
                if(cone.light.cookie!=null)cone.material.SetTexture("_BeamCookie",cone.light.cookie);
            }
            if (loop.Sim.State.Phase.IsTerminal()) { ClearParticles(); foreach(var light in flashes) light.intensity=0; return; }
            if (!playing) return;
            float dt = Mathf.Min(.05f,loop.Sim.LastDt); if (dt <= 0) return;
            Vector3 current = new Vector3(loop.Sim.PlayerX,0,loop.Sim.PlayerZ), delta = current-previous;
            distanceCarry += Mathf.Min(delta.magnitude,3f);
            Vector3 side = Vector3.Cross(Vector3.up,delta.normalized);
            for (int step=0;distanceCarry>=.65f&&step<3;step++)
            {
                distanceCarry-=.65f;footSide=1-footSide;FootstepsEmitted++;
                Vector3 p=current+side*(footSide==0?-.16f:.16f);
                var terrain=weather?.ground;p.y=(terrain!=null&&terrain.isActiveAndEnabled?terrain.SampleHeight(p)+terrain.transform.position.y:0)+.045f;
                bool wet=weather!=null&&weather.CurrentWetness>.2f;
                var target=wet?feet:dust;
                target.Emit(new ParticleSystem.EmitParams {position=p,velocity=wet?Vector3.zero:new Vector3(.15f,.13f,.08f),
                    startColor=wet?new Color(.55f,.72f,.75f,.7f):new Color(.4f,.35f,.25f,.3f),startSize=wet?.4f:.65f,startLifetime=wet?.4f:.65f},1);
            }
            foreach(var ps in particles) ps.Simulate(dt,true,false,false);
            for(int i=0;i<flashes.Length;i++)flashes[i].intensity=Mathf.Max(0,(flashUntil[i]-now)/.07f)*2.5f;
            recoil*=Mathf.Exp(-dt*17);
            if(visual!=null)
            {
                visual.localPosition=visualOrigin+recoil+Vector3.up*(!humanAndGhosts&&delta.sqrMagnitude>.0001f?Mathf.Sin(now*16)*.018f:0);
                var angle=visual.localEulerAngles;
                Vector3 velocity=delta/Mathf.Max(dt,.001f);
                angle.x=Mathf.Clamp(-velocity.z*.35f,-3,3);angle.z=Mathf.Clamp(velocity.x*.35f,-3,3);visual.localEulerAngles=angle;
            }
            previous=current;sparkBudget=0;
        }
        float Next() => (float)random.NextDouble();
        float RandomSigned() => Next()*2-1;
        void ClearParticles() { if(particles!=null)foreach(var ps in particles)if(ps!=null)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnEnable() { if(initialized&&loop!=null){loop.PresentationDispatched+=OnEvents;foreach(var ps in particles){ps.Play();ps.Pause();}} }
        void OnDisable()
        {
            if(loop!=null)
            {
                loop.PresentationDispatched-=OnEvents;
                var hud=loop.GetComponent<RC01GrowthHud>();if(hud!=null)hud.scenicView=false;
                var feedback=loop.GetComponent<GameplayFeedbackView>();if(feedback!=null)feedback.scenicView=false;
            }
            ClearParticles();foreach(var cone in cones)if(cone.root!=null)cone.root.gameObject.SetActive(false);
            if(flashes!=null)foreach(var light in flashes)if(light!=null)light.intensity=0;
            if(visual!=null)visual.localPosition=visualOrigin;
        }
        void OnDestroy() { foreach(var cone in cones)if(cone.material!=null)Destroy(cone.material);if(coneMesh!=null)Destroy(coneMesh); }
        static Mesh ConeMesh()
        {
            const int segments=32;
            var vertices=new Vector3[segments+2];var colors=new Color[vertices.Length];var indices=new int[segments*6];
            vertices[0]=Vector3.zero;vertices[segments+1]=Vector3.forward;
            for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;vertices[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),1);}
            for(int i=0;i<segments;i++){int next=(i+1)%segments+1,k=i*6;indices[k]=0;indices[k+1]=next;indices[k+2]=i+1;indices[k+3]=segments+1;indices[k+4]=i+1;indices[k+5]=next;}
            for(int i=0;i<colors.Length;i++)colors[i]=Color.white;
            var mesh=new Mesh{name="Local fog cone",vertices=vertices,triangles=indices,colors=colors};mesh.RecalculateBounds();return mesh;
        }
    }
}
