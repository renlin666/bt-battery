using BtBattery.Core;

namespace BtBattery.Core.Tests;

public class BatteryAndAddressTests
{
    [Theory]
    [InlineData(0, BatteryBand.Low)]
    [InlineData(20, BatteryBand.Low)]
    [InlineData(21, BatteryBand.Mid)]
    [InlineData(50, BatteryBand.Mid)]
    [InlineData(51, BatteryBand.Healthy)]
    [InlineData(100, BatteryBand.Healthy)]
    public void Bands_split_at_20_and_50(int percent, BatteryBand band)
    {
        Assert.Equal(band, BatteryMath.Band(percent));
        Assert.NotEqual(BadgePalette.For(band).BackgroundHex, BadgePalette.For(BatteryBand.Unknown).BackgroundHex);
    }

    [Fact]
    public void Unknown_band_is_separate()
    {
        Assert.Equal(BatteryBand.Unknown, BatteryMath.Band(null));
        Assert.Equal(BatteryBand.Unknown, BatteryMath.Band(255));
        Assert.Null(BatteryMath.NormalizeStoredLevel(255));
        Assert.Null(BatteryMath.NormalizeStoredLevel(-1));
    }

    [Theory]
    [InlineData(80, 100, 80)]
    [InlineData(80, null, 80)]
    [InlineData(2500, 5000, 50)]
    [InlineData(0, 100, 0)]
    [InlineData(200, 100, null)]
    public void Capacities_cover_percent_shaped_and_real_charges(int? remaining, int? full, int? expected)
    {
        Assert.Equal(expected, BatteryMath.FromCapacities(remaining, full));
    }

    [Fact]
    public void Multiple_batteries_use_the_lower_one()
    {
        var chosen = BatteryMath.Choose(new[]
        {
            BatteryReading.Known(80, BatterySource.DeviceProperty),
            BatteryReading.Known(42, BatterySource.GattBatteryService),
            BatteryReading.Unknown,
        });

        Assert.Equal(42, chosen.Percent);
        Assert.Equal(BatterySource.GattBatteryService, chosen.Source);
    }

    [Theory]
    [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("AABBCCDDEEFF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("AA-BB-CC-DD-EE-FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("000000000000", null)]
    [InlineData("nope", null)]
    public void Addresses_normalize(string raw, string? expected)
    {
        Assert.Equal(expected, BluetoothAddress.Normalize(raw));
    }

    [Fact]
    public void Ulong_address_roundtrips_with_the_classic_byte_order()
    {
        const ulong value = 0xAA_BB_CC_DD_EE_FF;
        var text = BluetoothAddress.Format(value);
        Assert.Equal("AA:BB:CC:DD:EE:FF", text);
        Assert.Equal(value, BluetoothAddress.ToUInt64(text));
    }

    [Fact]
    public void Instance_id_dev_prefix_is_an_address()
    {
        Assert.Equal(
            "AA:BB:CC:DD:EE:FF",
            BluetoothAddress.ExtractFromInstanceId(@"BTHLE\Dev_aabbccddeeff\7&pid"));
    }

    [Fact]
    public void Property_values_accept_the_boxes_winrt_uses()
    {
        Assert.Equal(72, PropertyValues.AsPercent((byte)72));
        Assert.Equal(72, PropertyValues.AsPercent(72));
        Assert.Equal(72, PropertyValues.AsPercent(new byte[] { 72 }));
        Assert.Null(PropertyValues.AsPercent((byte)255));
        Assert.True(PropertyValues.AsBool(1));
        Assert.Equal((ushort)0x03C2, PropertyValues.AsUInt16(0x03C2));
        Assert.Equal(new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), BluetoothProperty.BatteryLevelFormat);
        Assert.Equal(2u, BluetoothProperty.BatteryLevelPid);
    }
}
