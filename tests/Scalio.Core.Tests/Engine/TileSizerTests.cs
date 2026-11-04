using Scalio.Core.Engine;

namespace Scalio.Core.Tests.Engine;

public sealed class TileSizerTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(4000, 3000)]
    public void Small_and_medium_images_get_automatic_tile(int width, int height)
    {
        Assert.Null(TileSizer.Suggest(width, height));
    }

    [Theory]
    [InlineData(4001, 2000)]
    [InlineData(2000, 4001)]
    [InlineData(6000, 6000)]
    public void Large_images_get_a_fixed_moderate_tile(int width, int height)
    {
        Assert.Equal(400, TileSizer.Suggest(width, height));
    }

    [Theory]
    [InlineData(8001, 2000)]
    [InlineData(9000, 9000)]
    public void Very_large_images_get_a_smaller_tile(int width, int height)
    {
        Assert.Equal(200, TileSizer.Suggest(width, height));
    }
}
