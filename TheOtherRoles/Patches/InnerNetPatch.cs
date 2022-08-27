using HarmonyLib;
using UnhollowerBaseLib;
using Hazel.Udp;
namespace TheOtherRoles.Patches
{
    [HarmonyPatch(typeof(UnityUdpClientConnection), nameof(UnityUdpClientConnection.ConnectAsync))]
    public static class UnityUdpClientConnectionConnectAsyncPatch
    {
        public static void Prefix(UnityUdpClientConnection __instance, Il2CppStructArray<byte> bytes)
        {
            __instance.KeepAliveInterval = 2000;
            __instance.DisconnectTimeoutMs = 15000;
        }
    }
}
