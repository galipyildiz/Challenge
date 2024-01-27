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
        private const int controlHeight = 23;
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
                if (panel != null)
                    panel.BackColor = GetPanelColorByConnectionStatus(ConnectionStatus.Connected);

                while (true)
                {

                    byte[] buffer = new byte[36];
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        var packetStartByte = buffer[0];//130

                        var rocketId = Encoding.UTF8.GetString(buffer, 1, 10);

                        var packetNumber = buffer[11];//0-255
                        var packetSize = buffer[12];//expected 36 but arrives 20

                        var altitude = ConvertByteArrayToFloatBigEndian(buffer, 13, 16);
                        var speed = ConvertByteArrayToFloatBigEndian(buffer, 17, 20);
                        var thrust = ConvertByteArrayToFloatBigEndian(buffer, 25, 28);
                        var temperature = ConvertByteArrayToFloatBigEndian(buffer, 29, 32);

                        var bypassValue = ConvertByteArrayToShortBigEndian(buffer, 33, 34);
                        var delimiter = buffer[35];//128

                        var panelFromReceivedId = FindPanel(rocketId);
                        if (panelFromReceivedId != null)
                            UpdateTelemetryValues(panelFromReceivedId, altitude, speed, thrust, temperature);
                        Thread.Sleep(100);
                    }
                    else
                        throw new Exception();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{rocket.Id} disconnected: {ex.Message}");
                tcpClient.Close();
                var panel = FindPanel(rocket.Id);
                if (panel != null)
                {
                    panel.BackColor = GetPanelColorByConnectionStatus(ConnectionStatus.Disconnected);
                    ConnectToRocket(rocket);
                }
            }
        }

        private void UpdateTelemetryValues(Panel panel, float altitude, float speed, float thrust, float temperature)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    foreach (Control control in panel.Controls)
                    {
                        switch (control.Tag)
                        {
                            case "Altitude":
                                control.Text = "Altitude: " + altitude.ToString("F2");
                                break;
                            case "Speed":
                                control.Text = "Speed: " + speed.ToString("F2");
                                break;
                            case "Thrust":
                                control.Text = "Thrust: " + thrust.ToString("F2");
                                break;
                            case "Temperature":
                                control.Text = "Temperature: " + temperature.ToString("F2");
                                break;
                            default:
                                break;
                        }

                    }
                }));
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
        public float ConvertByteArrayToShortBigEndian(byte[] byteArray, int startIndex, int endIndex)
        {
            byte[] reversedBytes = new byte[sizeof(short)];
            for (int i = startIndex, j = 0; i <= endIndex; i++, j++)
            {
                reversedBytes[j] = byteArray[i];
            }
            Array.Reverse(reversedBytes);
            return BitConverter.ToInt16(reversedBytes, 0);
        }

        private Panel? FindPanel(string id)
        {
            foreach (Control rocketPanel in Controls)
            {
                if (rocketPanel.Tag != null)
                {
                    if (rocketPanel.Tag.Equals(id))
                    {
                        return (Panel)rocketPanel;
                    }
                }
            }
            return null;
        }

        private async Task UpdateWeatherInformations()
        {
            using var client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                var response = await client.GetAsync(ApiUrl + "/weather");
                if (response.IsSuccessStatusCode)
                {
                    var weather = await response.Content.ReadFromJsonAsync<Weather>();
                    if (weather != null)
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
                    AutoScroll = true,
                };

                panel.BackColor = GetPanelColorByConnectionStatus(rockets[i].Telemetry.ConnectionStatus);

                var idLabel = GetLabel("Id", rockets[i].Id, new Point(0, 0));
                var modelLabel = GetLabel("Model", rockets[i].Model, new Point(0, idLabel.Location.Y + controlHeight));
                var descriptionLabel = GetLabel("Description", rockets[i].Payload.Description, new Point(0, modelLabel.Location.Y + controlHeight));
                var weightLabel = GetLabel("Weight", rockets[i].Payload.Weight.ToString(), new Point(0, descriptionLabel.Location.Y + controlHeight));
                var statusLabel = GetLabel("Status", rockets[i].Status, new Point(0, weightLabel.Location.Y + controlHeight));
                var launchedLabel = GetLabel("Launched", rockets[i].Timestamps.Launched.ToString(), new Point(0, statusLabel.Location.Y + controlHeight));
                var deployedLabel = GetLabel("Deployed", rockets[i].Timestamps.Deployed.ToString(), new Point(0, launchedLabel.Location.Y + controlHeight));
                var failedLabel = GetLabel("Failed", rockets[i].Timestamps.Failed.ToString(), new Point(0, deployedLabel.Location.Y + controlHeight));
                var cancelledLabel = GetLabel("Canceled", rockets[i].Timestamps.Canceled.ToString(), new Point(0, failedLabel.Location.Y + controlHeight));
                var alitudeLabel = GetLabel("Altitude", rockets[i].Altitude.ToString("F2"), new Point(0, cancelledLabel.Location.Y + controlHeight));
                var speedLabel = GetLabel("Speed", rockets[i].Speed.ToString("F2"), new Point(0, alitudeLabel.Location.Y + controlHeight));
                var accelerationLabel = GetLabel("Acceleration", rockets[i].Acceleration.ToString("F2"), new Point(0, speedLabel.Location.Y + controlHeight));
                var thrustLabel = GetLabel("Thrust", rockets[i].Thrust.ToString("F2"), new Point(0, accelerationLabel.Location.Y + controlHeight));
                var temperatureLabel = GetLabel("Temperature", rockets[i].Temperature.ToString("F2"), new Point(0, thrustLabel.Location.Y + controlHeight));

                var launchButton = new Button()
                {
                    Text = "Launch",
                    Tag = rockets[i].Id,
                    Location = new Point(0, temperatureLabel.Location.Y + controlHeight),
                    BackColor = Color.White
                };
                launchButton.Click += LaunchButton_Click;

                var deployButton = new Button()
                {
                    Text = "Deploy",
                    Tag = rockets[i].Id,
                    Location = new Point(launchButton.Width + 10, temperatureLabel.Location.Y + controlHeight),
                    BackColor = Color.White
                };
                deployButton.Click += DeployButton_Click;

                var cancelButton = new Button()
                {
                    Text = "Cancel",
                    Tag = rockets[i].Id,
                    Location = new Point(deployButton.Location.X + deployButton.Width + 10, temperatureLabel.Location.Y + controlHeight),
                    BackColor = Color.White
                };
                cancelButton.Click += CancelButton_Click;

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
                panel.Controls.Add(launchButton);
                panel.Controls.Add(deployButton);
                panel.Controls.Add(cancelButton);

                if (i > 4)
                    panel.Location = new Point(paddingValue + (i - 5) * panel.Width, paddingValue + panel.Height);
                else
                    panel.Location = new Point(paddingValue + i * panel.Width, paddingValue);

                Controls.Add(panel);
            }
        }

        private async void CancelButton_Click(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                var button = (Button)sender;
                var rocketId = button.Tag;
                using var client = new HttpClient();
                try
                {
                    button.Visible = false;
                    client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                    var response = await client.DeleteAsync(ApiUrl + "/rocket/" + rocketId + "/status" + "/launched");
                    if (response.IsSuccessStatusCode)
                    {
                        var rocket = await response.Content.ReadFromJsonAsync<Rocket>();
                        if (rocket != null)
                        {
                            var panelFromReceivedId = FindPanel(rocket.Id);
                            if (panelFromReceivedId != null)
                                UpdateRocketValues(panelFromReceivedId, rocket);
                        }
                    }
                    if (response.StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("Already deployed");
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
                finally
                {
                    button.Visible = true;
                }
            }
        }

        private async void DeployButton_Click(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                var button = (Button)sender;
                var rocketId = button.Tag;
                using var client = new HttpClient();
                try
                {
                    button.Visible = false;
                    client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                    var response = await client.PutAsync(ApiUrl + "/rocket/" + rocketId + "/status" + "/deployed", null);
                    if (response.IsSuccessStatusCode)
                    {
                        var rocket = await response.Content.ReadFromJsonAsync<Rocket>();
                        if (rocket != null)
                        {
                            var panelFromReceivedId = FindPanel(rocket.Id);
                            if (panelFromReceivedId != null)
                                UpdateRocketValues(panelFromReceivedId, rocket);
                        }
                    }
                    if (response.StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("Already deployed");
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
                finally
                {
                    button.Visible = true;
                }
            }
        }

        private async void LaunchButton_Click(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                var button = (Button)sender;
                var rocketId = button.Tag;
                using var client = new HttpClient();
                try
                {
                    button.Visible = false;
                    client.DefaultRequestHeaders.Add(tokenKey, tokenValue);
                    var response = await client.PutAsync(ApiUrl + "/rocket/" + rocketId + "/status" + "/launched", null);
                    if (response.IsSuccessStatusCode)
                    {
                        var rocket = await response.Content.ReadFromJsonAsync<Rocket>();
                        if (rocket != null)
                        {
                            var panelFromReceivedId = FindPanel(rocket.Id);
                            if (panelFromReceivedId != null)
                                UpdateRocketValues(panelFromReceivedId, rocket);
                        }
                    }
                    if (response.StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("Already launched");
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
                finally
                {
                    button.Visible = true;
                }
            }
        }

        private void UpdateRocketValues(Panel panel, Rocket rocket)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    foreach (Control control in panel.Controls)
                    {
                        switch (control.Tag)
                        {
                            case "Altitude":
                                control.Text = "Altitude: " + rocket.Altitude.ToString("F2");
                                break;
                            case "Speed":
                                control.Text = "Speed: " + rocket.Speed.ToString("F2");
                                break;
                            case "Thrust":
                                control.Text = "Thrust: " + rocket.Thrust.ToString("F2");
                                break;
                            case "Temperature":
                                control.Text = "Temperature: " + rocket.Temperature.ToString("F2");
                                break;
                            case "Weight":
                                control.Text = "Weight: " + rocket.Payload.Weight.ToString("F2");
                                break;
                            case "Status":
                                control.Text = "Status: " + rocket.Status;
                                break;
                            case "Launched":
                                control.Text = "Launched: " + rocket.Timestamps.Launched.ToString();
                                break;
                            case "Deployed":
                                control.Text = "Deployed: " + rocket.Timestamps.Deployed.ToString();
                                break;
                            case "Canceled":
                                control.Text = "Canceled: " + rocket.Timestamps.Canceled.ToString();
                                break;
                            case "Failed":
                                control.Text = "Failed: " + rocket.Timestamps.Failed.ToString();
                                break;
                            case "Acceleration":
                                control.Text = "Acceleration: " + rocket.Acceleration.ToString();
                                break;
                            default:
                                break;
                        }

                    }
                }));
            }
            else
            {
                foreach (Control control in panel.Controls)
                {
                    switch (control.Tag)
                    {
                        case "Altitude":
                            control.Text = "Altitude: " + rocket.Altitude.ToString("F2");
                            break;
                        case "Speed":
                            control.Text = "Speed: " + rocket.Speed.ToString("F2");
                            break;
                        case "Thrust":
                            control.Text = "Thrust: " + rocket.Thrust.ToString("F2");
                            break;
                        case "Temperature":
                            control.Text = "Temperature: " + rocket.Temperature.ToString("F2");
                            break;
                        case "Weight":
                            control.Text = "Weight: " + rocket.Payload.Weight.ToString("F2");
                            break;
                        case "Status":
                            control.Text = "Status: " + rocket.Status;
                            break;
                        case "Launched":
                            control.Text = "Launched: " + rocket.Timestamps.Launched.ToString();
                            break;
                        case "Deployed":
                            control.Text = "Deployed: " + rocket.Timestamps.Deployed.ToString();
                            break;
                        case "Canceled":
                            control.Text = "Canceled: " + rocket.Timestamps.Canceled.ToString();
                            break;
                        case "Failed":
                            control.Text = "Failed: " + rocket.Timestamps.Failed.ToString();
                            break;
                        case "Acceleration":
                            control.Text = "Acceleration: " + rocket.Acceleration.ToString();
                            break;
                        default:
                            break;
                    }

                }
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
            using var client = new HttpClient();
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
