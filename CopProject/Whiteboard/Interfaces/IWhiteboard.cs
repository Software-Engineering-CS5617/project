using System;
using System.Collections.Generic;

namespace Whiteboard.Interfaces;

public interface IWhiteboard
{
    IDictionary<Guid, IItem> Items { get; }

    void AddItem(IItem item);
    void DeleteItem(Guid guid);
}