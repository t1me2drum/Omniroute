namespace Omniroute.Protocol;

/// <summary>
/// Протокол для Delta 2 і Delta 2 Max
/// Використовує JSON формат повідомлень через MQTT
/// </summary>
public class Delta2Protocol : IDeviceProtocol
{
    private readonly string[] _solarKeys;
    private readonly bool _isMax;

    public Delta2Protocol(bool isMax = false)
    {
        _isMax = isMax;
        _solarKeys = isMax
            ? new[] { "mppt.inWatts", "mppt.dc24vWatts" }
            : new[] { "mppt.inWatts" };
    }

    public DeviceParams Parse(TopicKind kind, byte[] payload)
    {
        return kind switch
        {
            TopicKind.Data or TopicKind.GetReply => JsonMessages.Parse(payload),
            TopicKind.SetReply => new DeviceParams(),
            _ => new DeviceParams()
        };
    }

    public OutgoingMessage QuotaRequest(string serialNumber)
    {
        return JsonMessages.LatestQuotas();
    }

    public DeviceState GetState(DeviceParams p)
    {
        var acInVolt = p.GetNumber("inv.acInVol");
        var acInVoltInt = acInVolt.HasValue ? (int)(acInVolt.Value / 1000) : (int?)null;
        var acIn = p.GetInt("inv.inputWatts");

        return new DeviceState
        {
            Soc = p.GetInt("bms_emsStatus.lcdShowSoc") ?? p.GetInt("bms_bmsStatus.soc"),
            InputW = p.GetInt("pd.wattsInSum"),
            OutputW = p.GetInt("pd.wattsOutSum"),
            AcInW = acIn,
            AcOutW = p.GetInt("inv.outputWatts"),
            SolarW = p.SumOf(_solarKeys),
            DcOutW = p.GetInt("mppt.outWatts") ?? p.GetInt("pd.carWatts"),
            UsbOutW = p.SumOf("pd.typec1Watts", "pd.typec2Watts", "pd.usb1Watts",
                             "pd.usb2Watts", "pd.qcUsb1Watts", "pd.qcUsb2Watts"),
            ChargeRemainMin = DeviceParamsExtensions.ValidMinutes(p.GetInt("bms_emsStatus.chgRemainTime")),
            DischargeRemainMin = DeviceParamsExtensions.ValidMinutes(p.GetInt("bms_emsStatus.dsgRemainTime")),
            BatteryTempC = p.GetInt("bms_bmsStatus.temp"),
            AcInVolt = acInVoltInt,
            GridConnected = acInVoltInt == null && acIn == null
                ? null
                : (acInVoltInt ?? 0) > 100 || (acIn ?? 0) > 0,
            Cycles = p.GetInt("bms_bmsStatus.cycles"),
            Soh = p.GetInt("bms_bmsStatus.soh")
        };
    }

    public List<IControl> GetControls(string serialNumber)
    {
        var controls = new List<IControl>
        {
            // AC вихід
            new ToggleControl
            {
                Id = "ac_out",
                Label = "AC вихід",
                Section = ControlSection.Outputs,
                Read = p => p.GetFlag("mppt.cfgAcEnabled"),
                Command = (on, _) => JsonMessages.Command(5, "acOutCfg", new()
                {
                    ["enabled"] = on ? 1 : 0,
                    ["out_voltage"] = -1,
                    ["out_freq"] = 255,
                    ["xboost"] = 255
                }),
                Optimistic = on => new DeviceParams { ["mppt.cfgAcEnabled"] = on ? 1 : 0 }
            },

            // X-Boost
            new ToggleControl
            {
                Id = "xboost",
                Label = "X-Boost",
                Section = ControlSection.Outputs,
                Read = p => p.GetFlag("mppt.cfgAcXboost"),
                Command = (on, _) => JsonMessages.Command(5, "acOutCfg", new()
                {
                    ["enabled"] = 255,
                    ["out_voltage"] = -1,
                    ["out_freq"] = 255,
                    ["xboost"] = on ? 1 : 0
                }),
                Optimistic = on => new DeviceParams { ["mppt.cfgAcXboost"] = on ? 1 : 0 }
            },

            // DC 12V вихід
            new ToggleControl
            {
                Id = "dc_out",
                Label = "DC 12V вихід",
                Section = ControlSection.Outputs,
                Read = p => p.GetFlag("pd.carState"),
                Command = (on, _) => JsonMessages.Command(5, "mpptCar", new()
                {
                    ["enabled"] = on ? 1 : 0
                }),
                Optimistic = on => new DeviceParams { ["pd.carState"] = on ? 1 : 0 }
            },

            // USB виходи
            new ToggleControl
            {
                Id = "usb_out",
                Label = "USB виходи",
                Section = ControlSection.Outputs,
                Read = p => p.GetFlag("pd.dcOutState"),
                Command = (on, _) => JsonMessages.Command(1, "dcOutCfg", new()
                {
                    ["enabled"] = on ? 1 : 0
                }),
                Optimistic = on => new DeviceParams { ["pd.dcOutState"] = on ? 1 : 0 }
            },

            // Потужність заряду від мережі
            new SliderControl
            {
                Id = "ac_chg_w",
                Label = "Потужність заряду від мережі",
                Section = ControlSection.Charging,
                Min = 200,
                Max = _isMax ? 2400 : 1200,
                Step = 100,
                Unit = "Вт",
                Read = p => p.GetInt("mppt.cfgChgWatts"),
                Command = (watts, _) => JsonMessages.Command(5, "acChgCfg", new()
                {
                    ["chgWatts"] = watts,
                    ["chgPauseFlag"] = 255
                }),
                Optimistic = watts => new DeviceParams { ["mppt.cfgChgWatts"] = watts }
            },

            // Максимальний рівень заряду
            new SliderControl
            {
                Id = "max_chg",
                Label = "Макс. рівень заряду",
                Section = ControlSection.Charging,
                Min = 50,
                Max = 100,
                Step = 1,
                Unit = "%",
                Read = p => p.GetInt("bms_emsStatus.maxChargeSoc"),
                Command = (soc, _) => JsonMessages.Command(2, "upsConfig", new()
                {
                    ["maxChgSoc"] = soc
                }),
                Optimistic = soc => new DeviceParams { ["bms_emsStatus.maxChargeSoc"] = soc }
            },

            // Мінімальний рівень розряду
            new SliderControl
            {
                Id = "min_dsg",
                Label = "Мін. рівень розряду",
                Section = ControlSection.Charging,
                Min = 0,
                Max = 30,
                Step = 1,
                Unit = "%",
                Read = p => p.GetInt("bms_emsStatus.minDsgSoc"),
                Command = (soc, _) => JsonMessages.Command(2, "dsgCfg", new()
                {
                    ["minDsgSoc"] = soc
                }),
                Optimistic = soc => new DeviceParams { ["bms_emsStatus.minDsgSoc"] = soc }
            },

            // Беззвучний режим
            new ToggleControl
            {
                Id = "quiet",
                Label = "Беззвучний режим",
                Section = ControlSection.System,
                Read = p => p.GetFlag("mppt.beepState"),
                Command = (on, _) => JsonMessages.Command(5, "quietMode", new()
                {
                    ["enabled"] = on ? 1 : 0
                }),
                Optimistic = on => new DeviceParams { ["mppt.beepState"] = on ? 1 : 0 }
            },

            // Вимкнення екрана
            new ChoiceControl
            {
                Id = "screen",
                Label = "Вимкнення екрана",
                Section = ControlSection.System,
                Options = ControlOptions.ScreenTimeoutOptions,
                Read = p => p.GetInt("pd.lcdOffSec"),
                Command = (value, _) => JsonMessages.Command(1, "lcdCfg", new()
                {
                    ["brighLevel"] = 255,
                    ["delayOff"] = value
                }),
                Optimistic = value => new DeviceParams { ["pd.lcdOffSec"] = value }
            },

            // Автовимкнення станції
            new ChoiceControl
            {
                Id = "unit_standby",
                Label = "Автовимкнення станції",
                Section = ControlSection.System,
                Options = ControlOptions.StandbyOptions,
                Read = p => p.GetInt("pd.standbyMin"),
                Command = (value, _) => JsonMessages.Command(1, "standbyTime", new()
                {
                    ["standbyMin"] = value
                }),
                Optimistic = value => new DeviceParams { ["pd.standbyMin"] = value }
            }
        };

        return controls;
    }
}
