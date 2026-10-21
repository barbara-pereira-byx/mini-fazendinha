using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using MiniFazendinha.Planting;

namespace MiniFazendinha.EditorTools
{
    public static class FarmDragReview
    {
        [MenuItem("MiniFazendinha/Drag Tools/Capture Closed Context Menu")]
        public static void CaptureClosed()
        {
            var interactor=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();interactor.CancelTool();Capture();
            File.Copy("Screenshots/MiniFazendinha-DragTools-Review.png","Screenshots/MiniFazendinha-ContextMenu-Closed.png",true);
            interactor.OpenContextMenu(UnityEngine.Object.FindAnyObjectByType<FarmPlot>());interactor.SelectHarvestTool();Capture();
        }
        [MenuItem("MiniFazendinha/Drag Tools/Capture Current Play View")]
        public static void Capture()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
            var cam=Camera.main;var canvas=UnityEngine.Object.FindAnyObjectByType<FarmHUD>().GetComponent<Canvas>();
            var oldTarget=cam.targetTexture;float oldAspect=cam.aspect;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var oldActive=RenderTexture.active;
            var target=new RenderTexture(1440,1000,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1440,1000,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=target;cam.aspect=1.44f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1440,1000),0,0);texture.Apply();
                Directory.CreateDirectory("Screenshots");File.WriteAllBytes("Screenshots/MiniFazendinha-DragTools-Review.png",texture.EncodeToPNG());
                var log=new StringBuilder("Direct render of CURRENT Play Mode state; screen-overlay HUD temporarily rendered by the same camera, restored afterwards.\n");log.AppendLine("Orthographic size="+cam.orthographicSize+" | tilt="+cam.transform.eulerAngles.x+" | screenshot aspect=1.44");
                foreach(var plot in UnityEngine.Object.FindObjectsByType<FarmPlot>().OrderBy(p=>p.name))log.AppendLine(plot.name+" | "+plot.State+" | crop="+(plot.Crop==null?"none":plot.Crop.Identifier)+" | visual stage="+plot.GetComponent<FarmPlotView>().VisualStage+" | plants="+(plot.GetComponent<FarmPlotView>().Display==null?0:plot.GetComponent<FarmPlotView>().Display.childCount));
                File.WriteAllText("Library/MiniFazendinha-DragReview.txt",log.ToString());
            }
            finally
            {
                RenderTexture.active=oldActive;cam.targetTexture=oldTarget;cam.aspect=oldAspect;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;Canvas.ForceUpdateCanvases();target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}