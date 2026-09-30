using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using PowerHub.Protocol.Proto;

namespace PowerHub.Protocol;

/// <summary>
/// Обгортка кадрів для protobuf-моделей (Delta 3, Delta Pro 3).
/// Кожне MQTT-повідомлення — HeaderMessage { repeated Header header = 1 }; кожен Header містить
/// cmd_func / cmd_id і блок pdata, який може бути закодований XOR з молодшим байтом seq.
/// Заголовки Delta 3 і Delta Pro 3 однакові на рівні формату, тож класи Delta 3 обслуговують обидві.
/// </summary>
public static class ProtoCodec
{
    public sealed record Frame(int CmdFunc, int CmdId, byte[] Pdata);

    private const int SrcApp = 32;

    public static List<Frame> Frames(byte[] payload)
    {
        var raw = MaybeBase64(payload);
        Delta3HeaderMessage message;
        try
        {
            message = Delta3HeaderMessage.Parser.ParseFrom(raw);
        }
        catch (InvalidProtocolBufferException)
        {
            return new List<Frame>();
        }

        var frames = new List<Frame>();
        foreach (var h in message.Header)
        {
            if (!h.HasPdata || h.Pdata.IsEmpty)
                continue;

            var data = h.Pdata.ToByteArray();
            if (h.EncType == 1 && h.Src != SrcApp)
            {
                var key = (byte)(h.Seq & 0xFF);
                for (var i = 0; i < data.Length; i++)
                    data[i] ^= key;
            }
            frames.Add(new Frame(h.CmdFunc, h.CmdId, data));
        }
        return frames;
    }

    private static byte[] MaybeBase64(byte[] payload)
    {
        if (payload.Length == 0 || payload.Length % 4 != 0)
            return payload;

        foreach (var b in payload)
        {
            var c = (char)b;
            var ok = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/' or '=';
            if (!ok) return payload;
        }

        try
        {
            return Convert.FromBase64String(Encoding.ASCII.GetString(payload));
        }
        catch (FormatException)
        {
            return payload;
        }
    }

    /// <summary>
    /// Декодувати pdata відповідним парсером і розгорнути вкладені повідомлення з роздільником `_`
    /// </summary>
    public static DeviceParams DecodeFlat(MessageParser parser, byte[] pdata)
    {
        try
        {
            return Flatten(parser.ParseFrom(pdata));
        }
        catch (InvalidProtocolBufferException)
        {
            return new DeviceParams();
        }
    }

    /// <summary>
    /// Розгорнути лише задані поля повідомлення в плоский словник
    /// </summary>
    public static DeviceParams Flatten(IMessage message, string prefix = "", DeviceParams? output = null)
    {
        output ??= new DeviceParams();

        foreach (var fd in message.Descriptor.Fields.InFieldNumberOrder())
        {
            if (fd.IsMap)
                continue;

            var key = prefix.Length == 0 ? fd.Name : $"{prefix}_{fd.Name}";
            var value = fd.Accessor.GetValue(message);

            if (fd.IsRepeated)
            {
                if (value is IList list && list.Count > 0)
                    output[key] = list.Cast<object?>().Select(v => ConvertValue(fd, v)).ToList();
            }
            else if (fd.FieldType == FieldType.Message)
            {
                if (value is IMessage nested)
                    Flatten(nested, key, output);
            }
            else if (IsSet(fd, message, value))
            {
                output[key] = ConvertValue(fd, value);
            }
        }
        return output;
    }

    /// <summary>
    /// Як allFields у Java: поле з presence — якщо задане, без presence — якщо не дорівнює значенню за замовчуванням
    /// </summary>
    private static bool IsSet(FieldDescriptor fd, IMessage message, object? value)
    {
        if (fd.HasPresence)
            return fd.Accessor.HasValue(message);

        return value switch
        {
            null => false,
            string s => s.Length > 0,
            ByteString bs => !bs.IsEmpty,
            bool b => b,
            IConvertible c => c.ToDouble(System.Globalization.CultureInfo.InvariantCulture) != 0,
            _ => true
        };
    }

    private static object? ConvertValue(FieldDescriptor fd, object? value) => fd.FieldType switch
    {
        FieldType.Float => value is float f ? (double)f : value,
        FieldType.Enum => value == null ? null : Convert.ToInt32(value),
        FieldType.Bytes => null,
        FieldType.Message => value is IMessage m ? Flatten(m) : null,
        _ => value
    };

    /// <summary>
    /// Створити pdata SetCommand з одним заданим скалярним полем
    /// </summary>
    public static byte[] SetCommandPdata<T>(string field, int value) where T : IMessage<T>, new()
    {
        var message = new T();
        var fd = message.Descriptor.FindFieldByName(field)
                 ?? throw new ArgumentException($"Unknown field {field}");
        fd.Accessor.SetValue(message, ToFieldType(fd, value));
        return message.ToByteArray();
    }

    private static object ToFieldType(FieldDescriptor fd, int value) => fd.FieldType switch
    {
        FieldType.UInt32 or FieldType.Fixed32 => (uint)value,
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => (long)value,
        FieldType.UInt64 or FieldType.Fixed64 => (ulong)value,
        FieldType.Bool => value != 0,
        _ => value
    };

    public static byte[] EncodeVarint(int value)
    {
        var x = (ulong)(uint)value;
        var output = new List<byte>();
        while (true)
        {
            var b = (byte)(x & 0x7F);
            x >>= 7;
            if (x != 0)
            {
                output.Add((byte)(b | 0x80));
            }
            else
            {
                output.Add(b);
                break;
            }
        }
        return output.ToArray();
    }

    /// <summary>
    /// Обгорнути SetCommand у пакет, який надсилає офіційний застосунок:
    /// src=32 (застосунок) → dest=2, cmd_func=254 cmd_id=17, version=19
    /// </summary>
    public static OutgoingMessage SetPacket(string sn, byte[] pdata, int? dataLen = null)
    {
        var header = new Delta3Header
        {
            Src = SrcApp,
            Dest = 2,
            DSrc = 1,
            DDest = 1,
            CmdFunc = 254,
            CmdId = 17,
            NeedAck = 1,
            Seq = JsonMessages.SeqNumber(),
            ProductId = 1,
            Version = 19,
            PayloadVer = 1,
            DeviceSn = sn,
            DataLen = dataLen ?? pdata.Length,
            Pdata = ByteString.CopyFrom(pdata)
        };
        var packet = new Delta3SendHeaderMsg();
        packet.Msg.Add(header);
        return new OutgoingMessage(packet.ToByteArray());
    }

    /// <summary>
    /// Порожній заголовок від застосунку до застосунку: станція відповідає повним станом у get_reply
    /// </summary>
    public static OutgoingMessage SnapshotRequest()
    {
        var header = new Delta3Header
        {
            Src = SrcApp,
            Dest = SrcApp,
            Seq = JsonMessages.SeqNumber(),
            From = "Android"
        };
        var packet = new Delta3SendHeaderMsg();
        packet.Msg.Add(header);
        return new OutgoingMessage(packet.ToByteArray());
    }

    /// <summary>
    /// Стан виходів напряму не передається: flow_info == 14 означає увімкнено, 4 — вимкнено
    /// </summary>
    public static void DeriveFlag(DeviceParams output, IEnumerable<string> flowKeys, string target)
    {
        var values = flowKeys
            .Select(k => output.GetInt(k))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        if (values.Count == 0)
            return;

        if (values.Any(v => v == 14))
            output[target] = 1;
        else if (values.All(v => v == 4))
            output[target] = 0;
    }
}
