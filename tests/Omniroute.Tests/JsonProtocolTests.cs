using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Omniroute.Protocol;
using Xunit;

namespace Omniroute.Tests;

/// <summary>
/// Перенесено з Android-версії (JsonProtocolTest.kt)
/// </summary>
public class JsonProtocolTests
{
    private static byte[] Bytes(string json) => Encoding.UTF8.GetBytes(json);

    private static JsonElement Json(OutgoingMessage message) => JsonDocument.Parse(message.Payload).RootElement;

    [Fact]
    public void DataTopicParams_AreFlatKeys()
    {
        var p = JsonMessages.Parse(Bytes("""{"params":{"pd.soc":85,"bms_emsStatus.maxChargeSoc":100}}"""));
        Assert.Equal(85, p["pd.soc"]);
        Assert.Equal(100, p["bms_emsStatus.maxChargeSoc"]);
    }

    [Fact]
    public void LatestQuotasReply_UsesQuotaMapOnlyWhenOnline()
    {
        var online = """{"operateType":"latestQuotas","data":{"online":1,"quotaMap":{"pd.wattsOutSum":42}}}""";
        Assert.Equal(42, JsonMessages.Parse(Bytes(online))["pd.wattsOutSum"]);
        var offline = """{"operateType":"latestQuotas","data":{"online":0,"quotaMap":{"pd.wattsOutSum":42}}}""";
        Assert.Empty(JsonMessages.Parse(Bytes(offline)));
    }

    [Fact]
    public void NestedObjects_AreFlattenedWithDots_GarbageIgnored()
    {
        var p = JsonMessages.Parse(Bytes("""{"params":{"inv":{"inputWatts":10,"arr":[1,2]}}}"""));
        Assert.Equal(10, p["inv.inputWatts"]);
        Assert.Equal(new List<object?> { 1, 2 }, p["inv.arr"]);
        Assert.Empty(JsonMessages.Parse(Bytes("not json")));
    }

    [Fact]
    public void JsonDetection_DoesNotMistakeProtobuf()
    {
        Assert.True(JsonMessages.LooksLikeJson(Bytes("{}")));
        Assert.False(JsonMessages.LooksLikeJson(new byte[] { 0x0A, (byte)'{' }));
    }

    [Fact]
    public void Command_CarriesAppFraming()
    {
        var json = Json(JsonMessages.Command(5, "acOutCfg", new Dictionary<string, object> { ["enabled"] = 1 }, moduleSn: "SN1"));
        Assert.Equal("Android", json.GetProperty("from").GetString());
        Assert.Equal("1.0", json.GetProperty("version").GetString());
        Assert.Equal(5, json.GetProperty("moduleType").GetInt32());
        Assert.Equal("acOutCfg", json.GetProperty("operateType").GetString());
        Assert.Equal("SN1", json.GetProperty("moduleSn").GetString());
        Assert.Equal(1, json.GetProperty("params").GetProperty("enabled").GetInt32());
    }

    [Fact]
    public void Delta2State_MapsKeysAndConvertsMillivolts()
    {
        var p = JsonMessages.Parse(Bytes("""
            {"params":{"bms_emsStatus.lcdShowSoc":85,"pd.wattsInSum":0,"pd.wattsOutSum":120,
            "inv.acInVol":140000,"inv.inputWatts":0,"bms_emsStatus.dsgRemainTime":300,
            "bms_emsStatus.chgRemainTime":5939}}
            """));
        var s = Delta2Protocol.Instance.GetState(p);
        Assert.Equal(85, s.Soc);
        Assert.Equal(140, s.AcInVolt);
        Assert.True(s.GridConnected);
        Assert.Equal(GridStatus.Weak, s.GetGridStatus(180));
        Assert.Null(s.ChargeRemainMin); // 5939 — ознака «немає оцінки»
        Assert.Equal(new BatteryFlow.Discharging(300), s.GetBatteryFlow());
    }

    [Fact]
    public void Delta2WithoutGridVoltage_HasNoGrid()
    {
        var s = Delta2Protocol.Instance.GetState(new DeviceParams { ["inv.acInVol"] = 0, ["inv.inputWatts"] = 0 });
        Assert.False(s.GridConnected);
    }

    [Fact]
    public void Delta2AcToggle_BuildsDocumentedCommand()
    {
        var toggle = Delta2Protocol.Instance.GetControls("SN").OfType<ToggleControl>().First(c => c.Id == "ac_out");
        var json = Json(toggle.Command(true, new DeviceParams()));
        Assert.Equal("acOutCfg", json.GetProperty("operateType").GetString());
        Assert.Equal(5, json.GetProperty("moduleType").GetInt32());
        Assert.Equal(1, json.GetProperty("params").GetProperty("enabled").GetInt32());
        Assert.Equal(new DeviceParams { ["mppt.cfgAcEnabled"] = 1 }, toggle.Optimistic(true));
    }

    [Fact]
    public void DeltaPro3Commands_AreTcpWithParameterId()
    {
        var toggle = DeltaPro3Protocol.Instance.GetControls("SN").OfType<ToggleControl>().First(c => c.Id == "ac_hv");
        var json = Json(toggle.Command(true, new DeviceParams()));
        Assert.Equal("TCP", json.GetProperty("operateType").GetString());
        Assert.Equal(66, json.GetProperty("params").GetProperty("id").GetInt32());
        Assert.Equal(1, json.GetProperty("params").GetProperty("cfgHvAcOutOpen").GetInt32());
    }

    [Fact]
    public void DeltaMax_UsesOwnKeys()
    {
        var s = DeltaMaxProtocol.Instance.GetState(new DeviceParams
        {
            ["ems.lcdShowSoc"] = 55, ["bmsMaster.cycles"] = 12, ["pd.wattsOutSum"] = 30
        });
        Assert.Equal(55, s.Soc);
        Assert.Equal(12, s.Cycles);
    }
}
