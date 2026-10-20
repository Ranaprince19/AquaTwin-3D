namespace LeakWatch
{
    public enum SensorType { Pressure, Flow, Vibration, Location }

    public struct StationReading
    {
        public string tag;
        public float pressure;
        public float flow;
        public float vibration;
    }
}
