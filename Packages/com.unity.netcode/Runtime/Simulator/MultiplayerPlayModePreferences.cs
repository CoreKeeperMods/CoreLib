#if UNITY_EDITOR
using System;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEditor;
using UnityEngine;

namespace Unity.NetCode
{
    /// <summary>Developer preferences for the `MultiplayerPlayModeWindow`. Only applicable in editor.</summary>
    public static class MultiplayerPlayModePreferences
    {
        public const SimulatorView DefaultSimulatorView = SimulatorView.PingView;

        const int k_MaxPacketDelayMs = 2000;
        const int k_MaxPacketJitterMs = 200;
        const int k_DefaultSimulatorMaxPacketCount = 300;

        static string s_PrefsKeyPrefix = $"MultiplayerPlayMode_{Application.productName}_";
        static string s_PlayModeTypeKey = s_PrefsKeyPrefix + "PlayMode_Type";

        static string s_RequestedSimulatorViewKey = s_PrefsKeyPrefix + "SimulatorView";
        static string s_SimulatorPreset = s_PrefsKeyPrefix + "SimulatorPreset";

        static string s_PacketDelayMsKey = s_PrefsKeyPrefix + "PacketDelayMs";
        static string s_PacketJitterMsKey = s_PrefsKeyPrefix + "PacketJitterMs";
        static string s_PacketDropPercentageKey = s_PrefsKeyPrefix + "PacketDropRate";

        static string s_RequestedNumThinClientsKey = s_PrefsKeyPrefix + "NumThinClients";
        static string s_StaggerThinClientCreationKey = s_PrefsKeyPrefix + "StaggerThinClientCreation";

        static string s_AutoConnectionAddressKey = s_PrefsKeyPrefix + "AutoConnection_Address";
        static string s_AutoConnectionPortKey = s_PrefsKeyPrefix + "AutoConnection_Port";

        static string s_LagSpikeDurationSelectionKey = s_PrefsKeyPrefix + "LagSpikeDurationSelection";

        public static bool SimulatorEnabled => RequestedSimulatorView != SimulatorView.Disabled;
        static string s_ApplyLoggerSettings = s_PrefsKeyPrefix + "NetDebugLogger_ApplyOverload";
        static string s_LoggerLevelType = s_PrefsKeyPrefix + "NetDebugLogger_LogLevelType";
        static string s_TargetShouldDumpPackets = s_PrefsKeyPrefix + "NetDebugLogger_ShouldDumpPackets";
        static string s_ShowAllSimulatorPresets = s_PrefsKeyPrefix + "ShowAllSimulatorPresets";

        /// <summary>Editor "mode". Displays different data, and can be used to disable the simulator entirely.</summary>
        public static SimulatorView RequestedSimulatorView
        {
            get => (SimulatorView) EditorPrefs.GetInt(s_RequestedSimulatorViewKey, (int) DefaultSimulatorView);
            set => EditorPrefs.SetInt(s_RequestedSimulatorViewKey, (int) value);
        }

        /// <inheritdoc cref="SimulatorUtility.Parameters"/>
        public static SimulatorUtility.Parameters ClientSimulatorParameters => new SimulatorUtility.Parameters
        {
            Mode = ApplyMode.AllPackets, MaxPacketSize = NetworkParameterConstants.MTU, MaxPacketCount = k_DefaultSimulatorMaxPacketCount,
            PacketDelayMs = PacketDelayMs, PacketJitterMs = PacketJitterMs,
            PacketDropPercentage = PacketDropPercentage, PacketDuplicationPercentage = 0, FuzzFactor = 0,
        };

        /// <summary>Denotes what type of worlds are created by <see cref="ClientServerBootstrap"/> when entering playmode in the editor.</summary>
        public static ClientServerBootstrap.PlayType RequestedPlayType
        {
            get => (ClientServerBootstrap.PlayType) EditorPrefs.GetInt(s_PlayModeTypeKey, (int) ClientServerBootstrap.PlayType.ClientAndServer);
            set => EditorPrefs.SetInt(s_PlayModeTypeKey, (int) value);
        }

        private static string s_SimulateDedicatedServer = s_PrefsKeyPrefix + "SimulateDedicatedServer";
        public static bool SimulateDedicatedServer
        {
            get => EditorPrefs.GetBool(s_SimulateDedicatedServer, false);
            set => EditorPrefs.SetBool(s_SimulateDedicatedServer, value);
        }

        /// <inheritdoc cref="SimulatorUtility.Parameters.PacketDelayMs"/>
        public static int PacketDelayMs
        {
            get => math.clamp(EditorPrefs.GetInt(s_PacketDelayMsKey, 0), 0, k_MaxPacketDelayMs);
            set => EditorPrefs.SetInt(s_PacketDelayMsKey, math.clamp(value, 0, k_MaxPacketDelayMs));
        }

        /// <inheritdoc cref="SimulatorUtility.Parameters.PacketJitterMs"/>
        public static int PacketJitterMs
        {
            get => math.clamp(EditorPrefs.GetInt(s_PacketJitterMsKey, 0), 0, k_MaxPacketJitterMs);
            set => EditorPrefs.SetInt(s_PacketJitterMsKey, math.clamp(value, 0, k_MaxPacketJitterMs));
        }

        /// <inheritdoc cref="SimulatorUtility.Parameters.PacketDropPercentage"/>
        public static int PacketDropPercentage
        {
            get => math.clamp(EditorPrefs.GetInt(s_PacketDropPercentageKey, 0), 0, 100);
            set => EditorPrefs.SetInt(s_PacketDropPercentageKey, math.clamp(value, 0, 100));
        }

        /// <summary>Denotes how many thin client worlds are created in the <see cref="ClientServerBootstrap"/> (and at runtime, the PlayMode window).</summary>
        public static int RequestedNumThinClients
        {
            get => math.clamp(EditorPrefs.GetInt(s_RequestedNumThinClientsKey, 0), 0, ClientServerBootstrap.k_MaxNumThinClients);
            set => EditorPrefs.SetInt(s_RequestedNumThinClientsKey, math.clamp(value, 0, ClientServerBootstrap.k_MaxNumThinClients));
        }

        /// <summary>How many thin client worlds to spawn per second. 0 implies spawn all at once.</summary>
        public static float ThinClientCreationFrequency
        {
            get => math.clamp(EditorPrefs.GetFloat(s_StaggerThinClientCreationKey, 2), 0f, 1_000);
            set => EditorPrefs.SetFloat(s_StaggerThinClientCreationKey, value);
        }

        public static string AutoConnectionAddress
        {
            get => EditorPrefs.GetString(s_AutoConnectionAddressKey, "127.0.0.1");
            set => EditorPrefs.SetString(s_AutoConnectionAddressKey, value);
        }

        public static ushort AutoConnectionPort
        {
            get => (ushort) EditorPrefs.GetInt(s_AutoConnectionPortKey, 0);
            set => EditorPrefs.SetInt(s_AutoConnectionPortKey, value);
        }

        /// <summary>Maps to a <see cref="SimulatorPreset"/>.</summary>
        public static string CurrentNetworkSimulatorPreset
        {
            get => EditorPrefs.GetString(s_SimulatorPreset, null);
            set => EditorPrefs.SetString(s_SimulatorPreset, value);
        }

        /// <summary>True if is user-defined, custom preset.</summary>
        public static bool IsCurrentNetworkSimulatorPresetCustom => SimulatorPreset.k_CustomProfileKey.Equals(CurrentNetworkSimulatorPreset, StringComparison.OrdinalIgnoreCase);

        /// <summary>There is a hardcoded list of lag spike values. This is the saved indexer.</summary>
        public static int LagSpikeSelectionIndex
        {
            get => EditorPrefs.GetInt(s_LagSpikeDurationSelectionKey, 4); // Default 1s.
            set => EditorPrefs.SetInt(s_LagSpikeDurationSelectionKey, value);
        }

        /// <summary>If true, will force <see cref="NetDebugSystem"/> to set these values on boot.</summary>
        public static bool ApplyLoggerSettings
        {
            get => EditorPrefs.GetBool(s_ApplyLoggerSettings, false);
            set => EditorPrefs.SetBool(s_ApplyLoggerSettings, value);
        }

        /// <summary>If <see cref="ApplyLoggerSettings"/>, forces all <see cref="NetDebugSystem"/> loggers to this log level.</summary>
        public static NetDebug.LogLevelType TargetLogLevel
        {
            get => (NetDebug.LogLevelType) EditorPrefs.GetInt(s_LoggerLevelType, (int) NetDebug.LogLevelType.Notify);
            set => EditorPrefs.SetInt(s_LoggerLevelType, (int)value);
        }

        /// <summary>If <see cref="ApplyLoggerSettings"/>, forces all <see cref="NetDebugSystem"/> loggers to have this value for ShouldDumpPackets.</summary>
        public static bool TargetShouldDumpPackets
        {
            get => EditorPrefs.GetBool(s_TargetShouldDumpPackets, false);
            set => EditorPrefs.SetBool(s_TargetShouldDumpPackets, value);
        }

        /// <summary>If true, all simulator presets will be visible, rather than only platform specific ones.</summary>
        public static bool ShowAllSimulatorPresets
        {
            get => EditorPrefs.GetBool(s_ShowAllSimulatorPresets, false);
            set => EditorPrefs.SetBool(s_ShowAllSimulatorPresets, value);
        }

        /// <summary>Returns true if the editor-inputted address is a valid connection address.</summary>
        public static bool IsEditorInputtedAddressValidForConnect(out NetworkEndpoint ep)
        {
            if (AutoConnectionPort != 0 && NetworkEndpoint.TryParse(AutoConnectionAddress, AutoConnectionPort, out ep) && ep.IsValid && !ep.IsAny)
                return true;

            ep = default;
            return false;
        }

        /// <summary>Apply the selected preset to the static, saved fields.
        /// Clobbers any custom values the user may have entered.</summary>
        /// <param name="preset">Preset to apply.</param>
        public static void ApplySimulatorPresetToPrefs(SimulatorPreset preset)
        {
            if (!preset.IsCustom)
            {
                PacketDelayMs = preset.PacketDelayMs;
                PacketJitterMs = preset.PacketJitterMs;
                PacketDropPercentage = math.clamp(preset.PacketLossPercent, 0, 100);
            }
        }
    }

    /// <summary>For the Multiplayer PlayMode Window.</summary>
    public enum SimulatorView
    {
        Disabled,
        PingView,
        PerPacketView,
    }
}
#endif
