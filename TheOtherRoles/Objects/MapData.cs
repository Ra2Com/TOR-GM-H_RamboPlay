using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnhollowerBaseLib.Attributes;

namespace TheOtherRoles.Objects
{
    public class MapData
    {
        public static ShipStatus AirShip;
        public static ShipStatus SkeldShip;
        public static ShipStatus MiraHQ;
        public static ShipStatus PolusShip;


        public static void LoadAssets(AmongUsClient __instance)
        {
            // Skeld
            AssetReference assetReference = __instance.ShipPrefabs.ToArray()[0];
            AsyncOperationHandle<GameObject> asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            SkeldShip = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();

            // Mira
            assetReference = __instance.ShipPrefabs.ToArray()[1];
            asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            MiraHQ = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();

            // Polus
            assetReference = __instance.ShipPrefabs.ToArray()[2];
            asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            PolusShip = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();

            // AirShip
            assetReference = __instance.ShipPrefabs.ToArray()[4];
            asset = assetReference.LoadAsset<GameObject>();
            asset.WaitForCompletion();
            AirShip = assetReference.Asset.Cast<GameObject>().GetComponent<ShipStatus>();
        }
    }
}
