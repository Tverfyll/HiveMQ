using HiveMQtt.Client;
using MqttToPlantScada.Settings;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace MqttToPlantScada
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HiveMQClient _mqttClient;
        private readonly MqttSettings _mqttSettings;

        public Worker(
            ILogger<Worker> logger,
            MqttSettings mqttSettings)
        {
            _logger = logger;
            _mqttSettings = mqttSettings;
            _mqttClient = new HiveMQClient();
            _mqttClient.OnMessageReceived += HandleMessageReceivedAsync;
            _mqttClient.AfterDisconnect += DisconnectedAsync;
            _mqttClient.AfterConnect += ConnectedAsync;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await ConnectAndSubscribe();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public void ConnectedAsync(object? sender, AfterConnectEventArgs e)
        {
            _logger.LogInformation("Connected to Mqtt Broker");
        }

        public async void DisconnectedAsync(object? sender, AfterDisconnectEventArgs e)
        {
            _logger.LogInformation($"Disconnected from Mqtt Broker");

            await ReconnectAndSubscribe();

        }

        public async void HandleMessageReceivedAsync(object? sender, OnMessageReceivedEventArgs e)
        {
            string topic = e.PublishMessage.Topic ?? "";
            string payload = e.PublishMessage.PayloadAsString;

            _logger.LogInformation("Received message on topic '{topic}': {payload}", topic, payload);
            await Task.CompletedTask;
        }

        public async Task ConnectAndSubscribe()
        {
            try
            {
                var options = new HiveMQClientOptionsBuilder()
                           .WithBroker(_mqttSettings.Server)
                           .WithPort(_mqttSettings.Port)
                           .WithUseTls(true)
                           .WithUserName(_mqttSettings.Username)
                           .WithPassword(_mqttSettings.Password)
                           .WithClientId($"{_mqttSettings.ClientId}-{Guid.NewGuid}")
                           .WithCleanStart(true)
                           .WithKeepAlive(10)
                           .Build();

                _mqttClient.Options = options;

                await _mqttClient.ConnectAsync().ConfigureAwait(false);

                var builder = new SubscribeOptionsBuilder();
                builder.WithSubscription(_mqttSettings.SubscribeTopic, QualityOfService.AtLeastOnceDelivery);
                var subscribeOptions = builder.Build();
                await _mqttClient.SubscribeAsync(subscribeOptions);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ReconnectAndSubscribe()
        {
            _logger.LogInformation("Reconnecting");

            //Code for reconnecting on connection failure.

            await Task.CompletedTask;
            return;
        }
    }
}