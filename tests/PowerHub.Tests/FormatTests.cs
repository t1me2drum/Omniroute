using PowerHub.Models;
using PowerHub.Protocol;
using Xunit;

namespace PowerHub.Tests;

public class FormatTests
{
    [Fact]
    public void Watts_SwitchesToKilowatts()
    {
        Assert.Equal("—", Format.Watts(null));
        Assert.Equal("850 Вт", Format.Watts(850));
        Assert.Equal("1,20 кВт", Format.Watts(1200));
    }

    [Fact]
    public void Minutes_ShowsHoursWhenNeeded()
    {
        Assert.Equal("45 хв", Format.Minutes(45));
        Assert.Equal("2 год 5 хв", Format.Minutes(125));
    }

    [Fact]
    public void FlowText_ShowsOnlyMatchingEstimate()
    {
        Assert.Equal("⏱ до повного заряду 1 год 10 хв", Format.FlowText(new BatteryFlow.Charging(70)));
        Assert.Equal("🔋 розряджається", Format.FlowText(new BatteryFlow.Discharging(null)));
        Assert.Equal("🔌 навантаження 16 Вт · батарея в спокої", Format.FlowText(new BatteryFlow.Idle(16)));
        Assert.Equal("без навантаження", Format.FlowText(new BatteryFlow.Idle(0)));
    }

    [Fact]
    public void GridShort_DescribesWeakGrid()
    {
        Assert.Equal("⚠ слабка мережа 140 В", Format.GridShort(GridStatus.Weak, 140));
        Assert.Equal("мережа ✓ 228 В", Format.GridShort(GridStatus.Ok, 228));
        Assert.Equal("без мережі", Format.GridShort(GridStatus.None, null));
    }
}
