namespace Utils;

public interface IUserNotificationHandler
{
    void OnUserJoined(ControllerUser user);

    void OnUserLeft(ControllerUser user);
}
