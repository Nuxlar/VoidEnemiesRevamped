using EntityStates.NullifierMonster;
using RoR2;
using RoR2.Projectile;
using System.Linq;
using UnityEngine;

namespace VoidEnemiesRevamped.NewEntityStates.Reaver;

public class PortalBombNux : EntityStates.BaseState
{
    public static int portalBombCount = 3;
    public static float minDistanceBetweenBombs = 8f;
    public static float radius = 5f;
    private HurtBox targetHurtBox;
    private float trackingDuration = 0.5f;
    private float elapsedTime;
    private PortalBombNux.Predictor predictor;
    private Vector3 predictedTargetPosition;
    private int bombsFired = 0;
    private CharacterBody targetBody;
    private Vector3 targetVelocity;

    public override void OnEnter()
    {
        base.OnEnter();
        this.elapsedTime = 0.0f;
        BullseyeSearch bullseyeSearch = new BullseyeSearch();
        bullseyeSearch.viewer = this.characterBody;
        bullseyeSearch.searchOrigin = this.characterBody.corePosition;
        bullseyeSearch.searchDirection = this.characterBody.corePosition;
        bullseyeSearch.maxDistanceFilter = FirePortalBomb.maxDistance;
        bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(this.GetTeam());
        bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
        bullseyeSearch.filterByLoS = false;
        bullseyeSearch.RefreshCandidates();
        this.targetHurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
        if ((bool)this.targetHurtBox)
        {
            this.predictor = new PortalBombNux.Predictor(this.transform);
            this.predictor.SetTargetTransform(this.targetHurtBox.transform);
            this.targetBody = this.targetHurtBox.healthComponent.body;
            this.targetVelocity = targetBody.characterMotor.velocity;
            if (!this.targetBody.hasAuthority)
            {
                //Less accurate, but it works online.
                this.targetVelocity = (targetBody.transform.position - targetBody.previousPosition) / Time.fixedDeltaTime;
            }
        }
        else
            this.outer.SetNextStateToMain();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        this.elapsedTime += Time.fixedDeltaTime;
        if (this.predictor != null && this.bombsFired == 0)
            this.predictor.Update();
        if ((double)this.elapsedTime <= this.trackingDuration)
            return;

        if (!this.predictor.hasTargetTransform || !this.predictor.isPredictionReady)
            return;

        if (this.bombsFired == 0)
            this.predictor.GetPredictedTargetPosition(this.trackingDuration, out this.predictedTargetPosition);

        Vector3 point = this.predictedTargetPosition;

        // Raycast to floor if target is moving down to prevent bombs from spawning in the ground
        if (this.targetBody.characterMotor && !targetBody.characterMotor.isGrounded && this.targetVelocity.y > 0f)
            point = RaycastToFloor(this.predictedTargetPosition);

        if (this.bombsFired < PortalBombNux.portalBombCount)
        {
            if (this.bombsFired > 0)
            {
                point += PortalBombNux.GetPointOnUnitSphereCap();
            }
            this.FireBomb(point);
            EffectManager.SimpleMuzzleFlash(FirePortalBomb.muzzleflashEffectPrefab, this.gameObject, FirePortalBomb.muzzleString, true);
            this.bombsFired++;
        }

        if (this.bombsFired < PortalBombNux.portalBombCount)
            return;
        this.outer.SetNextStateToMain();
    }

    public static Vector3 GetPointOnUnitSphereCap()
    {
        Quaternion targetDirection = Quaternion.LookRotation(Vector3.up);
        float angle = 180f;
        float angleInRad = Random.Range(0.0f, angle) * Mathf.Deg2Rad;
        Vector2 pointOnCircle = Random.insideUnitCircle.normalized * PortalBombNux.minDistanceBetweenBombs * Mathf.Sin(angleInRad);
        Vector3 v = new Vector3(pointOnCircle.x, pointOnCircle.y, Mathf.Cos(angleInRad));
        return targetDirection * v;
    }

    private void FireBomb(Vector3 targetPos)
    {
        ProjectileManager.instance.FireProjectile(new FireProjectileInfo()
        {
            projectilePrefab = FirePortalBomb.portalBombProjectileEffect,
            position = targetPos,
            rotation = Quaternion.identity,
            owner = this.gameObject,
            damage = this.damageStat * FirePortalBomb.damageCoefficient,
            force = FirePortalBomb.force,
            crit = this.characterBody.RollCrit()
        });
    }

    private Vector3 RaycastToFloor(Vector3 position)
    {
        RaycastHit hitInfo;
        return Physics.Raycast(new Ray(position, Vector3.down), out hitInfo, 500f, (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hitInfo.point : new Vector3();
    }

    private class Predictor
    {
        private Transform ownerTransform;
        private Transform targetTransform;
        private Vector3 recentPosition0;
        private Vector3 recentPosition1;
        private Vector3 recentPosition2;
        private int samplesCollected;

        public Predictor(Transform bodyTransform) => this.ownerTransform = bodyTransform;

        public bool hasTargetTransform => (bool)this.targetTransform;

        public bool isPredictionReady => this.samplesCollected > 2;

        private void PushTargetPosition(Vector3 newTargetPosition)
        {
            this.recentPosition2 = this.recentPosition1;
            this.recentPosition1 = this.recentPosition0;
            this.recentPosition0 = newTargetPosition;
            ++this.samplesCollected;
        }

        public void SetTargetTransform(Transform newTargetTransform)
        {
            this.targetTransform = newTargetTransform;
            this.recentPosition2 = this.recentPosition1 = this.recentPosition0 = newTargetTransform.position;
            this.samplesCollected = 1;
        }

        public void Update()
        {
            if (!(bool)this.targetTransform)
                return;
            this.PushTargetPosition(this.targetTransform.position);
        }

        public bool GetPredictedTargetPosition(float time, out Vector3 predictedPosition)
        {
            Vector3 deltaOldToMid = this.recentPosition1 - this.recentPosition2;
            Vector3 deltaMidToNew = this.recentPosition0 - this.recentPosition1;
            deltaOldToMid.y = 0.0f;
            deltaMidToNew.y = 0.0f;
            PortalBombNux.Predictor.ExtrapolationType extrapolationType = deltaOldToMid == Vector3.zero || deltaMidToNew == Vector3.zero ? PortalBombNux.Predictor.ExtrapolationType.None : ((double)Vector3.Dot(deltaOldToMid.normalized, deltaMidToNew.normalized) <= 0.980000019073486 ? PortalBombNux.Predictor.ExtrapolationType.Polar : PortalBombNux.Predictor.ExtrapolationType.Linear);
            float fixedUpdateRate = 1f / Time.fixedDeltaTime;
            predictedPosition = this.recentPosition0;
            switch (extrapolationType)
            {
                case PortalBombNux.Predictor.ExtrapolationType.Linear:
                    predictedPosition = this.recentPosition0 + deltaMidToNew * (time * fixedUpdateRate);
                    break;
                case PortalBombNux.Predictor.ExtrapolationType.Polar:
                    Vector3 origin = this.ownerTransform.position;
                    Vector2 pos2D_old = Util.Vector3XZToVector2XY(this.recentPosition2 - origin);
                    Vector2 pos2D_mid = Util.Vector3XZToVector2XY(this.recentPosition1 - origin);
                    Vector2 pos2D_new = Util.Vector3XZToVector2XY(this.recentPosition0 - origin);
                    float magOld = pos2D_old.magnitude;
                    float magMid = pos2D_mid.magnitude;
                    float magNew = pos2D_new.magnitude;
                    float angleRateOldToMid = Vector2.SignedAngle(pos2D_old, pos2D_mid) * fixedUpdateRate;
                    float angleRateMidToNew = Vector2.SignedAngle(pos2D_mid, pos2D_new) * fixedUpdateRate;
                    double magDeltaOldToMid = ((double)magMid - (double)magOld) * (double)fixedUpdateRate;
                    double magDeltaMidToNew = ((double)magNew - (double)magMid) * (double)fixedUpdateRate;
                    float averageAngleRate = (float)(((double)angleRateOldToMid + (double)angleRateMidToNew) * 0.5);
                    double avgMagAccel = magDeltaOldToMid + magDeltaMidToNew;
                    float averageMagAccel = (float)(avgMagAccel * 0.5);
                    float predictedRadius = magNew + averageMagAccel * time;
                    if ((double)predictedRadius < 0.0)
                        predictedRadius = 0.0f;
                    Vector2 rotated = Util.RotateVector2(pos2D_new, averageAngleRate * time) * (predictedRadius * magNew);
                    predictedPosition = origin;
                    predictedPosition.x += rotated.x;
                    predictedPosition.z += rotated.y;
                    break;
            }
            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(predictedPosition + Vector3.up * 1f, Vector3.down), out hitInfo, 10f, (int)LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                predictedPosition = hitInfo.point;
            return true;
        }

        private enum ExtrapolationType
        {
            None,
            Linear,
            Polar,
        }
    }
}
