using System.Collections.Generic;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Network.Dtos;
using Whiteboard.Network.Mappers;
using Whiteboard.Server;
using Whiteboard.Test.Mocks.Networking;
using Xunit;

namespace Whiteboard.Test.ClientServer;

public class ClientServerRestoreTests
{
    [Fact]
    public void ClientRestoreState_UpdatesServerAndOtherClients()
    {
        var factory = new MockCommunicatorFactory();
       
        IWhiteboardServer server = WhiteboardFactory.GetServerWhiteboard(factory.GetServerCommunicator());
        IWhiteboardClient client1 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User1");
        IWhiteboardClient client2 = WhiteboardFactory.CreateClientWhiteboard(factory.CreateClientCommunicator(), factory.ServerEndPoint, "User2");

        var oldItem1 = new FreehandItem();
        oldItem1.Points.Add(new Point2D(1, 2));
        client1.AddItem(oldItem1);

        var oldItem2 = new FreehandItem();
        oldItem2.Points.Add(new Point2D(3, 4));
        client2.AddItem(oldItem2);

        Assert.True(client1.CanUndo);
        Assert.True(client2.CanUndo);

        var restoredItem = new FreehandItem();
        restoredItem.Points.Add(new Point2D(10, 20));

        client1.RestoreSnapshot(new WhiteboardDto(new List<ItemDto> { ItemMapper.ToDto(restoredItem) }));

        Assert.Single(server.Canvas.Items);
        Assert.Contains(restoredItem.Id, server.Canvas.Items.Keys);
        Assert.Contains(restoredItem.Id, client1.Canvas.Items.Keys);
        Assert.Contains(restoredItem.Id, client2.Canvas.Items.Keys);


        Assert.DoesNotContain(oldItem1.Id, server.Canvas.Items.Keys);
        Assert.DoesNotContain(oldItem2.Id, server.Canvas.Items.Keys);

        Assert.DoesNotContain(oldItem1.Id, client1.Canvas.Items.Keys);
        Assert.DoesNotContain(oldItem2.Id, client1.Canvas.Items.Keys);
        Assert.DoesNotContain(oldItem1.Id, client2.Canvas.Items.Keys);
        Assert.DoesNotContain(oldItem2.Id, client2.Canvas.Items.Keys);

        Assert.False(client1.CanUndo);
        Assert.False(client1.CanRedo);
        Assert.False(client2.CanUndo);
        Assert.False(client2.CanRedo);
    }
}