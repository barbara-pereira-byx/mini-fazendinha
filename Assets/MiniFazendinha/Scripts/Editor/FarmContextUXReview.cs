using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
    public static class FarmContextUXReview
    {
        public static void Capture(string name,int width=1600,int height=900)
        {
            var cam=Camera.main;var canvas=UnityEngine.Object.FindAnyObjectByType<FarmHUD>().GetComponent<Canvas>();
            var oldTarget=cam.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;var oldActive=RenderTexture.active;float oldAspect=cam.aspect;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=target;cam.aspect=(float)width/height;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
                var context=canvas.GetComponent<FarmContextMenu>();context.Refresh();context.Reposition();cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();Directory.CreateDirectory("Screenshots/ContextUX");File.WriteAllBytes("Screenshots/ContextUX/"+name+".png",texture.EncodeToPNG());
                if(canvas.GetComponent<FarmWarehousePanel>().IsOpen)AssertInside(canvas.GetComponent<FarmWarehousePanel>().Panel,(RectTransform)canvas.transform,"Warehouse",true);
                if(context.IsOpen)AssertInside(context.Panel,(RectTransform)canvas.transform,"Context",true);
            }
            finally{RenderTexture.active=oldActive;cam.targetTexture=oldTarget;cam.ResetAspect();canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;Canvas.ForceUpdateCanvases();target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static void AssertInside(RectTransform panel,RectTransform canvas,string label,bool onlyIfVisible=false)
        {
            if(onlyIfVisible&&!panel.gameObject.activeInHierarchy)return;
            var corners=new Vector3[4];panel.GetWorldCorners(corners);foreach(var corner in corners){var p=canvas.InverseTransformPoint(corner);if(!canvas.rect.Contains(p))throw new Exception(label+" exceeds canvas: "+p+" / "+canvas.rect);}
        }
    }
}