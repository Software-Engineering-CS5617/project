/// Owner   : Raagam Hitesh Parmar, Sangam
/// Roll No : 112301024, 112201006
/// LICENSE : MIT

using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Network.Mappers;
using Whiteboard.Server;
using Whiteboard.Services;
using Whiteboard.Test.Mocks.Networking;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.ClientServer;

/// <summary>
/// Tests shared canvas updates and history synchronization between users.
/// </summary>
public class ClientServerIntegrationTests
{
    [Fact]
    public void ClientAddItem_PropagatesToServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item);

        Assert.True(server.Canvas.Items.ContainsKey(item.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item.Id));
    }

    [Fact]
    public void ClientDeleteItem_RemovesFromServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        client1.AddItem(item);

        Assert.True(server.Canvas.Items.ContainsKey(item.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item.Id));

        client1.DeleteItem(item.Id);

        Assert.False(server.Canvas.Items.ContainsKey(item.Id));
        Assert.False(client1.Canvas.Items.ContainsKey(item.Id));
        Assert.False(client2.Canvas.Items.ContainsKey(item.Id));
    }

    [Fact]
    public void ClientEditItem_UpdatesServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        IItem item = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        client1.AddItem(item);

        item.Orientation = (item.Orientation + 45) % 360;
        client1.EditItem(item);

        Assert.Equal(item.Orientation, server.Canvas.Items[item.Id].Orientation);
        Assert.Equal(item.Orientation, client1.Canvas.Items[item.Id].Orientation);
        Assert.Equal(item.Orientation, client2.Canvas.Items[item.Id].Orientation);
    }

    [Fact]
    public void LateJoiningClient_ReceivesExistingStateOnInitialization()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");

        IItem item1 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item2 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item3 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item1);
        client1.AddItem(item2);
        client1.AddItem(item3);

        Assert.Equal(3, server.Canvas.Items.Count);

        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        Assert.Equal(3, client2.Canvas.Items.Count);
        Assert.True(client2.Canvas.Items.ContainsKey(item1.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item2.Id));
        Assert.True(client2.Canvas.Items.ContainsKey(item3.Id));
    }

    [Fact]
    public void MultipleClients_ConcurrentAddsAndDeletes_RemainConsistent()
    {
        var factory = new MockCommunicatorFactory();

        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");
        IWhiteboardClient client3 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User3");

        IItem item1 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item2 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());
        IItem item3 = ItemMapper.FromDto(RandomItemDtoGenerator.Generate());

        client1.AddItem(item1);
        client2.AddItem(item2);
        client3.AddItem(item3);

        client1.DeleteItem(item1.Id);

        Guid[] expectedIds = [item2.Id, item3.Id];

        Assert.Equal(expectedIds.OrderBy(id => id), server.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client1.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client2.Canvas.Items.Keys.OrderBy(id => id));
        Assert.Equal(expectedIds.OrderBy(id => id), client3.Canvas.Items.Keys.OrderBy(id => id));
    }

    /// <summary>
    /// Checks that host undo and redo update every canvas without undoing client work.
    /// </summary>
    [Fact]
    public void HostUndoRedoUpdatesBothUsers()
    {
        var factory = new MockCommunicatorFactory();
        var server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        var host = WhiteboardFactory.CreateClientWhiteboard(
            factory.CreateClientCommunicator(), factory.ServerEndPoint, "Host");
        var client = WhiteboardFactory.CreateClientWhiteboard(
            factory.CreateClientCommunicator(), factory.ServerEndPoint, "Client");

        var hostItem = new Square();
        var clientItem = new Square();
        host.AddItem(hostItem);
        client.AddItem(clientItem);

        host.Undo();

        foreach (var board in new[] { server.Canvas, host.Canvas, client.Canvas })
        {
            Assert.False(board.Items.ContainsKey(hostItem.Id));
            Assert.True(board.Items.ContainsKey(clientItem.Id));
        }

        Assert.False(host.CanUndo);
        Assert.True(host.CanRedo);
        Assert.True(client.CanUndo);
        Assert.False(client.CanRedo);

        host.Redo();

        foreach (var board in new[] { server.Canvas, host.Canvas, client.Canvas })
        {
            Assert.True(board.Items.ContainsKey(hostItem.Id));
            Assert.True(board.Items.ContainsKey(clientItem.Id));
        }

        Assert.True(host.CanUndo);
        Assert.False(host.CanRedo);
    }

    /// <summary>
    /// Checks that a client's edit updates both the canvas and personal histories.
    /// </summary>
    [Fact]
    public void ClientEditUpdatesHostHistory()
    {
        var factory = new MockCommunicatorFactory();
        var server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        var host = WhiteboardFactory.CreateClientWhiteboard(
            factory.CreateClientCommunicator(), factory.ServerEndPoint, "Host");
        var client = WhiteboardFactory.CreateClientWhiteboard(
            factory.CreateClientCommunicator(), factory.ServerEndPoint, "Client");

        var item = new Square { SideLegth = 40 };
        host.AddItem(item);

        var edited = (Square)ItemSnapshot.Copy(client.Canvas.Items[item.Id]);
        edited.SideLegth = 80;
        client.EditItem(edited);

        foreach (var board in new[] { server.Canvas, host.Canvas, client.Canvas })
        {
            Assert.Equal(80, Assert.IsType<Square>(board.Items[item.Id]).SideLegth);
            Assert.Equal("Host", board.Items[item.Id].CreatedBy);
        }

        Assert.False(host.CanUndo);
        Assert.True(client.CanUndo);

        // The client's undo restores the shape but not the host's old history.
        client.Undo();

        foreach (var board in new[] { server.Canvas, host.Canvas, client.Canvas })
        {
            Assert.Equal(40, Assert.IsType<Square>(board.Items[item.Id]).SideLegth);
        }

        Assert.False(host.CanUndo);
        Assert.False(client.CanUndo);
        Assert.True(client.CanRedo);
    }
}
