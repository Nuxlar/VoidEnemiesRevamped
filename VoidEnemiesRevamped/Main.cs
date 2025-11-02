using BepInEx;
using EntityStates.NullifierMonster;
using EntityStates.VoidJailer;
using EntityStates.VoidJailer.Weapon;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VoidEnemiesRevamped.NewEntityStates.Jailer;
using VoidEnemiesRevamped.NewEntityStates.Reaver;

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


    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);
      AddContent();
      LoadAssets();
      TweakAssets();

      // PluginDirectory = Path.GetDirectoryName(Info.Location);

      // change Capture skilldriver on jailer 0.8 currently maxUserHealthFraction
      // WhiteCannonProjectile ProjectileImpactExplosion childrenProjectilePrefab
      // BlackCannonProjectile remove MegacrabProjectileController
      // Need to rewrite portal bomb to be predictive like the titan fist

      // reaver, jailer, devastator

      On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += PreventPermaRoot;
    }

    private void PreventPermaRoot(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
      if (buffDef == RoR2Content.Buffs.NullifyStack && self.HasBuff(RoR2Content.Buffs.Nullified))
        return;
      orig(self, buffDef, duration);
    }

    private void Glorp(On.EntityStates.VoidJailer.Weapon.Capture2.orig_OnEnter orig, Capture2 self)
    {
      self.outer.SetState(new Capture());
    }
    private void Glorp4(On.EntityStates.VoidJailer.Capture.orig_OnEnter orig, Capture self)
    {
      Capture.tetherReelSpeed = 0; // 50 orig

      // Capture.innerRangeDebuffDuration = 1f; // 5f orig
      self.duration = 5f; // 0.2 orig
      orig(self);
    }

    private void AddContent()
    {
      ContentAddition.AddEntityState<ChargeCaptureNux>(out _);
      ContentAddition.AddEntityState<PortalBombNux>(out _);
    }

    private static void LoadAssets()
    {
      AssetReferenceT<BuffDef> tetherDebuff2Ref = new AssetReferenceT<BuffDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidJailer.bdJailerSlow_asset);
      //  AssetAsyncReferenceManager<BuffDef>.LoadAsset(tetherDebuff2Ref).Completed += (x) => tetherDebuff = x.Result;
    }

    private static void TweakAssets()
    {
      AssetReferenceT<SkillDef> reaverSkillRef = new AssetReferenceT<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Nullifier.FireNullifier_asset);
      AssetAsyncReferenceManager<SkillDef>.LoadAsset(reaverSkillRef).Completed += (x) =>
      {
        SkillDef portalBombSkill = x.Result;
        portalBombSkill.activationState = new EntityStates.SerializableEntityStateType(typeof(PortalBombNux));
      };
      AssetReferenceT<GameObject> reaverBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Nullifier.NullifierBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(reaverBodyRef).Completed += (x) =>
      {
        GameObject reaverBody = x.Result;
        CharacterBody cb = reaverBody.GetComponent<CharacterBody>();
        cb.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath;
        reaverBody.GetComponent<SetStateOnHurt>().canBeHitStunned = false;
      };
      AssetReferenceT<GameObject> jailerBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidJailer.VoidJailerBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(jailerBodyRef).Completed += (x) =>
      {
        GameObject jailerBody = x.Result;
        CharacterBody cb = jailerBody.GetComponent<CharacterBody>();
        cb.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath;
        jailerBody.GetComponent<SetStateOnHurt>().canBeHitStunned = false;
      };
      AssetReferenceT<GameObject> devastatorBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(devastatorBodyRef).Completed += (x) =>
      {
        GameObject devastatorBody = x.Result;
        CharacterBody cb = devastatorBody.GetComponent<CharacterBody>();
        cb.bodyFlags |= CharacterBody.BodyFlags.ImmuneToVoidDeath;
        devastatorBody.GetComponent<SetStateOnHurt>().canBeHitStunned = false;
      };
    }
  }
}