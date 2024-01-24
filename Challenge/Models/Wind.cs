namespace Challenge.Models
{
    public class Wind
    {
        public string Direction { get; set; } = "";
        public double Angle { get; set; }
        public double Speed { get; set; }
        public override string ToString()
        {
            return $"Direction: {Direction} - Angle: {Angle:F2} - Speed: {Speed:F2}";
        }
    }
}
