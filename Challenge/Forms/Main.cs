using Challenge.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;

namespace Challenge.Forms
{
    public partial class Main : Form
    {
        private readonly string ApiUrl = "http://localhost:5000";
        private readonly string tokenKey = "X-API-Key";
        private readonly string tokenValue = "API_KEY_1";
        private const int labelHeight = 23;
        private const int paddingValue = 10;
        private const int panelHeight = 450;
        private const int panelWidth = 300;

        public Main()
        {
            InitializeComponent();
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            Width = 1540;
            Height = 950;
            var rockets = await GetRocketsAsync();
            LoadRocketsToForm(rockets);
            ConnectRockets(rockets);
            InitializeTimer();
        }

        private void ConnectRockets(List<Rocket> rockets)
        {
            foreach (var rocket in rockets)
            {
                var thread = new Thread(() => ConnectToRocket(rocket));
                thread.Start();
            }
        }

        private void ConnectToRocket(Rocket rocket)
        {
            var tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect("localhost", rocket.Telemetry.Port);
                var networkStream = tcpClient.GetStream();
                var panel = FindPanel(rocket.Id);
                panel.BackColor = GetPanelColorByConnectionStatus(ConnectionStatus.Connected);
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var rocketId = Encoding.UTF8.GetString(buffer, 1, 10);
                        var altitude = ConvertByteArrayToFloatBigEndian(buffer, 13, 16);
                        var speed = ConvertByteArrayToFloatBigEndian(buffer, 17, 20);
                        var thrust = ConvertByteArrayToFloatBigEndian(buffer, 25, 28);
                        var temperature = ConvertByteArrayToFloatBigEndian(buffer, 29, 32);
                        Debug.WriteLine($"id: {rocketId} - altitude: {altitude:F2} - speed: {speed:F2} - thrust: {thrust:F2} - temp: {temperature:F2}");
                    }
                    else
                        throw new Exception();
                    Thread.Sleep(100);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{rocket.Id} disconnected: {ex.Message}");
                var panel = FindPanel(rocket.Id);
                panel.BackColor = GetPanelColorByConnectionStatus(ConnectionStatus.Disconnected);
                ConnectToRocket(rocket);
            }
        }

        public float ConvertByteArrayToFloatBigEndian(byte[] byteArray, int startIndex, int endIndex)
        {
            byte[] reversedBytes = new byte[sizeof(float)];
            for (int i = startIndex, j = 0; i <= endIndex; i++, j++)
            {
                reversedBytes[j] = byteArray[i];
            }
            Array.Reverse(reversedBytes);
            return BitConverter.ToSingle(reversedBytes, 0);
        }

        private Panel FindPanel(string id)
        {
            foreach (var control in Controls)
            {
                if (control is Panel)
                {
                    var panel = (Panel)control;
                    if (panel.Tag == id)
                    {
                        return panel;
                    }
                }
            }
            return null;
        }

        private void InitializeTimer()
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;//1sn
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await UpdateWeatherInformations();
        }

        private async Task UpdateWeatherInformations()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                    var response = await client.GetAsync(ApiUrl + "/weather");
                    if (response.IsSuccessStatusCode)
                    {
                        var weather = await response.Content.ReadFromJsonAsync<Weather>();
                        Text = weather.ToString();
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        private void LoadRocketsToForm(List<Rocket> rockets)
        {
            for (int i = 0; i < rockets.Count; i++)
            {
                var panel = new Panel
                {
                    Tag = rockets[i].Id,
                    Width = panelWidth,
                    Height = panelHeight,
                    BorderStyle = BorderStyle.Fixed3D,
                };
                panel.HorizontalScroll.Visible = true;

                panel.BackColor = GetPanelColorByConnectionStatus(rockets[i].Telemetry.ConnectionStatus);

                var idLabel = GetLabel("Id", rockets[i].Id, new Point(0, 0));
                var modelLabel = GetLabel("Model", rockets[i].Model, new Point(0, idLabel.Location.Y + labelHeight));
                var descriptionLabel = GetLabel("Description", rockets[i].Payload.Description, new Point(0, modelLabel.Location.Y + labelHeight));
                var weightLabel = GetLabel("Weight", rockets[i].Payload.Weight.ToString(), new Point(0, descriptionLabel.Location.Y + labelHeight));
                var statusLabel = GetLabel("Status", rockets[i].Status, new Point(0, weightLabel.Location.Y + labelHeight));
                var launchedLabel = GetLabel("Launched", rockets[i].Timestamps.Launched.ToString(), new Point(0, statusLabel.Location.Y + labelHeight));
                var deployedLabel = GetLabel("Deployed", rockets[i].Timestamps.Deployed.ToString(), new Point(0, launchedLabel.Location.Y + labelHeight));
                var failedLabel = GetLabel("Failed", rockets[i].Timestamps.Failed.ToString(), new Point(0, deployedLabel.Location.Y + labelHeight));
                var cancelledLabel = GetLabel("Canceled", rockets[i].Timestamps.Canceled.ToString(), new Point(0, failedLabel.Location.Y + labelHeight));
                var alitudeLabel = GetLabel("Altitude", rockets[i].Altitude.ToString(), new Point(0, cancelledLabel.Location.Y + labelHeight));
                var speedLabel = GetLabel("Speed", rockets[i].Speed.ToString(), new Point(0, alitudeLabel.Location.Y + labelHeight));
                var accelerationLabel = GetLabel("Acceleration", rockets[i].Acceleration.ToString(), new Point(0, speedLabel.Location.Y + labelHeight));
                var thrustLabel = GetLabel("Thrust", rockets[i].Thrust.ToString(), new Point(0, accelerationLabel.Location.Y + labelHeight));
                var temperatureLabel = GetLabel("Temperature", rockets[i].Temperature.ToString(), new Point(0, thrustLabel.Location.Y + labelHeight));

                panel.Controls.Add(idLabel);
                panel.Controls.Add(modelLabel);
                panel.Controls.Add(descriptionLabel);
                panel.Controls.Add(weightLabel);
                panel.Controls.Add(statusLabel);
                panel.Controls.Add(launchedLabel);
                panel.Controls.Add(deployedLabel);
                panel.Controls.Add(failedLabel);
                panel.Controls.Add(cancelledLabel);
                panel.Controls.Add(alitudeLabel);
                panel.Controls.Add(speedLabel);
                panel.Controls.Add(accelerationLabel);
                panel.Controls.Add(thrustLabel);
                panel.Controls.Add(temperatureLabel);

                if (i > 4)
                    panel.Location = new Point(paddingValue + (i - 5) * panel.Width, paddingValue + panel.Height);
                else
                    panel.Location = new Point(paddingValue + i * panel.Width, paddingValue);

                Controls.Add(panel);
            }
        }

        private Color GetPanelColorByConnectionStatus(ConnectionStatus connectionStatus)
        {
            switch (connectionStatus)
            {
                default:
                case ConnectionStatus.None:
                    return Color.Gray;
                case ConnectionStatus.Connected:
                    return Color.Green;
                case ConnectionStatus.Disconnected:
                    return Color.Red;
            }
        }

        private Label GetLabel(string tag, string text, Point location)
        {
            var label = new Label();
            label.Tag = tag;
            label.AutoSize = true;
            label.Location = location;
            label.Text = tag + ": " + text;
            return label;
        }

        private async Task<List<Rocket>> GetRocketsAsync()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                    var response = await client.GetAsync(ApiUrl + "/rockets");
                    if (response.IsSuccessStatusCode)
                    {
                        var rockets = await response.Content.ReadFromJsonAsync<List<Rocket>>();
                        if (rockets != null)
                            return rockets;
                    }
                    else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        return await GetRocketsAsync();

                    return [];
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine(ex.Message);
                    return await GetRocketsAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }
}
