using System.Collections.Generic;
using UnityEngine;
using MiniFazendinha.Planting;

namespace MiniFazendinha.Animals
{
    // Small, obstacle-aware grid search. Paths retain physical clearance at every step.
    [DisallowMultipleComponent]
    public sealed class FarmAnimalNavigation : MonoBehaviour
    {
        const float Cell=.25f;
        Bounds walkBounds;
        Vector3 destination;
        float groundY=.075f, bodyRadius=.25f, currentSpeed;
        bool hasDestination; double neighbourBlockedAt;
        AnimalShelter home;
        AnimalShelter[] pens;
        readonly List<Vector3> route=new List<Vector3>();
        int routeIndex;
        readonly Collider[] overlaps=new Collider[32];
        readonly RaycastHit[] hits=new RaycastHit[32];
        public Bounds WalkBounds=>walkBounds;
        public bool HasDestination=>hasDestination;
        public Vector3 Destination=>destination;
        public float CurrentSpeed=>currentSpeed;
        public bool Confined {get;set;}=true;
        public void Configure(Bounds area,float inset,float radius)
        {walkBounds=area;groundY=.075f;bodyRadius=Mathf.Max(.13f,radius);home=GetComponent<FarmAnimalController>().Home;pens=FindObjectsByType<AnimalShelter>(FindObjectsSortMode.None);}
        bool Allowed(Vector3 p)
        {
            if(home==null)return true;
            if(Confined)return home.Contains(p,bodyRadius+.05f);
            foreach(var pen in pens)if(pen!=null&&pen!=home&&pen.PermittedSpecies!=null&&pen.Contains(p,-bodyRadius))return false;
            return true;
        }
        bool Obstacle(Collider c)
        {
            if(c==null||c.transform==transform||c.transform.IsChildOf(transform)||c.GetComponentInParent<FarmAnimalController>()!=null)return false;
            if(c.GetComponentInParent<FarmPlot>()!=null)return true;
            // Ignore the ground, but retain fences, troughs and buildings.
            return c.bounds.max.y>groundY+.18f && c.bounds.min.y<groundY+.8f;
        }
        bool Free(Vector3 p)
        {
            if(!Allowed(p))return false;
            int n=Physics.OverlapSphereNonAlloc(p+Vector3.up*.4f,bodyRadius,overlaps,~0,QueryTriggerInteraction.Ignore);
            if(n==overlaps.Length)return false;
            for(int i=0;i<n;i++){if(Obstacle(overlaps[i]))return false;var animal=overlaps[i].GetComponentInParent<FarmAnimalController>();if(animal!=null&&animal.gameObject!=gameObject&&Vector3.Distance(p,animal.transform.position)<bodyRadius+animal.Definition.BodyRadius)return false;}
            return true;
        }
        bool Clear(Vector3 a,Vector3 b,bool avoidAnimals=false)
        {
            if(avoidAnimals)foreach(var animal in FarmAnimalRegistry.All){if(animal==null||animal.gameObject==gameObject)continue;Vector3 edge=b-a;float t=edge.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(animal.transform.position-a,edge)/edge.sqrMagnitude);if(Vector3.Distance(a+edge*t,animal.transform.position)<bodyRadius+animal.Definition.BodyRadius)return false;}
float d=Vector3.Distance(a,b);
            for(float t=0;t<=d;t+=Cell*.5f)if(!Allowed(Vector3.Lerp(a,b,d==0?0:t/d)))return false;
            if(!Allowed(b))return false;
            int n=Physics.SphereCastNonAlloc(a+Vector3.up*.4f,bodyRadius,(b-a).normalized,hits,d,~0,QueryTriggerInteraction.Ignore);
            if(n==hits.Length)return false;
            for(int i=0;i<n;i++)if(Obstacle(hits[i].collider))return false;
            int end=Physics.OverlapSphereNonAlloc(b+Vector3.up*.4f,bodyRadius,overlaps,~0,QueryTriggerInteraction.Ignore);for(int i=0;i<end;i++)if(Obstacle(overlaps[i]))return false;
            return true;
        }
        public bool ChooseWanderPoint()
        {
            Bounds b=Confined&&home!=null?home.RoamingBounds:walkBounds;
            if(!Confined&&home!=null){for(int n=0;n<8;n++){var nearby=transform.position+new Vector3(Random.Range(-2f,2f),0,Random.Range(-3f,3f));nearby.y=groundY;if(!home.Contains(nearby)&&Free(nearby)&&TrySetDestination(nearby))return true;}}
            for(int i=0;i<12;i++)
            {
                var p=new Vector3(Random.Range(b.min.x+.4f,b.max.x-.4f),groundY,Random.Range(b.min.z+.4f,b.max.z-.4f));
                if(!Confined&&home!=null&&home.Contains(p))continue;
                if(Free(p)&&TrySetDestination(p))return true;
            }
            return false;
        }
        public void SetDestination(Vector3 p)=>TrySetDestination(p);
        public bool TrySetDestination(Vector3 p)
        {
            p.y=groundY;destination=p;route.Clear();routeIndex=0;hasDestination=false;neighbourBlockedAt=0;
            if(!Free(p))return false;
            if(Clear(transform.position,p,true)){route.Add(p);hasDestination=true;return true;}
            var b=home==null?walkBounds:home.FarmBounds;
            int width=Mathf.CeilToInt(b.size.x/Cell)+1,height=Mathf.CeilToInt(b.size.z/Cell)+1;
            int Index(Vector3 v)=>Mathf.Clamp(Mathf.RoundToInt((v.z-b.min.z)/Cell),0,height-1)*width+Mathf.Clamp(Mathf.RoundToInt((v.x-b.min.x)/Cell),0,width-1);
            Vector3 Point(int k)=>new Vector3(b.min.x+(k%width)*Cell,groundY,b.min.z+(k/width)*Cell);
            int start=Index(transform.position),goal=Index(p),count=width*height;
            if(!Free(Point(start))||!Clear(transform.position,Point(start),true))
            {int replacement=-1;float nearest=float.PositiveInfinity;int sx=start%width,sz=start/width;for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){int x=sx+dx,z=sz+dz;if(x<0||x>=width||z<0||z>=height)continue;int k=z*width+x;float distance=Vector3.Distance(transform.position,Point(k));if(distance<nearest&&Free(Point(k))&&Clear(transform.position,Point(k),true)){replacement=k;nearest=distance;}}if(replacement<0)return false;start=replacement;}
            var cost=new float[count];var from=new int[count];var closed=new bool[count];var free=new byte[count];var open=new List<int>{start};
            for(int i=0;i<count;i++){cost[i]=float.PositiveInfinity;from[i]=-1;}cost[start]=0;
            for(int iterations=0;open.Count>0&&iterations<count;iterations++)
            {
                int best=0;float score=float.PositiveInfinity;
                for(int j=0;j<open.Count;j++){int k=open[j];float s=cost[k]+Vector3.Distance(Point(k),p);if(s<score){score=s;best=j;}}
                int node=open[best];open.RemoveAt(best);if(closed[node])continue;closed[node]=true;
                if(node==goal)
                {
                    var reverse=new List<Vector3>{p};int k=node;
                    while(k!=start&&k>=0){reverse.Add(Point(k));k=from[k];}
                    reverse.Add(Point(start));reverse.Reverse();Vector3 origin=transform.position;
                    for(int j=0;j<reverse.Count;j++)
                    {int far=j;while(far+1<reverse.Count&&Clear(origin,reverse[far+1],true))far++;if(!Clear(origin,reverse[far],true))return false;route.Add(reverse[far]);origin=reverse[far];j=far;}
                    hasDestination=route.Count>0;return hasDestination;
                }
                int x=node%width,z=node/width;
                foreach(var offset in new[]{new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1)})
                {
                    int nx=x+offset.x,nz=z+offset.y;if(nx<0||nx>=width||nz<0||nz>=height)continue;int next=nz*width+nx;
                    if(closed[next])continue;if(free[next]==0)free[next]=(byte)(Free(Point(next))?1:2);if(free[next]==2)continue;
                    float g=cost[node]+Cell;if(g>=cost[next])continue;cost[next]=g;from[next]=node;open.Add(next);
                }
            }
            return false;
        }
                bool RecoverContact(float dt,float speed)
        {
            var origin=transform.position+Vector3.up*.4f;int n=Physics.OverlapSphereNonAlloc(origin,bodyRadius,overlaps,~0,QueryTriggerInteraction.Ignore);Vector3 escape=Vector3.zero;
            for(int i=0;i<n;i++)
            {
                var c=overlaps[i];if(!Obstacle(c))continue;Vector3 away=origin-c.ClosestPoint(origin);away.y=0;float distance=away.magnitude;
                if(distance>.001f&&distance<bodyRadius+.005f)escape+=away.normalized*(bodyRadius-distance+.015f);
            }
            if(escape.sqrMagnitude<.000001f)return false;
            var next=transform.position+Vector3.ClampMagnitude(escape,speed*dt);if(!Allowed(next))return false;
            // Walk out of a shallow contact along its outward normal, never relocate across a fence.
            transform.position=next;currentSpeed=speed;return true;
        }
        public void ClearDestination(){hasDestination=false;route.Clear();currentSpeed=0;}
        public bool Advance(float speed,float dt,float separationRadius,IEnumerable<FarmAnimalController> others,bool allowOutside=false)
        {
            if(RecoverContact(dt,speed))return false;
            if(!hasDestination||routeIndex>=route.Count){hasDestination=false;currentSpeed=0;return false;}
            Vector3 target=route[routeIndex],pos=transform.position;target.y=pos.y;
            float distance=Vector3.Distance(pos,target);
            if(distance<.09f)
            {routeIndex++;if(routeIndex>=route.Count){hasDestination=false;currentSpeed=0;return true;}target=route[routeIndex];target.y=pos.y;}
            var direction=(target-pos).normalized;
            currentSpeed=Mathf.MoveTowards(currentSpeed,speed,dt*speed*3f);
            float step=Mathf.Min(currentSpeed*dt,Vector3.Distance(pos,target));
            var next=pos+direction*step;
            if(!Clear(pos,next)){currentSpeed=0;hasDestination=false;return false;}
            if(others!=null)foreach(var a in others)
            {
                if(a==null||a.gameObject==gameObject)continue;
                if(Vector3.Distance(next,a.transform.position)<bodyRadius+a.Definition.BodyRadius&&Vector3.Distance(pos,a.transform.position)>Vector3.Distance(next,a.transform.position))
                                {
                    // A moving neighbour invalidates the plan; the next search routes around it.
                    if(neighbourBlockedAt==0)neighbourBlockedAt=Time.timeAsDouble;
                    if(Time.timeAsDouble-neighbourBlockedAt>.8)hasDestination=false;
                    currentSpeed=0;return false;
                }
            }
            neighbourBlockedAt=0;var rotation=Quaternion.LookRotation(direction);transform.rotation=Quaternion.RotateTowards(transform.rotation,rotation,140f*dt);
            float facing=Mathf.Clamp01(1-Quaternion.Angle(transform.rotation,rotation)/100f);
            transform.position=pos+(next-pos)*facing;
            return false;
        }
    }
}
