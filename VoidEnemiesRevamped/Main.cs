using BepInEx;
using EntityStates.NullifierMonster;
using EntityStates.VoidJailer;
using EntityStates.VoidJailer.Weapon;
using RoR2;
using RoR2.ContentManagement;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace VoidEnemiesRevamped
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "VoidEnemiesRevamped";
    public const string PluginVersion = "1.0.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public static BuffDef tetherDebuff;
    public static BuffDef tetherDebuff2;

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);
      LoadAssets();

      // PluginDirectory = Path.GetDirectoryName(Info.Location);

      // change Capture skilldriver on jailer 0.8 currently maxUserHealthFraction
      // WhiteCannonProjectile ProjectileImpactExplosion childrenProjectilePrefab
      // BlackCannonProjectile remove MegacrabProjectileController
      // Need to rewrite portal bomb to be predictive like the titan fist
      On.EntityStates.VoidJailer.Weapon.Capture2.OnEnter += Glorp;
      On.EntityStates.VoidJailer.Capture.OnEnter += Glorp4;
      // On.EntityStates.NullifierMonster.AimPortalBomb.RaycastToFloor += Glorp2;
      // On.EntityStates.NullifierMonster.FirePortalBomb.OnEnter += Glorp3;
    }

    private void Glorp(On.EntityStates.VoidJailer.Weapon.Capture2.orig_OnEnter orig, Capture2 self)
    {
      self.outer.SetState(new Capture());
    }
    private void Glorp4(On.EntityStates.VoidJailer.Capture.orig_OnEnter orig, Capture self)
    {
      Capture.tetherReelSpeed = 0; // 50 orig
      Capture.innerRangeDebuff = tetherDebuff;
      Capture.tetherDebuff = tetherDebuff;

      // Capture.innerRangeDebuffDuration = 1f; // 5f orig
      self.duration = 5f; // 0.2 orig
      orig(self);
    }

    private Vector3? Glorp2(On.EntityStates.NullifierMonster.AimPortalBomb.orig_RaycastToFloor orig, AimPortalBomb self, Vector3 position)
    {
      return position;
    }

    private static void LoadAssets()
    {
      AssetReferenceT<BuffDef> tetherDebuffRef = new AssetReferenceT<BuffDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidJailer.bdJailerSlow_asset);
      AssetAsyncReferenceManager<BuffDef>.LoadAsset(tetherDebuffRef).Completed += (x) => tetherDebuff = x.Result;

      AssetReferenceT<BuffDef> tetherDebuff2Ref = new AssetReferenceT<BuffDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidJailer.bdJailerSlow_asset);
      AssetAsyncReferenceManager<BuffDef>.LoadAsset(tetherDebuff2Ref).Completed += (x) => tetherDebuff = x.Result;
    }

    /*
        private static void TweakAssets()
        {
          Example for how to edit an asset once it finishes loading
          AssetAsyncReferenceManager<GameObject>.LoadAsset(new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Items_LowerPricedChests.PickupSaleStar_prefab)).Completed += delegate (AsyncOperationHandle<GameObject> obj)
          {
            MeshCollider collider = obj.Result.transform.find("SaleStar")?.GetComponent<MeshCollider>();
            if (collider)
            {
              collider.convex = true;
            }
          };
        }
    */
  }
}