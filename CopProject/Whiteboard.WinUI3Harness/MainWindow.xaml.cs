// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.UI.Xaml;
using Networking;
using Networking.TCPCommunication;
using Whiteboard.Client;
using Whiteboard.Server;
using Whiteboard.ViewModels;
using Whiteboard.Views;

namespace Whiteboard.WinUI3Harness;

public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Port to be used for UI testing
    /// </summary>
    private const int ServerPort = 40000;

    /// <summary>
    /// Using loopback to test locally
    /// </summary>
    private static readonly IPEndPoint s_serverEndPoint = new(IPAddress.Loopback, ServerPort);

    /// <summary>
    /// Server communicator
    /// </summary>
    private static ICommunicator? s_serverCommunicator;

    /// <summary>
    /// Whiteboard server
    /// </summary>
    private static IWhiteboardServer? s_server;

    /// <summary>
    /// Client communmicator
    /// </summary>
    private readonly ICommunicator _clientCommunicator;

    public MainWindow()
    {
        InitializeComponent();

        bool isServerInstance = false;

        // Check if chosen port is available
        try
        {
            var testListener = new TcpListener(IPAddress.Loopback, ServerPort);
            testListener.Start();
            testListener.Stop();
            isServerInstance = true;
        }
        catch (SocketException)
        {
            isServerInstance = false;
        }

        // If port is free, start the server
        if (isServerInstance)
        {
            s_serverCommunicator = new CommunicatorServer(s_serverEndPoint);
            s_server = WhiteboardFactory.GetServerWhiteboard(s_serverCommunicator);

            // Waiting for a small time for server to start, otherwise the client would start sending messages before server is ready
            Thread.Sleep(100);
        }

        _clientCommunicator = new CommunicatorClient(s_serverEndPoint);
        string username = $"User_{Environment.ProcessId}";

        IWhiteboardClient client = WhiteboardFactory.CreateClientWhiteboard(
            _clientCommunicator,
            s_serverEndPoint,
            username
        );

        Title = $"Whiteboard Harness - {username} {(isServerInstance ? "[Host Server]" : "[Client]")}";

        var viewModel = new WhiteBoardViewModel(client);
        var view = new WhiteBoardView(viewModel);

        this.Content = view;

        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        (_clientCommunicator as IDisposable)?.Dispose();
        (s_serverCommunicator as IDisposable)?.Dispose();
    }
}
