using UnityEngine;

namespace MiniFazendinha.Planting
{
    [DisallowMultipleComponent, RequireComponent(typeof(FarmPlot))]
    public sealed class FarmPlotView : MonoBehaviour
    {
        [SerializeField] Transform cropAnchor;
        [SerializeField] LineRenderer outline, progressTrack, progressFill;
        [SerializeField] Material selectedMaterial, growingMaterial, readyMaterial;
        FarmPlot plot;
        Transform display;
        bool hovered, validHover;
        int stage=-2;
        float pulseUntil;
        public int VisualStage => stage;
        public Transform Display => display;
        void Awake() {plot=GetComponent<FarmPlot>();}
        void OnEnable() {if(plot==null)plot=GetComponent<FarmPlot>();plot.Changed+=Refresh;Refresh();}
        void OnDisable() {if(plot!=null)plot.Changed-=Refresh;}
        public void SetSelected(bool selected) {SetHover(selected,true);}
        public void SetHover(bool value,bool valid) {hovered=value;validHover=valid;RefreshOutline();}
        public void Pulse(PlotActionResult result) {if(result==PlotActionResult.Planted||result==PlotActionResult.Harvested)pulseUntil=Time.unscaledTime+0.65f;RefreshOutline();}
        int CurrentStage() => plot.State==FarmPlotState.Empty?-1:plot.State==FarmPlotState.ReadyToHarvest?2:plot.Progress<0.42f?0:1;
        void LateUpdate() {if(CurrentStage()!=stage)Refresh();RefreshOutline();}
        void Refresh()
        {
            stage=CurrentStage();
            if(display!=null){display.gameObject.SetActive(false);Destroy(display.gameObject);display=null;}
            // Progress is shown only in the hover HUD, never on all plots at once.
            if(progressTrack!=null)progressTrack.enabled=false;if(progressFill!=null)progressFill.enabled=false;
            if(stage>=0 && plot.Crop!=null)
            {
                var crop=plot.Crop;var visual=stage==2?crop.MatureVisual:stage==1?crop.IntermediateVisual:crop.GrowingVisual;
                if(visual==null || visual.prefab==null)visual=crop.GrowingVisual;
                display=new GameObject("CropVisual_Runtime").transform;display.SetParent(cropAnchor,false);display.gameObject.layer=2;
                if(visual!=null && visual.prefab!=null)
                    for(int x=0;x<crop.VisualRows;x++)for(int z=0;z<crop.VisualRows;z++)
                    {
                        var go=Instantiate(visual.prefab,display);go.name=crop.Identifier+"_"+x+"_"+z;
                        go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(visual.eulerAngles);go.transform.localScale=Vector3.Scale(visual.prefab.transform.localScale,visual.scale);
                        var renderers=go.GetComponentsInChildren<Renderer>();
                        if(renderers.Length>0)
                        {
                            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                            var center=cropAnchor.TransformPoint(new Vector3((x-(crop.VisualRows-1)*0.5f)*crop.VisualSpacing,0,(z-(crop.VisualRows-1)*0.5f)*crop.VisualSpacing));
                            go.transform.position+=new Vector3(center.x-bounds.center.x,cropAnchor.position.y-bounds.min.y,center.z-bounds.center.z);
                        }
                        foreach(var child in go.GetComponentsInChildren<Transform>(true))child.gameObject.layer=2;
                        foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                        if(visual.materialOverride!=null)foreach(var renderer in renderers){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=visual.materialOverride;renderer.sharedMaterials=materials;}
                    }
            }
            RefreshOutline();
        }
        void RefreshOutline()
        {
            if(outline==null||plot==null)return;
            bool ready=plot.State==FarmPlotState.ReadyToHarvest,pulsing=Time.unscaledTime<pulseUntil;
            outline.enabled=hovered||ready||pulsing;
            var hoverMaterial=validHover?selectedMaterial:progressTrack!=null?progressTrack.sharedMaterial:growingMaterial;
            outline.sharedMaterial=hovered?hoverMaterial:ready?readyMaterial:selectedMaterial;
            outline.widthMultiplier=(hovered?0.12f:0.07f)*(pulsing?1.3f:1f);
        }
        public void EmitHarvest()
        {
            var effect=new GameObject("HarvestParticles_Runtime");effect.layer=2;effect.transform.position=cropAnchor.position+Vector3.up*0.4f;effect.transform.rotation=Quaternion.Euler(-90,0,0);
            var particles=effect.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.duration=0.7f;main.loop=false;main.startLifetime=0.65f;main.startSize=new ParticleSystem.MinMaxCurve(0.08f,0.17f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.4f,2.3f);main.gravityModifier=0.7f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=16;
            var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,12)});
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=30;shape.radius=0.5f;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=readyMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            particles.Play();Destroy(effect,1.5f);
        }
    }
}