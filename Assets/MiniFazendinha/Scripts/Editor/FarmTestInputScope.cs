using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace MiniFazendinha.EditorTools
{
    // Test-only routing clone. Never edits the project's InputSettings asset.
    public sealed class FarmTestInputScope : IDisposable
    {
        static FarmTestInputScope active;
        InputSettings original,temporary;HideFlags originalFlags;
        public FarmTestInputScope()
        {
            if(active!=null)throw new InvalidOperationException("Another farm input test is active.");
            original=InputSystem.settings;originalFlags=original.hideFlags;
            // InputManager automatically destroys a previous HideAndDontSave default settings object.
            // Preserve it while swapping the test clone, then restore its flags in Dispose.
            if(originalFlags==HideFlags.HideAndDontSave)original.hideFlags=HideFlags.DontSave;
            temporary=UnityEngine.Object.Instantiate(original);temporary.hideFlags=HideFlags.DontSave;
            temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings=temporary;active=this;
            AssemblyReloadEvents.beforeAssemblyReload+=Dispose;EditorApplication.playModeStateChanged+=PlayState;
        }
        void PlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)Dispose();}
        public void Dispose()
        {
            if(temporary==null)return;
            AssemblyReloadEvents.beforeAssemblyReload-=Dispose;EditorApplication.playModeStateChanged-=PlayState;
            if(original==null)original=ScriptableObject.CreateInstance<InputSettings>();
            InputSystem.settings=original;original.hideFlags=originalFlags;UnityEngine.Object.DestroyImmediate(temporary);temporary=null;if(active==this)active=null;
        }
    }
}