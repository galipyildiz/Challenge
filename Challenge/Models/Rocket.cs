
namespace Challenge.Models
{
    public class Rocket
    {
        public string Id { get; set; } = "";
        public string Model { get; set; } = "";
        public double Mass { get; set; }
        public Payload Payload { get; set; } = new();
        public Telemetry Telemetry { get; set; } = new();
        public string Status { get; set; } = ""; //maybe can be enum, not necessary
        public Timestamps Timestamps { get; set; } = new();
        public float Altitude { get; set; }
        public float Speed { get; set; }
        public float Acceleration { get; set; }
        public float Thrust { get; set; }
        public float Temperature { get; set; }
    }
}
