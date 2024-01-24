
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
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Acceleration { get; set; }
        public double Thrust { get; set; }
        public double Temperature { get; set; }
    }
}
