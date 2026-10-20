using UnityEngine;

namespace LeakWatch
{
    public struct VoidRect
    {
        public float x0, x1, y0, y1;
        public VoidRect(float x0, float x1, float y0, float y1)
        {
            this.x0 = x0; this.x1 = x1; this.y0 = y0; this.y1 = y1;
        }
    }

    /// Fixed world layout (left to right): Pump, Station A, UB, Station B, TP2.
    /// Camera looks from +Z at the cut face of the ground block (front face at z = 0).
    public static class NetworkLayout
    {
        public const float GroundMinX = -40f;
        public const float GroundMaxX = 58f;
        public const float GrassBottomY = -0.5f;
        public const float TopsoilBottomY = -3f;
        public const float GroundBottomY = -9f;
        public const float FrontSlabDepth = 2f;
        public const float GroundBackZ = -14f;

        public const float PipeY = -4.2f;
        public const float PipeR = 1.1f;

        public const float PumpX = -34f;
        public const float StationAX = -13.4f;
        public const float UbX = 6.1f;
        public const float StationBX = 25.6f;
        public const float Tp2X = 45.6f;

        public const float UbWidth = 12f;
        public const float Tp2Width = 12f;
        public const float PumpWidth = 7f;

        public const float BuildingFrontZ = -0.6f;
        public const float BasementWallZ = 2.2f;
        public const float LeakX = UbX - 1f;
        public const float PipeEndX = Tp2X + 1.5f;
        public const float ChamberHalf = 12f;
        public static readonly float[] SensorOffsets = { -10.8f, -3.6f, 3.6f, 10.8f };

        public static readonly VoidRect ChamberA = new VoidRect(StationAX - ChamberHalf, StationAX + ChamberHalf, -6.4f, -0.8f);
        public static readonly VoidRect ChamberB = new VoidRect(StationBX - ChamberHalf, StationBX + ChamberHalf, -6.4f, -0.8f);
        public static readonly VoidRect Basement = new VoidRect(UbX - UbWidth / 2f, UbX + UbWidth / 2f, -6.6f, -0.9f);
        public static readonly VoidRect ValvePit = new VoidRect(-31.6f, -28.4f, -3.7f, -0.2f);

        public static Vector3[] PipeControlPoints()
        {
            return new[]
            {
                new Vector3(-33.5f, -0.3f, 0f),
                new Vector3(-33.5f, -1.0f, 0f),
                new Vector3(-33.2f, -1.7f, 0f),
                new Vector3(-32.5f, -2.1f, 0f),
                new Vector3(-31.7f, -2.2f, 0f),
                new Vector3(-30f, -2.2f, 0f),
                new Vector3(-28.5f, -2.2f, 0f),
                new Vector3(-27.8f, -2.6f, 0f),
                new Vector3(-27f, -3.5f, 0f),
                new Vector3(-26.2f, PipeY, 0f),
                new Vector3(-24f, PipeY, 0f),
                new Vector3(-10f, PipeY, 0f),
                new Vector3(4f, PipeY, 0f),
                new Vector3(18f, PipeY, 0f),
                new Vector3(32f, PipeY, 0f),
                new Vector3(PipeEndX - 6.5f, PipeY, 0f),
                new Vector3(PipeEndX - 4.5f, PipeY, 0f),
                new Vector3(PipeEndX - 2.7f, -4.1f, 0f),
                new Vector3(PipeEndX - 1.3f, -3.6f, 0f),
                new Vector3(PipeEndX - 0.3f, -2.7f, 0f),
                new Vector3(PipeEndX, -1.6f, 0f),
                new Vector3(PipeEndX, -0.3f, 0f)
            };
        }

    }
}
