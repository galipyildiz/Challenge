namespace Challenge.Models
{
    public class Precipitation
    {
        public double Probability { get; set; }
        public bool Rain { get; set; }
        public bool Snow { get; set; }
        public bool Sleet { get; set; }
        public bool Hail { get; set; }
        public override string ToString()
        {
            var current = "";
            if (Rain) current += " Rain";
            if (Snow) current += " Snow";
            if (Sleet) current += " Sleet";
            if (Hail) current += " Hail";
            return $"Probability: {Probability:F2} - {current}";
        }
    }
}
