﻿using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitfinexClientSharp.Example
{
    class Program
    {
        private static object consoleLock = new object();
        private const int receiveChunkSize = 256;
        private const bool verbose = true;
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(30000);
        private static UTF8Encoding encoder = new UTF8Encoding();

        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            Connect("wss://api.bitfinex.com/ws/2").Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static async Task Connect(string uri)
        {
            ClientWebSocket webSocket = null;

            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(false);
                await Task.WhenAll(Receive(webSocket), Send(webSocket)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
                Console.WriteLine();

                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("WebSocket closed.");
                    Console.ResetColor();
                }
            }
        }
        

        private static async Task Send(ClientWebSocket webSocket)
        {
            var buffer = encoder.GetBytes("{\r\n  \"event\": \"subscribe\",\r\n  \"channel\": \"ticker\",\r\n  \"symbol\": \"BTCUSD\"\r\n}");
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);

            while (webSocket.State == WebSocketState.Open)
            {
                LogStatus(false, buffer);
                await Task.Delay(delay);
            }
        }

        private static async Task Receive(ClientWebSocket webSocket)
        {
            var buffer = new byte[receiveChunkSize];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    LogStatus(true, buffer);
                }
            }
        }

        private static void LogStatus(bool receiving, byte[] buffer)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = receiving ? ConsoleColor.Green : ConsoleColor.Gray;

                if (verbose)
                    Console.WriteLine(encoder.GetString(buffer));

                Console.ResetColor();
            }
        }
    }
}
