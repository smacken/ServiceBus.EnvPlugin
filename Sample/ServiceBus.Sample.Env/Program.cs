﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using ServiceBus.EnvPlugin;
using Environment = System.Environment;

namespace ServiceBus.Sample.Env
{
    class Program
    {
        private const string ServiceBusConnectionString = "Insert Bus ConnectionString";
        private const string QueueName = "plugin";
        private static IQueueClient queueClient;
        
        private const string TopicName = "plugintopic";
        private static ITopicClient topicClient;

        private static Microsoft.Extensions.Hosting.IHostingEnvironment _environment;

        private static void Main(string[] args)
        {
            _environment = new HostingEnvironment
            {
                EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                ApplicationName = AppDomain.CurrentDomain.FriendlyName,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
                ContentRootFileProvider = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory)
            };

            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{_environment.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var myConnString = configuration.GetConnectionString("SQLConn");

            MainClientAsync().GetAwaiter().GetResult();
            MainAsync().GetAwaiter().GetResult();

            Console.ReadLine();
        }

        private static async Task MainAsync()
        {
            // queue
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            queueClient.RegisterPlugin(new EnvPlugin.EnvPlugin(_environment.EnvironmentName));
            await SendMessagesAsync(10);

            // topic
            topicClient = new TopicClient(ServiceBusConnectionString, TopicName);
            topicClient.RegisterPlugin(new EnvPlugin.EnvPlugin(_environment.EnvironmentName));
            await SendTopicMessagesAsync(10);
        }

        public static async Task MainClientAsync()
        {
            var conn = new ServiceBusConnectionStringBuilder(ServiceBusConnectionString);
            var subClient = new SubscriptionClient(conn, TopicName);
            await subClient.AddRuleAsync(new RuleDescription("EnvRule", new SqlFilter("Env LIKE 'Development'")));
        }

        private static async Task SendMessagesAsync(int numberOfMessagesToSend = 1)
        {
            var client = new ManagementClient(ServiceBusConnectionString);

            if (!await client.QueueExistsAsync(QueueName))
                await client.CreateQueueAsync(QueueName);

            try
            {
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue.
                    var messageBody = $"Message {i}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Write the body of the message to the console.
                    Console.WriteLine($"Sending message: {messageBody}");

                    // Send the message to the queue.
                    await queueClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }

        private static async Task SendTopicMessagesAsync(int numberOfMessagesToSend = 1)
        {
            var client = new ManagementClient(ServiceBusConnectionString);

            if (!await client.TopicExistsAsync(TopicName))
                await client.CreateTopicAsync(TopicName);

            try
            {
                for (var i = 0; i < numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue.
                    var messageBody = $"Message {i}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Write the body of the message to the console.
                    Console.WriteLine($"Sending message: {messageBody}");

                    // Send the message to the queue.
                    await topicClient.SendAsync(message);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}
