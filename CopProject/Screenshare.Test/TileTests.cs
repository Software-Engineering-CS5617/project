// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ScreenShare;

namespace Screenshare.Tests;

public class TileTests
{
    [Fact]
    public void Constructor_ValidArguments_StoresProperties()
    {
        byte[] data = { 1, 2, 3, 4 };

        Tile tile = new Tile(10, 20, 64, 32, 123UL, data);

        Assert.Equal(10, tile.X);
        Assert.Equal(20, tile.Y);
        Assert.Equal(64, tile.Width);
        Assert.Equal(32, tile.Height);
        Assert.Equal(123UL, tile.Hash);
        Assert.Equal(data, tile.Data);
    }

    [Fact]
    public void Constructor_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Tile(0, 0, 64, 64, 0UL, null!));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Constructor_NegativeCoordinates_ThrowsArgumentOutOfRangeException(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(x, y, 64, 64, 0UL, new byte[] { 1 }));
    }

    [Theory]
    [InlineData(0, 64)]
    [InlineData(64, 0)]
    [InlineData(0, -1)]
    [InlineData(-1, 64)]
    public void Constructor_InvalidDimensions_ThrowsArgumentOutOfRangeException(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tile(0, 0, width, height, 0UL, new byte[] { 1 }));
    }

    [Fact]
    public void Constructor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Tile(0, 0, 64, 64, 0UL, Array.Empty<byte>()));
    }

    [Fact]
    public void Constructor_ClonesInputData()
    {
        byte[] data = { 1, 2, 3 };
        var tile = new Tile(0, 0, 64, 64, 0UL, data);

        data[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, tile.Data);
    }

    [Fact]
    public void Data_ReturnsCopyOfStoredData()
    {
        var tile = new Tile(0, 0, 64, 64, 0UL, new byte[] { 1, 2, 3 });

        byte[] returnedData = tile.Data;
        returnedData[0] = 99;

        Assert.Equal(new byte[] { 1, 2, 3 }, tile.Data);
    }
}
