using UnityEditor;
namespace MiniFazendinha.EditorTools
{
    // Preserve the existing menu entry; current tests exercise the refactored HUD.
    public static class FarmDragToolsTests
    {
        [MenuItem("MiniFazendinha/Drag Tools/Test Gestures In Play Mode")]
        public static void Start()=>FarmContextUXTests.Start();
    }
}