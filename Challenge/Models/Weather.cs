namespace Challenge.Models
{
    public class Weather
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public Precipitation Precipitation { get; set; } = new();
        public DateTime Time { get; set; }
        public Wind Wind { get; set; } = new();
        public override string ToString()
        {
            return $"Temprature: {Temperature:F2} - Humidity: {Humidity:F2} - Pressure: {Pressure:F2} - Precipitatin: {Precipitation} - Time: {Time} - Wind: {Wind} ";
        }
    }
}
