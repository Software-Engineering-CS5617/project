using System.Net;
using System.Text;
using Controller.Models;
using ScreenShare;
using Screenshare.Tests.Fakes;
using Utils;

namespace Screenshare.Tests;

public class ControllerMeetingSessionTests
{
    // UserKey is gmail#ip#port; with this ip and port the key is the gmail's length + 14
    private const string Ip = "10.0.0.1";
    private const int Port = 5000;
    private const int KeyOverhead = 14;

    private static ControllerUser User(string username = "Sumit", string gmail = "sumit@gmail.com", bool isHost = false)
    {
        return new ControllerUser(username, gmail, null, Ip, Port, isHost);
    }

    private static ControllerUser UserWithKeyBytes(int keyBytes)
    {
        return User(gmail: new string('a', keyBytes - KeyOverhead));
    }

    // ToParticipant

    [Fact]
    public void ToParticipant_NullUser_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ControllerMeetingSession.ToParticipant(null!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToParticipant_MapsIdNameAndHost(bool isHost)
    {
        ControllerUser user = User(isHost: isHost);

        MeetingParticipant participant = ControllerMeetingSession.ToParticipant(user);

        Assert.Equal("sumit@gmail.com#10.0.0.1#5000", participant.Id);
        Assert.Equal("Sumit", participant.DisplayName);
        Assert.Equal(isHost, participant.IsHost);
    }

    // ParticipantId

    [Fact]
    public void ParticipantId_ShortKey_IsTheUserKey()
    {
        ControllerUser user = User();

        Assert.Equal(user.UserKey, ControllerMeetingSession.ParticipantId(user));
    }

    [Fact]
    public void ParticipantId_KeyAtLimit_IsTheUserKey()
    {
        ControllerUser user = UserWithKeyBytes(ScreenShareCodec.MaxIdBytes);

        Assert.Equal(user.UserKey, ControllerMeetingSession.ParticipantId(user));
    }

    [Fact]
    public void ParticipantId_KeyOverLimit_IsHashed()
    {
        ControllerUser user = UserWithKeyBytes(ScreenShareCodec.MaxIdBytes + 1);

        string id = ControllerMeetingSession.ParticipantId(user);

        // "user-" and a SHA-256 written as 64 lowercase hex digits
        Assert.Matches("^user-[0-9a-f]{64}$", id);
        Assert.True(Encoding.UTF8.GetByteCount(id) <= ScreenShareCodec.MaxIdBytes);
    }

    [Fact]
    public void ParticipantId_SameLongUser_GetsSameId()
    {
        // the host and the client each work out the id, so they must agree
        string first = ControllerMeetingSession.ParticipantId(UserWithKeyBytes(200));
        string second = ControllerMeetingSession.ParticipantId(UserWithKeyBytes(200));

        Assert.Equal(first, second);
    }

    [Fact]
    public void ParticipantId_DifferentLongUsers_GetDifferentIds()
    {
        string first = ControllerMeetingSession.ParticipantId(UserWithKeyBytes(200));
        string second = ControllerMeetingSession.ParticipantId(UserWithKeyBytes(201));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ParticipantId_CountsBytesNotCharacters()
    {
        // 'é' is 2 bytes in UTF-8: 60 of them are 120 bytes but only 60 characters
        ControllerUser user = User(gmail: new string('é', 60));

        string id = ControllerMeetingSession.ParticipantId(user);

        Assert.StartsWith("user-", id);
    }

    // DisplayName

    [Fact]
    public void DisplayName_UsesTrimmedUsername()
    {
        Assert.Equal("Sumit", ControllerMeetingSession.DisplayName(User(username: "  Sumit  ")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DisplayName_BlankUsername_UsesTrimmedGmail(string username)
    {
        ControllerUser user = User(username: username, gmail: " sumit@gmail.com ");

        Assert.Equal("sumit@gmail.com", ControllerMeetingSession.DisplayName(user));
    }

    [Fact]
    public void DisplayName_NoUsernameOrGmail_UsesFallback()
    {
        ControllerUser user = User(username: " ", gmail: "");

        Assert.Equal("Participant", ControllerMeetingSession.DisplayName(user));
    }

    [Fact]
    public void DisplayName_AtLimit_IsKept()
    {
        string name = new('n', ControllerMeetingSession.MaxDisplayNameLength);

        Assert.Equal(name, ControllerMeetingSession.DisplayName(User(username: name)));
    }

    [Fact]
    public void DisplayName_OverLimit_IsCut()
    {
        string name = new string('n', ControllerMeetingSession.MaxDisplayNameLength) + "xyz";

        string shown = ControllerMeetingSession.DisplayName(User(username: name));

        Assert.Equal(new string('n', ControllerMeetingSession.MaxDisplayNameLength), shown);
    }

    // ParseHostEndpoint

    [Fact]
    public void ParseHostEndpoint_Ipv4()
    {
        IPEndPoint endpoint = ControllerMeetingSession.ParseHostEndpoint("10.0.0.5", "5000");

        Assert.Equal(new IPEndPoint(IPAddress.Parse("10.0.0.5"), 5000), endpoint);
    }

    [Fact]
    public void ParseHostEndpoint_Ipv6()
    {
        IPEndPoint endpoint = ControllerMeetingSession.ParseHostEndpoint("::1", "5000");

        Assert.Equal(new IPEndPoint(IPAddress.IPv6Loopback, 5000), endpoint);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LocalHost")]
    [InlineData("  localhost  ")]
    public void ParseHostEndpoint_Localhost_IsLoopback(string address)
    {
        IPEndPoint endpoint = ControllerMeetingSession.ParseHostEndpoint(address, "5000");

        Assert.Equal(IPAddress.Loopback, endpoint.Address);
    }

    [Fact]
    public void ParseHostEndpoint_TrimsAddressAndPort()
    {
        IPEndPoint endpoint = ControllerMeetingSession.ParseHostEndpoint(" 10.0.0.5 ", " 6000 ");

        Assert.Equal(new IPEndPoint(IPAddress.Parse("10.0.0.5"), 6000), endpoint);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("65535")]
    public void ParseHostEndpoint_PortAtEdges_IsAccepted(string port)
    {
        IPEndPoint endpoint = ControllerMeetingSession.ParseHostEndpoint("10.0.0.5", port);

        Assert.Equal(int.Parse(port), endpoint.Port);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("10.0.0.256")]
    [InlineData("example.com")] // names are not looked up, only localhost is known
    public void ParseHostEndpoint_BadAddress_Throws(string address)
    {
        Assert.Throws<FormatException>(() => ControllerMeetingSession.ParseHostEndpoint(address, "5000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("50.5")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("65536")]
    public void ParseHostEndpoint_BadPort_Throws(string port)
    {
        Assert.Throws<FormatException>(() => ControllerMeetingSession.ParseHostEndpoint("10.0.0.5", port));
    }

    // the session's lifecycle, with a fake Controller and the UI's real MeetingSession

    private readonly FakeNetwork _network = new();
    private readonly MeetingSession _meetingSession = new();
    private readonly MeetingInfo _joinedMeeting = new("10.0.0.5", "6000", IsHost: false);

    private FakeControllerService Controller(bool isHost = false)
    {
        return new FakeControllerService
        {
            CurrentUser = User(isHost: isHost),
            Communicator = isHost ? _network.Server : _network.AddClient(),
        };
    }

    private ControllerMeetingSession NewSession(FakeControllerService controller)
    {
        return new ControllerMeetingSession(controller, _meetingSession, _joinedMeeting);
    }

    private void StartAndEndMeeting()
    {
        _meetingSession.StartMeeting(_joinedMeeting);
        _meetingSession.EndMeeting();
    }

    [Fact]
    public void Constructor_NullArgument_Throws()
    {
        FakeControllerService controller = Controller();

        Assert.Throws<ArgumentNullException>(() => new ControllerMeetingSession(null!, _meetingSession, _joinedMeeting));
        Assert.Throws<ArgumentNullException>(() => new ControllerMeetingSession(controller, null!, _joinedMeeting));
        Assert.Throws<ArgumentNullException>(() => new ControllerMeetingSession(controller, _meetingSession, null!));
    }

    [Fact]
    public void Constructor_NoCurrentUser_Throws()
    {
        FakeControllerService controller = Controller();
        controller.CurrentUser = null;

        Assert.Throws<InvalidOperationException>(() => NewSession(controller));
    }

    [Fact]
    public void Constructor_NoConnection_Throws()
    {
        FakeControllerService controller = Controller();
        controller.Communicator = null;

        Assert.Throws<InvalidOperationException>(() => NewSession(controller));
    }

    [Fact]
    public void Constructor_ClientWithBadMeetingAddress_Throws()
    {
        MeetingInfo meeting = new("not an address", "6000", IsHost: false);

        Assert.Throws<FormatException>(() => new ControllerMeetingSession(Controller(), _meetingSession, meeting));
    }

    [Fact]
    public void Constructor_Client_UsesMeetingAddressAsHost()
    {
        FakeControllerService controller = Controller();

        using ControllerMeetingSession session = NewSession(controller);

        Assert.Equal(new IPEndPoint(IPAddress.Parse("10.0.0.5"), 6000), session.HostEndpoint);
        Assert.Same(controller.Communicator, session.Communicator);
        Assert.Equal(ControllerMeetingSession.ToParticipant(controller.CurrentUser!), session.LocalUser);
    }

    [Fact]
    public void Constructor_Host_UsesOwnListeningEndpoint()
    {
        FakeControllerService controller = Controller(isHost: true);
        MeetingInfo meeting = new("not used by the host", "not used", IsHost: true);

        using ControllerMeetingSession session = new(controller, _meetingSession, meeting);

        Assert.Equal(_network.Server.Endpoint, session.HostEndpoint);
        Assert.True(session.LocalUser.IsHost);
    }

    [Fact]
    public void Constructor_RegistersWithController()
    {
        FakeControllerService controller = Controller();

        using ControllerMeetingSession session = NewSession(controller);

        Assert.Same(session, Assert.Single(controller.Handlers));
    }

    [Fact]
    public void OnUserJoined_DoesNothing()
    {
        using ControllerMeetingSession session = NewSession(Controller());
        int events = 0;
        session.ParticipantLeft += _ => events++;
        session.MeetingEnded += () => events++;

        session.OnUserJoined(User(username: "Aryan"));

        Assert.Equal(0, events);
    }

    [Fact]
    public void OnUserLeft_RaisesParticipantLeft()
    {
        using ControllerMeetingSession session = NewSession(Controller());
        List<MeetingParticipant> left = new();
        session.ParticipantLeft += left.Add;
        ControllerUser leaving = User(username: "Aryan", gmail: "aryan@gmail.com");

        session.OnUserLeft(leaving);

        Assert.Equal(ControllerMeetingSession.ToParticipant(leaving), Assert.Single(left));
    }

    [Fact]
    public void OnUserLeft_NullUser_IsIgnored()
    {
        using ControllerMeetingSession session = NewSession(Controller());
        int events = 0;
        session.ParticipantLeft += _ => events++;

        session.OnUserLeft(null!);

        Assert.Equal(0, events);
    }

    [Fact]
    public void OnUserLeft_AfterEnd_IsIgnored()
    {
        ControllerMeetingSession session = NewSession(Controller());
        int events = 0;
        session.ParticipantLeft += _ => events++;
        session.Dispose();

        session.OnUserLeft(User(username: "Aryan"));

        Assert.Equal(0, events);
    }

    [Fact]
    public void OnUserLeft_NoSubscribers_DoesNotThrow()
    {
        using ControllerMeetingSession session = NewSession(Controller());

        Assert.Null(Record.Exception(() => session.OnUserLeft(User(username: "Aryan"))));
    }

    [Fact]
    public void MeetingEnding_NoSubscribers_StillUnregisters()
    {
        FakeControllerService controller = Controller();
        using ControllerMeetingSession session = NewSession(controller);

        StartAndEndMeeting();

        Assert.Empty(controller.Handlers);
    }

    [Fact]
    public void OnUserLeft_HandlerThrows_DoesNotReachController()
    {
        using ControllerMeetingSession session = NewSession(Controller());
        session.ParticipantLeft += _ => throw new InvalidOperationException("handler failed");

        Exception? thrown = Record.Exception(() => session.OnUserLeft(User(username: "Aryan")));

        Assert.Null(thrown);
    }

    [Fact]
    public void MeetingEnding_RaisesMeetingEndedOnceAndUnregisters()
    {
        FakeControllerService controller = Controller();
        using ControllerMeetingSession session = NewSession(controller);
        int ended = 0;
        session.MeetingEnded += () => ended++;

        StartAndEndMeeting();
        StartAndEndMeeting();

        Assert.Equal(1, ended);
        Assert.Empty(controller.Handlers);
    }

    [Fact]
    public void Dispose_UnregistersWithoutRaisingMeetingEnded()
    {
        FakeControllerService controller = Controller();
        ControllerMeetingSession session = NewSession(controller);
        int ended = 0;
        session.MeetingEnded += () => ended++;

        session.Dispose();
        StartAndEndMeeting();

        Assert.Equal(0, ended);
        Assert.Empty(controller.Handlers);
    }

    [Fact]
    public void Dispose_Twice_UnregistersOnce()
    {
        FakeControllerService controller = Controller();
        ControllerMeetingSession session = NewSession(controller);

        session.Dispose();
        session.Dispose();

        Assert.Equal(1, controller.UnregisterCalls);
    }

    [Fact]
    public void MeetingEnding_ControllerAlreadyDisposed_StillRaisesMeetingEnded()
    {
        FakeControllerService controller = Controller();
        controller.FailUnregister = true;
        using ControllerMeetingSession session = NewSession(controller);
        int ended = 0;
        session.MeetingEnded += () => ended++;

        StartAndEndMeeting();

        Assert.Equal(1, ended);
    }
}
