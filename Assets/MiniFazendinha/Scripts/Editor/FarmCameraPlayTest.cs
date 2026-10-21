using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MiniFazendinha.EditorTools
{
    public static class FarmCameraPlayTest
    {
        static MiniFazendinha.FarmCameraController controller;
        static Camera camera;
        static Mouse testMouse, previousMouse;
        static Mouse[] isolatedMice;static FarmTestInputScope inputScope;
        static Vector3 originalFocus, releasedFocus;
        static float originalZoom;
        static Vector2 pointer;
        static int stage, frame, firstFrame;
        static double deadline;
        static StringBuilder report;
        static MouseState? pendingState;
        [MenuItem("MiniFazendinha/Test Camera In Play Mode")]
        public static void Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            controller=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.FarmCameraController>();
            if(controller==null)throw new InvalidOperationException("Farm camera not found.");
            camera=controller.GetComponent<Camera>();originalFocus=controller.FocusPoint;originalZoom=camera.orthographicSize;
            inputScope=new FarmTestInputScope();previousMouse=Mouse.current;isolatedMice=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();foreach(var device in isolatedMice)InputSystem.DisableDevice(device);testMouse=InputSystem.AddDevice<Mouse>("MiniFazendinha_TestMouse");testMouse.MakeCurrent();
            pointer=new Vector2(camera.pixelWidth*0.5f,camera.pixelHeight*0.5f);
            report=new StringBuilder();stage=0;firstFrame=frame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+25;
            InputSystem.onBeforeUpdate+=BeforeInput; InputSystem.onAfterUpdate+=AfterInput;
            EditorApplication.update+=Tick;
        }
        static void Require(bool condition,string message) {if(!condition)throw new Exception(message);report.AppendLine("PASS: "+message);}
        static void Queue(Vector2 pos,bool right=false,float scroll=0,bool left=false)
        {
            var state=new MouseState {position=pos,scroll=new Vector2(0,scroll)};
            if(right)state=state.WithButton(MouseButton.Right);if(left)state=state.WithButton(MouseButton.Left);
            pendingState=state;
        }
        static void BeforeInput()
        {
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || testMouse==null || !pendingState.HasValue)return;
            InputSystem.QueueStateEvent(testMouse,pendingState.Value);pendingState=null;
        }
        static void AfterInput()
        {
            if(InputState.currentUpdateType==InputUpdateType.Dynamic && testMouse!=null)testMouse.MakeCurrent();
        }
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying)throw new Exception("Play Mode stopped during test.");
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Player loop timeout.");
                if(Time.frameCount<frame+3)return;
                frame=Time.frameCount;
                switch(stage++)
                {
                    case 0:
                        Require(Time.frameCount>firstFrame,"Player loop is advancing");
                        controller.ZoomAt(-1000,pointer);Require(Mathf.Approximately(camera.orthographicSize,controller.MaximumZoom),"Maximum zoom clamps correctly");
                        controller.ZoomAt(1000,pointer);Require(Mathf.Approximately(camera.orthographicSize,controller.MinimumZoom),"Minimum zoom clamps correctly");
                        controller.PanGround(new Vector3(1000,0,1000));Require(Mathf.Abs(controller.FocusPoint.x)<=20 && Mathf.Abs(controller.FocusPoint.z)<=20,"Positive boundary clamps");
                        controller.PanGround(new Vector3(-2000,0,-2000));Require(Mathf.Abs(controller.FocusPoint.x)<=20 && Mathf.Abs(controller.FocusPoint.z)<=20,"Negative boundary clamps");
                        controller.PanGround(-controller.FocusPoint);Queue(pointer,false,-120);break;
                    case 1:
                        Require(camera.orthographicSize>controller.MinimumZoom,"Input System scroll changes zoom");
                        Queue(pointer,true);break;
                    case 2: Queue(pointer+new Vector2(160,90),true);break;
                    case 3:
                        Require(controller.FocusPoint.magnitude>0.1f,"Input System right-button drag pans the camera");
                        Queue(pointer+new Vector2(160,90));break;
                    case 4: releasedFocus=controller.FocusPoint;Queue(pointer+new Vector2(260,190));break;
                    case 5:
                        Require(Vector3.Distance(controller.FocusPoint,releasedFocus)<0.001f,"Releasing right button stops panning");
                        camera.aspect=9f/16f;controller.PanGround(new Vector3(1000,0,1000));
                        Require(Mathf.Abs(controller.FocusPoint.x)<=20 && Mathf.Abs(controller.FocusPoint.z)<=20,"Portrait viewport retains farm bounds");
                        camera.ResetAspect();controller.PanGround(-controller.FocusPoint);controller.ZoomAt(-1000,pointer);Queue(pointer,false,0,true);break;
                    case 6: Queue(pointer+new Vector2(160,90),false,0,true);break;
                    case 7: Require(controller.LeftGestureWasPan&&controller.FocusPoint.magnitude>0.1f,"Left drag pans at maximum zoom without planting");Queue(pointer+new Vector2(160,90));break;
                    case 8: releasedFocus=controller.FocusPoint;Queue(pointer+new Vector2(260,190));break;
                    case 9: Require(Vector3.Distance(controller.FocusPoint,releasedFocus)<0.001f,"Releasing left button stops panning");Require(Time.frameCount-firstFrame>=27,"Multiple gameplay frames observed");Finish(null);break;
                }
            }
            catch(Exception ex) {Finish(ex);}
        }
        static void Finish(Exception error)
        {
            EditorApplication.update-=Tick; InputSystem.onBeforeUpdate-=BeforeInput; InputSystem.onAfterUpdate-=AfterInput; pendingState=null;
            if(testMouse!=null) {InputSystem.RemoveDevice(testMouse);testMouse=null;}
            foreach(var device in isolatedMice)if(device.added)InputSystem.EnableDevice(device);if(previousMouse!=null && previousMouse.added)previousMouse.MakeCurrent();inputScope?.Dispose();
            if(camera!=null && controller!=null)
            {
                camera.ResetAspect();camera.orthographicSize=originalZoom;controller.PanGround(originalFocus-controller.FocusPoint);
            }
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL "+error);
            File.WriteAllText("Library/MiniFazendinha-CameraTest.txt",report.ToString());
            if(error!=null)Debug.LogError(report.ToString());else Debug.Log("MiniFazendinha camera test PASS");
        }
    }
}