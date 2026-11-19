using BtBattery.Core;

namespace BtBattery.Core.Tests;

public class DeviceClassifierTests
{
    [Theory]
    [InlineData(0x03C2, DeviceKind.Mouse, "鼠标")]
    [InlineData(0x03C1, DeviceKind.Keyboard, "键盘")]
    [InlineData(0x03C4, DeviceKind.Gamepad, "手柄")]
    [InlineData(0x03C3, DeviceKind.Gamepad, "摇杆")]
    [InlineData(0x03C9, DeviceKind.Mouse, "触控板")]
    [InlineData(0x0941, DeviceKind.Headset, "耳塞")]
    [InlineData(0x0942, DeviceKind.Headset, "耳机")]
    [InlineData(0x0943, DeviceKind.Headset, "头戴耳机")]
    public void Appearance_maps_supported_accessories(int appearance, DeviceKind kind, string label)
    {
        var result = DeviceClassifier.Classify(new ClassificationInput { Appearance = (ushort)appearance });
        Assert.Equal(kind, result.Kind);
        Assert.Equal(label, result.Label);
    }

    [Theory]
    [InlineData(0x0040)]
    [InlineData(0x0083)]
    [InlineData(0x0841)]
    public void Appearance_excludes_phones_computers_and_speakers(int appearance)
    {
        var result = DeviceClassifier.Classify(new ClassificationInput { Appearance = (ushort)appearance });
        Assert.Equal(DeviceKind.Excluded, result.Kind);
    }

    [Theory]
    [InlineData(0x580, DeviceKind.Mouse)]
    [InlineData(0x540, DeviceKind.Keyboard)]
    [InlineData(0x5C0, DeviceKind.Combo)]
    [InlineData(0x508, DeviceKind.Gamepad)]
    [InlineData(0x504, DeviceKind.Gamepad)]
    [InlineData(0x404, DeviceKind.Headset)]
    [InlineData(0x408, DeviceKind.Headset)]
    [InlineData(0x418, DeviceKind.Headset)]
    [InlineData(0x240404, DeviceKind.Headset)]
    public void Class_of_device_maps_accessories(uint cod, DeviceKind kind)
    {
        var result = DeviceClassifier.Classify(new ClassificationInput { ClassOfDevice = cod });
        Assert.Equal(kind, result.Kind);
    }

    [Theory]
    [InlineData(0x414)]
    [InlineData(0x200)]
    [InlineData(0x100)]
    public void Class_of_device_excludes_speakers_phones_and_computers(uint cod)
    {
        var result = DeviceClassifier.Classify(new ClassificationInput { ClassOfDevice = cod });
        Assert.Equal(DeviceKind.Excluded, result.Kind);
    }

    [Fact]
    public void Phone_class_beats_a_misleading_name()
    {
        var result = DeviceClassifier.Classify(new ClassificationInput
        {
            ClassOfDevice = 0x200,
            Name = "降噪耳机",
        });
        Assert.Equal(DeviceKind.Excluded, result.Kind);
    }

    [Fact]
    public void Headset_name_rescues_a_device_that_reports_itself_as_a_speaker()
    {
        var result = DeviceClassifier.Classify(new ClassificationInput
        {
            ClassOfDevice = 0x414,
            Name = "WH-1000XM5",
        });
        Assert.Equal(DeviceKind.Headset, result.Kind);
    }

    [Theory]
    [InlineData("MX Master 3S", DeviceKind.Mouse)]
    [InlineData("Keychron K3", DeviceKind.Keyboard)]
    [InlineData("MX Keys Mini", DeviceKind.Keyboard)]
    [InlineData("AirPods Pro", DeviceKind.Headset)]
    [InlineData("Galaxy Buds2", DeviceKind.Headset)]
    [InlineData("WH-1000XM5", DeviceKind.Headset)]
    [InlineData("Xbox Wireless Headset", DeviceKind.Headset)]
    [InlineData("Xbox Wireless Controller", DeviceKind.Gamepad)]
    [InlineData("DualSense", DeviceKind.Gamepad)]
    [InlineData("8BitDo Pro 2", DeviceKind.Gamepad)]
    [InlineData("罗技鼠标", DeviceKind.Mouse)]
    [InlineData("机械键盘", DeviceKind.Keyboard)]
    [InlineData("游戏手柄", DeviceKind.Gamepad)]
    [InlineData("iPhone", DeviceKind.Excluded)]
    [InlineData("Galaxy S24", DeviceKind.Excluded)]
    public void Name_hints_are_used_only_when_the_device_class_is_missing(string name, DeviceKind kind)
    {
        var result = DeviceClassifier.Classify(new ClassificationInput { Name = name });
        Assert.Equal(kind, result.Kind);
    }

    [Fact]
    public void Hardware_id_identifies_a_mouse()
    {
        var result = DeviceClassifier.Classify(new ClassificationInput
        {
            CompatibleIds = new[] { @"HID_DEVICE_SYSTEM_MOUSE" },
        });
        Assert.Equal(DeviceKind.Mouse, result.Kind);
    }

    [Fact]
    public void Empty_input_stays_unknown()
    {
        var result = DeviceClassifier.Classify(default);
        Assert.Equal(DeviceKind.Unknown, result.Kind);
        Assert.Equal(0, result.Confidence);
    }
}
