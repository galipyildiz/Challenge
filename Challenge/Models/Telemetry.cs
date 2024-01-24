namespace Challenge.Models
{
    public class Telemetry
    {
        public string Host { get; set; } = "";
        public short Port { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.None;
    }
    public enum ConnectionStatus
    {
        None,
        Connected,
        Disconnected,
    }
}
