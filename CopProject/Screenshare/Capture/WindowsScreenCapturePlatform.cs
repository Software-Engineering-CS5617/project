// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Runtime.InteropServices;

namespace ScreenShare;

internal sealed class WindowsScreenCapturePlatform : IScreenCapturePlatform
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public int GetScreenWidth()
    {
        return GetSystemMetrics(SM_CXSCREEN);
    }

    public int GetScreenHeight()
    {
        return GetSystemMetrics(SM_CYSCREEN);
    }

    public IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext)
    {
        return NativeSetThreadDpiAwarenessContext(dpiContext);
    }

    public void CopyFromScreen(Bitmap destination, Rectangle bounds)
    {
        using Graphics graphics = Graphics.FromImage(destination);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetThreadDpiAwarenessContext")]
    private static extern IntPtr NativeSetThreadDpiAwarenessContext(
        IntPtr dpiContext);
}
