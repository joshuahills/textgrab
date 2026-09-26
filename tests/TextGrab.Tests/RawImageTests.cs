using TextGrab.Imaging;
using Xunit;

namespace TextGrab.Tests;

public class RawImageTests
{
    [Fact]
    public void Crop_CopiesExpectedPixels()
    {
        var img = RawImage.Allocate(4, 4);
        for (var i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (byte)i;

        var c = img.Crop(new PixelRect(1, 1, 2, 2));

        Assert.Equal(2, c.Width);
        Assert.Equal(2, c.Height);
        // pixel (1,1) in source starts at byte 1*16 + 1*4 = 20
        Assert.Equal(20, c.Pixels[0]);
        Assert.Equal(21, c.Pixels[1]);
    }

    [Fact]
    public void Crop_ClampsToImageBounds()
    {
        var img = RawImage.Allocate(10, 10);
        var c = img.Crop(new PixelRect(8, 8, 10, 10));
        Assert.Equal(2, c.Width);
        Assert.Equal(2, c.Height);
    }

    [Fact]
    public void Resize_PreservesSolidColour()
    {
        var img = RawImage.Allocate(3, 3);
        Array.Fill(img.Pixels, (byte)200);
        var r = img.Resize(9, 9);
        Assert.All(r.Pixels, b => Assert.Equal(200, b));
    }

    [Fact]
    public void ToPackedBytes_RemovesStridePadding()
    {
        var padded = new byte[(2 * 4 + 8) * 2];
        var img = new RawImage(padded, 2, 2, 16);
        Assert.Equal(16, img.ToPackedBytes().Length);
    }
}
