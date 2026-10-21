using System.Collections.ObjectModel;
using Whiteboard.Interfaces;

namespace Whiteboard;

public class WhiteboardCanvas
{
    public ObservableCollection<IItem> Items { get; } = [];

    public void AddItem(IItem item)
    {
        Items.Add(item);
    }

    public void RemoveItem(IItem item)
    {
        Items.Remove(item);
    }
}
