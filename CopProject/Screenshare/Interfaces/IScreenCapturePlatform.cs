// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;

namespace ScreenShare;

internal interface IScreenCapturePlatform
{
    int GetScreenWidth();

    int GetScreenHeight();

    IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    void CopyFromScreen(Bitmap destination, Rectangle bounds);
}
