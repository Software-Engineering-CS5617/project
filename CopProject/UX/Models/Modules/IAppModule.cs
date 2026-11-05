namespace SecureMeet.Shared.Modules;

// Every module has one class that implements this. Put your DLL in the UX/Modules folder
// and the app finds your class by itself (it needs a public constructor with no parameters).
public interface IAppModule
{
    // Called once at start. Subscribe to the session events here.
    void Initialize(IMeetingSession session);
}

// A module that has a screen (Video, Share, Whiteboard, Chat, Incidents).
public interface IMeetingModule : IAppModule
{
    ModuleSlot Slot { get; }

    // Your control (UserControl / Page). Create it once and return the same one every time.
    object View { get; }
}

// Controller team: implement this and the Start Meeting button uses it.
public interface IMeetingService : IAppModule
{
    (string Id, string Port) CreateMeeting();
}
