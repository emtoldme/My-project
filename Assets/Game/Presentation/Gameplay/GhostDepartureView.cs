using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Bounded visual residue pool, only actual EnemyKilled events create a residue.</summary>
    [DefaultExecutionOrder(240)]
    public sealed class GhostDepartureView : MonoBehaviour
    {
        public GameLoop loop; public GameObject ghostPrefab;
        sealed class Residue{public RedGhostView view;public float start;public int serial;public bool alive;}
        Residue[] pool;CombatViews combatViews;int next;
        public int Departures {get;private set;}
        public int Capacity=>pool?.Length??0;
        void Start()
        {
            if(loop==null)loop=GetComponent<GameLoop>();if(loop==null||ghostPrefab==null){enabled=false;return;}
            combatViews=loop.GetComponent<CombatViews>();pool=new Residue[12];
            for(int i=0;i<pool.Length;i++){var go=Instantiate(ghostPrefab,transform);go.name="Departing echo "+i;pool[i]=new Residue{view=go.GetComponent<RedGhostView>()};go.SetActive(false);}
            loop.PresentationDispatched+=Events;
        }
        void Events(IReadOnlyList<PresentationEvent> events)
        {
            foreach(var e in events)if(e.Type==PresentationEventType.EnemyKilled)
            {
                var p=pool[next++%pool.Length];p.start=e.Now;p.serial=e.Data;p.alive=true;
                combatViews.TryGetEnemyVisual(e.Data,out var scale,out var rotation);
                p.view.transform.SetPositionAndRotation(new Vector3(e.X,0,e.Z),rotation);p.view.transform.localScale=scale;
                p.view.gameObject.SetActive(true);p.view.Afterimage(e.Now,e.Data*2.39996f,0,new Color(.7f,.045f,.065f));Departures++;
            }
        }
        void LateUpdate()
        {
            if(pool==null||loop?.Sim==null)return;float now=loop.Sim.State.Now;
            foreach(var p in pool)if(p.alive)
            {
                float age=(now-p.start)/.65f;
                if(age>=1){p.alive=false;p.view.gameObject.SetActive(false);}
                else p.view.Afterimage(now,p.serial*2.39996f,age,new Color(.7f,.045f,.065f));
            }
        }
        void OnDestroy(){if(loop!=null)loop.PresentationDispatched-=Events;}
    }
}
