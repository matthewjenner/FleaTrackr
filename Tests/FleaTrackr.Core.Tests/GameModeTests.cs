using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class GameModeTests
{
    [Theory]
    [InlineData(GameMode.Pvp, "regular")]
    [InlineData(GameMode.Pve, "pve")]
    public void ToApiValue_maps_to_the_tarkov_dev_wire_value(GameMode mode, string expected) =>
        mode.ToApiValue().Should().Be(expected);
}
