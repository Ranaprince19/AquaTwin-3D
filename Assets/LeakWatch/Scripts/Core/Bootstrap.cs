using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LeakWatch
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var root = new GameObject("LeakWatchRoot");

            var cam = EnsureCamera();
            Ctx.Cam = cam;
            Ctx.Rig = cam.gameObject.AddComponent<CameraRig>();

            Ctx.Sim = root.AddComponent<WaterSimulation>();
            Ctx.Received = new ReceivedData();
            Ctx.Detector = new LeakDetector();

            BuildCanvas(root.transform);
            BuildEventSystem();

            SkyBuilder.Build(root.transform, cam);
            PostProcessSetup.Build(root.transform, cam);

            var path = PipeBuilder.CreatePath();
            GroundBuilder.Build(root.transform, path);
            Ctx.Flow = PipeBuilder.Build(root.transform, path);

            var pump = BuildingBuilder.Build(root.transform, NetworkLayout.PumpX, NetworkLayout.PumpWidth, 6f, 1, new Color(0.5f, 0.58f, 0.66f), false, true);
            var ub = BuildingBuilder.Build(root.transform, NetworkLayout.UbX, NetworkLayout.UbWidth, 9f, 3, new Color(0.76f, 0.45f, 0.35f), true, false);
            var tp2 = BuildingBuilder.Build(root.transform, NetworkLayout.Tp2X, NetworkLayout.Tp2Width, 8f, 2, new Color(0.86f, 0.8f, 0.65f), true, false);
            BuildingBuilder.BuildTrees(root.transform);

            var clear = new Color(0f, 0f, 0f, 0f);
            LabelLayer.Create("Pump", 40, Color.white, clear, pump.signPos, Vector2.zero).SetVisible(true);
            LabelLayer.Create("UB", 52, Color.white, clear, ub.signPos, Vector2.zero).SetVisible(true);
            LabelLayer.Create("TP2", 52, Color.white, clear, tp2.signPos, Vector2.zero).SetVisible(true);

            Ctx.Stations[0] = StationBuilder.Build(root.transform, 0, NetworkLayout.StationAX, "UB-IN", ub.router);
            Ctx.Stations[1] = StationBuilder.Build(root.transform, 1, NetworkLayout.StationBX, "TP2-IN", tp2.router);

            var leakGo = new GameObject("LeakFx");
            Ctx.Leak = leakGo.AddComponent<LeakFx>();
            Ctx.Leak.Build(root.transform);

            var packets = new GameObject("Packets");
            packets.transform.SetParent(root.transform, false);
            Ctx.Packets = packets.AddComponent<PacketPipeline>();
            Ctx.Packets.Build();

            Ctx.Dashboard = root.AddComponent<DashboardUI>();
            Ctx.Dashboard.Build();
            Ctx.Analysis = root.AddComponent<AnalysisPanelUI>();
            Ctx.Analysis.Build();
            Ctx.Hud = root.AddComponent<HudUI>();
            Ctx.Hud.Build();

            Ctx.Story = root.AddComponent<StoryController>();
            root.AddComponent<ScreenshotTour>();
            Ctx.Story.GoTo(0, true);
        }

        static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            return cam;
        }

        static Canvas MakeCanvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void BuildCanvas(Transform parent)
        {
            // World-anchored labels and floating packets sit on a canvas that sorts below all panels.
            Ctx.WorldRect = MakeCanvas(parent, "WorldLabelCanvas", 5).GetComponent<RectTransform>();

            var canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Ctx.CanvasRect = canvasGo.GetComponent<RectTransform>();

            var safe = UIFactory.Empty(canvasGo.transform, "SafeArea");
            UIFactory.Fill(safe);
            safe.offsetMin = new Vector2(40f, 40f);
            safe.offsetMax = new Vector2(-40f, -40f);
            Ctx.SafeRoot = safe;
        }

        static void BuildEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
