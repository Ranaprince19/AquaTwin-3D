using UnityEngine;

namespace LeakWatch
{
    /// Shared references set once by Bootstrap so scene components can find each other.
    public static class Ctx
    {
        public static Camera Cam;
        public static CameraRig Rig;
        public static WaterSimulation Sim;
        public static ReceivedData Received;
        public static LeakDetector Detector;
        public static Station[] Stations = new Station[2];
        public static PipeFlow Flow;
        public static LeakFx Leak;
        public static PacketPipeline Packets;
        public static DashboardUI Dashboard;
        public static AnalysisPanelUI Analysis;
        public static HudUI Hud;
        public static StoryController Story;
        public static RectTransform CanvasRect;
        public static RectTransform WorldRect;
        public static System.Collections.Generic.List<RectTransform> BlockRects = new System.Collections.Generic.List<RectTransform>();
        public static RectTransform SafeRoot;
    }
}
