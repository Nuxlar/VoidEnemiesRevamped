using RoR2;
using UnityEngine;
using EntityStates;
using EntityStates.VoidJailer.Weapon;

namespace VoidEnemiesRevamped.NewEntityStates.Jailer;

public class ChargeCaptureNux : BaseState
{
    public static string animationLayerName;
    public static string animationStateName;
    public static string animationPlaybackRateName;
    public static float duration;
    public static string enterSoundString;
    public static GameObject chargeEffectPrefab;
    public static GameObject attackIndicatorPrefab;
    public static string muzzleString;
    private float _crossFadeDuration;
    private uint soundID;
    private GameObject attackIndicatorInstance;

    public override void OnEnter()
    {
        base.OnEnter();
        ChargeCapture.duration /= this.attackSpeedStat;
        this._crossFadeDuration = ChargeCapture.duration * 0.25f;
        this.PlayCrossfade(ChargeCapture.animationLayerName, ChargeCapture.animationStateName, ChargeCapture.animationPlaybackRateName, ChargeCapture.duration, this._crossFadeDuration);
        this.soundID = Util.PlayAttackSpeedSound(ChargeCapture.enterSoundString, this.gameObject, this.attackSpeedStat);
        if ((bool)ChargeCapture.chargeEffectPrefab)
            EffectManager.SimpleMuzzleFlash(ChargeCapture.chargeEffectPrefab, this.gameObject, ChargeCapture.muzzleString, false);
        if (!(bool)ChargeCapture.attackIndicatorPrefab)
            return;
        Transform coreTransform = this.characterBody.coreTransform;
        if (!(bool)coreTransform)
            return;
        this.attackIndicatorInstance = Object.Instantiate<GameObject>(ChargeCapture.attackIndicatorPrefab, coreTransform);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!this.isAuthority || (double)this.fixedAge < (double)ChargeCapture.duration)
            return;
        this.outer.SetNextState(new Capture2());
    }

    public override void Update()
    {
        if ((bool)this.attackIndicatorInstance)
            this.attackIndicatorInstance.transform.forward = this.GetAimRay().direction;
        base.Update();
    }

    public override void OnExit()
    {
        AkSoundEngine.StopPlayingID(this.soundID);
        if ((bool)this.attackIndicatorInstance)
            EntityState.Destroy(this.attackIndicatorInstance);
        base.OnExit();
    }

    public override InterruptPriority GetMinimumInterruptPriority()
    {
        return InterruptPriority.PrioritySkill;
    }
}
