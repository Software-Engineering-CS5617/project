// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Whiteboard.Selection;

public class SelectionState
{
    public HashSet<Guid> SelectedItemIds { get; } = [];
    public Guid? PrimarySelectedId { get; private set; }

    public void Clear()
    {
        SelectedItemIds.Clear();
        PrimarySelectedId = null;
    }

    public void SelectSingle(Guid id)
    {
        SelectedItemIds.Clear();
        SelectedItemIds.Add(id);
        PrimarySelectedId = id;
    }

    public void Toggle(Guid id)
    {
        if (SelectedItemIds.Contains(id))
        {
            SelectedItemIds.Remove(id);
            if (PrimarySelectedId == id)
            {
                PrimarySelectedId = null;
            }
            return;
        }

        SelectedItemIds.Add(id);
        PrimarySelectedId = id;
    }

    public bool IsSelected(Guid id)
    {
        return SelectedItemIds.Contains(id);
    }
}
