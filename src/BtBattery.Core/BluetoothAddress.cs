namespace BtBattery.Core;

/// <summary>蓝牙地址统一成 AA:BB:CC:DD:EE:FF（高字节在前，与 Windows 设备管理器一致）。</summary>
public static class BluetoothAddress
{
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var hex = new char[12];
        var count = 0;
        foreach (var ch in raw)
        {
            if (IsHex(ch))
            {
                if (count >= 12)
                    return null;
                hex[count++] = char.ToUpperInvariant(ch);
            }
        }

        if (count != 12)
            return null;

        if (new string(hex) == "000000000000")
            return null;

        return string.Create(17, hex, static (span, digits) =>
        {
            for (var i = 0; i < 6; i++)
            {
                span[i * 3] = digits[i * 2];
                span[i * 3 + 1] = digits[i * 2 + 1];
                if (i < 5)
                    span[i * 3 + 2] = ':';
            }
        });
    }

    /// <summary>
    /// WinRT 的 BluetoothAddress 以及经典 API 的 BLUETOOTH_ADDRESS.ullLong
    /// 都是低 48 位，最低字节对应地址最后一组（显示时的最右边）。
    /// </summary>
    public static string? Format(ulong address)
    {
        if (address == 0 || address > 0xFFFF_FFFF_FFFF)
            return null;

        Span<char> digits = stackalloc char[12];
        for (var i = 0; i < 6; i++)
        {
            var shift = (5 - i) * 8;
            var b = (int)((address >> shift) & 0xFF);
            digits[i * 2] = ToHex(b >> 4);
            digits[i * 2 + 1] = ToHex(b & 0xF);
        }

        return Normalize(new string(digits));
    }

    public static ulong? ToUInt64(string? normalized)
    {
        var text = Normalize(normalized);
        if (text is null)
            return null;

        ulong value = 0;
        foreach (var ch in text)
        {
            if (ch == ':')
                continue;
            value = (value << 4) | HexValue(ch);
        }

        return value;
    }

    /// <summary>从实例 ID 里抽出 Dev_AABBCCDDEEFF 这种地址。</summary>
    public static string? ExtractFromInstanceId(string? instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        var marker = instanceId.IndexOf("Dev_", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
        {
            var parsed = Normalize(SliceHex(instanceId, marker + 4));
            if (parsed is not null)
                return parsed;
        }

        return null;
    }

    private static string SliceHex(string text, int start)
    {
        var end = start;
        while (end < text.Length && IsHex(text[end]) && end - start < 12)
            end++;
        return text[start..end];
    }

    private static bool IsHex(char ch) =>
        ch is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';

    private static char ToHex(int value) =>
        (char)(value < 10 ? '0' + value : 'A' + (value - 10));

    private static ulong HexValue(char ch) => ch switch
    {
        >= '0' and <= '9' => (ulong)(ch - '0'),
        >= 'A' and <= 'F' => (ulong)(ch - 'A' + 10),
        >= 'a' and <= 'f' => (ulong)(ch - 'a' + 10),
        _ => 0,
    };
}
