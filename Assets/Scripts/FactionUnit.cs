using TMPro;
using UnityEngine;

namespace TikTokFactionWar
{
    public enum UnitTier
    {
        Infantry = 0, // Comment 1 or 2
        Squad = 1,    // Rose (1 coin)
        Tank = 2,     // Donut (10 coins)
        Mecha = 3,    // Corgi / Sunglasses (100 coins)
        Dragon = 4    // Galaxy (500 coins)
    }

    /// <summary>
    /// 2D Faction Unit with compound silhouette parts, idle/walk sine bob, Viewer Avatar badge, nameplate, and combat logic.
    /// Follows craft-game-floors art-direction.md silhouette & motion rules.
    /// </summary>
    public class FactionUnit : MonoBehaviour
    {
        public int FactionId { get; private set; }
        public UnitTier Tier { get; private set; }
        public string OwnerUserId { get; private set; }
        public string OwnerNickname { get; private set; }

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public float AttackDamage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; }
        public float MoveSpeed { get; private set; }
        public float ShieldTimer { get; private set; }

        private Transform _visualRoot;
        private Transform _hpBarBgTransform;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _secondaryRenderer;
        private SpriteRenderer _weaponLeftRenderer;
        private SpriteRenderer _weaponRightRenderer;
        private SpriteRenderer _avatarRenderer;
        private SpriteRenderer _avatarFrameRenderer;
        private SpriteRenderer _hpBarFillRenderer;
        private SpriteRenderer _shieldRingRenderer;
        private TextMeshPro _nameLabel;

        private float _attackCooldown;
        private float _animSeed;
        private Vector3 _baseScale = Vector3.one;
        private float _hitPopTimer;
        private float _attackAnimTimer;
        private CircleCollider2D _unitCollider;

        public bool IsAlive => CurrentHp > 0f && gameObject.activeSelf;

        public void BuildVisualHierarchyIfNeeded()
        {
            if (_visualRoot != null) return;

            Material spriteMat = Palette.GetSpriteMaterial();

            // Simplified 2D collision shape per 2d-games/SKILL.md Section 3
            _unitCollider = gameObject.GetComponent<CircleCollider2D>();
            if (_unitCollider == null)
            {
                _unitCollider = gameObject.AddComponent<CircleCollider2D>();
                _unitCollider.isTrigger = true;
                _unitCollider.radius = 0.42f;
            }

            _visualRoot = new GameObject("VisualRoot").transform;
            _visualRoot.SetParent(transform, false);

            // 1. Ground Shadow Oval (Layering: Z-order 9 underneath unit)
            GameObject wLeftGo = new GameObject("GroundShadow");
            wLeftGo.transform.SetParent(_visualRoot, false);
            _weaponLeftRenderer = wLeftGo.AddComponent<SpriteRenderer>();
            _weaponLeftRenderer.sharedMaterial = spriteMat;
            _weaponLeftRenderer.sortingOrder = 9;

            // 2. Faction Energy Aura Ring (Layering: Z-order 10 under unit feet/hull)
            GameObject wRightGo = new GameObject("FactionAuraRing");
            wRightGo.transform.SetParent(_visualRoot, false);
            _weaponRightRenderer = wRightGo.AddComponent<SpriteRenderer>();
            _weaponRightRenderer.sharedMaterial = spriteMat;
            _weaponRightRenderer.sortingOrder = 10;

            // 3. Primary High-Detail 2D Unit Sprite (Layering: Z-order 12, unobstructed on top of aura/rings)
            GameObject bodyGo = new GameObject("BodyPrimary");
            bodyGo.transform.SetParent(_visualRoot, false);
            _bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            _bodyRenderer.sharedMaterial = spriteMat;
            _bodyRenderer.sortingOrder = 12;

            // 4. Secondary Under-Body Faction Glow (Layering: Z-order 10 behind body)
            GameObject secGo = new GameObject("BodySecondary");
            secGo.transform.SetParent(_visualRoot, false);
            _secondaryRenderer = secGo.AddComponent<SpriteRenderer>();
            _secondaryRenderer.sharedMaterial = spriteMat;
            _secondaryRenderer.sortingOrder = 10;

            // 5. Shield Ring (Layering: Z-order 11 behind body so it frames the unit without covering face/core)
            GameObject shieldGo = new GameObject("ShieldRing");
            shieldGo.transform.SetParent(_visualRoot, false);
            _shieldRingRenderer = shieldGo.AddComponent<SpriteRenderer>();
            _shieldRingRenderer.sharedMaterial = spriteMat;
            _shieldRingRenderer.sprite = Palette.GetRingSprite(64, 5);
            _shieldRingRenderer.color = Palette.WithAlpha(Palette.AccentGold, 0.55f);
            _shieldRingRenderer.sortingOrder = 11;
            shieldGo.SetActive(false);

            // 6. Viewer Avatar Badge above unit
            GameObject avatarFrameGo = new GameObject("AvatarFrame");
            avatarFrameGo.transform.SetParent(transform, false);
            avatarFrameGo.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            avatarFrameGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            _avatarFrameRenderer = avatarFrameGo.AddComponent<SpriteRenderer>();
            _avatarFrameRenderer.sharedMaterial = spriteMat;
            _avatarFrameRenderer.sprite = Palette.GetCircleSprite(64, 0);
            _avatarFrameRenderer.sortingOrder = 14;

            GameObject avatarGo = new GameObject("ViewerAvatar");
            avatarGo.transform.SetParent(avatarFrameGo.transform, false);
            avatarGo.transform.localScale = new Vector3(0.86f, 0.86f, 1f);
            _avatarRenderer = avatarGo.AddComponent<SpriteRenderer>();
            _avatarRenderer.sharedMaterial = spriteMat;
            _avatarRenderer.sortingOrder = 15;

            // 7. HP Bar
            GameObject hpBgGo = new GameObject("HpBarBg");
            hpBgGo.transform.SetParent(transform, false);
            hpBgGo.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            hpBgGo.transform.localScale = new Vector3(0.75f, 0.10f, 1f);
            _hpBarBgTransform = hpBgGo.transform;
            SpriteRenderer hpBg = hpBgGo.AddComponent<SpriteRenderer>();
            hpBg.sharedMaterial = spriteMat;
            hpBg.sprite = Palette.GetSquareSprite();
            hpBg.color = Palette.ShadowDark;
            hpBg.sortingOrder = 13;

            GameObject hpFillGo = new GameObject("HpBarFill");
            hpFillGo.transform.SetParent(hpBgGo.transform, false);
            hpFillGo.transform.localScale = new Vector3(0.94f, 0.72f, 1f);
            _hpBarFillRenderer = hpFillGo.AddComponent<SpriteRenderer>();
            _hpBarFillRenderer.sharedMaterial = spriteMat;
            _hpBarFillRenderer.sprite = Palette.GetSquareSprite();
            _hpBarFillRenderer.sortingOrder = 14;

            // 7. Viewer Nameplate
            GameObject nameGo = new GameObject("ViewerNameLabel");
            nameGo.transform.SetParent(transform, false);
            nameGo.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            _nameLabel = nameGo.AddComponent<TextMeshPro>();
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.fontSize = 2.1f;
            _nameLabel.fontStyle = FontStyles.Bold;
            _nameLabel.characterSpacing = 2f;
            _nameLabel.color = Palette.NeutralLight;
            if (Application.isPlaying && _nameLabel.fontSharedMaterial != null)
            {
                _nameLabel.outlineWidth = 0.22f;
                _nameLabel.outlineColor = Palette.ShadowDark;
            }
            _nameLabel.sortingOrder = 16;
        }

        public void Initialize(int factionId, UnitTier tier, string userId, string nickname, Sprite avatarSprite)
        {
            BuildVisualHierarchyIfNeeded();

            FactionId = factionId;
            Tier = tier;
            OwnerUserId = userId;
            OwnerNickname = string.IsNullOrEmpty(nickname) ? "Viewer" : nickname;
            _animSeed = Random.Range(0f, 10f);
            _attackCooldown = 0.1f;
            ShieldTimer = 0f;
            _hitPopTimer = 0f;

            Color primary = Palette.GetFactionPrimary(factionId);
            Color secondary = Palette.GetFactionSecondary(factionId);
            float dirSign = factionId == 1 ? -1f : 1f; // Top moves down (-Y), Bottom moves up (+Y)
            _attackAnimTimer = 0f;

            _bodyRenderer.sprite = Palette.GetUnitSprite(tier);
            _weaponLeftRenderer.sprite = Palette.GetCircleSprite(64, 0);   // Ground shadow
            _weaponRightRenderer.sprite = Palette.GetRingSprite(64, 7);    // Faction aura ring
            _secondaryRenderer.sprite = Palette.GetCircleSprite(64, 0);    // Soft under-body glow
            if (_shieldRingRenderer != null) _shieldRingRenderer.gameObject.SetActive(false);

            float topBadgeY = 0.58f;
            float bottomBarY = -0.45f;

            switch (tier)
            {
                case UnitTier.Infantry:
                case UnitTier.Squad:
                    MaxHp = 65f;
                    AttackDamage = 14f;
                    AttackRange = 0.75f;
                    AttackInterval = 0.65f;
                    MoveSpeed = 1.65f;
                    _baseScale = new Vector3(0.68f, 0.68f, 1f);
                    topBadgeY = 0.56f;
                    bottomBarY = -0.44f;
                    if (_unitCollider != null) _unitCollider.radius = 0.34f;
                    // 3/4 view infantry stays upright and flips X per faction for opposing stance
                    _bodyRenderer.flipX = (factionId == 1);
                    _bodyRenderer.flipY = false;
                    _weaponLeftRenderer.transform.localPosition = new Vector3(0f, -0.34f, 0f);
                    _weaponLeftRenderer.transform.localScale = new Vector3(0.75f, 0.32f, 1f);
                    _weaponRightRenderer.transform.localPosition = new Vector3(0f, -0.28f, 0f);
                    _weaponRightRenderer.transform.localScale = new Vector3(0.86f, 0.42f, 1f);
                    _secondaryRenderer.transform.localPosition = Vector3.zero;
                    _secondaryRenderer.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                    if (_shieldRingRenderer != null) _shieldRingRenderer.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                    break;

                case UnitTier.Tank:
                    MaxHp = 340f;
                    AttackDamage = 48f;
                    AttackRange = 1.85f;
                    AttackInterval = 1.05f;
                    MoveSpeed = 1.05f;
                    _baseScale = new Vector3(0.76f, 0.76f, 1f);
                    topBadgeY = 0.78f;
                    bottomBarY = -0.68f;
                    if (_unitCollider != null) _unitCollider.radius = 0.52f;
                    // Pure top-down hover tank points barrels toward enemy frontline
                    _bodyRenderer.flipX = false;
                    _bodyRenderer.flipY = (factionId == 1);
                    _weaponLeftRenderer.transform.localPosition = new Vector3(0f, -0.14f, 0f);
                    _weaponLeftRenderer.transform.localScale = new Vector3(1.22f, 0.92f, 1f);
                    _weaponRightRenderer.transform.localPosition = Vector3.zero;
                    _weaponRightRenderer.transform.localScale = new Vector3(1.36f, 1.36f, 1f);
                    _secondaryRenderer.transform.localPosition = Vector3.zero;
                    _secondaryRenderer.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                    if (_shieldRingRenderer != null) _shieldRingRenderer.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                    break;

                case UnitTier.Mecha:
                    MaxHp = 950f;
                    AttackDamage = 115f;
                    AttackRange = 2.25f;
                    AttackInterval = 0.75f;
                    MoveSpeed = 0.90f;
                    _baseScale = new Vector3(0.82f, 0.82f, 1f);
                    topBadgeY = 1.02f;
                    bottomBarY = -0.88f;
                    if (_unitCollider != null) _unitCollider.radius = 0.65f;
                    // 3/4 view giant Mecha stays upright with opposing blade stance
                    _bodyRenderer.flipX = (factionId == 1);
                    _bodyRenderer.flipY = false;
                    _weaponLeftRenderer.transform.localPosition = new Vector3(0f, -0.62f, 0f);
                    _weaponLeftRenderer.transform.localScale = new Vector3(1.42f, 0.52f, 1f);
                    _weaponRightRenderer.transform.localPosition = new Vector3(0f, -0.50f, 0f);
                    _weaponRightRenderer.transform.localScale = new Vector3(1.62f, 0.68f, 1f);
                    _secondaryRenderer.transform.localPosition = Vector3.zero;
                    _secondaryRenderer.transform.localScale = new Vector3(1.55f, 1.55f, 1f);
                    if (_shieldRingRenderer != null) _shieldRingRenderer.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
                    break;

                case UnitTier.Dragon:
                    MaxHp = 2200f;
                    AttackDamage = 260f;
                    AttackRange = 2.65f;
                    AttackInterval = 0.60f;
                    MoveSpeed = 1.25f;
                    _baseScale = new Vector3(0.98f, 0.98f, 1f);
                    topBadgeY = 1.08f;
                    bottomBarY = -0.96f;
                    if (_unitCollider != null) _unitCollider.radius = 0.78f;
                    // Top-down Cyber Dragon points head & wings toward enemy frontline
                    _bodyRenderer.flipX = false;
                    _bodyRenderer.flipY = (factionId == 1);
                    _weaponLeftRenderer.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                    _weaponLeftRenderer.transform.localScale = new Vector3(1.65f, 1.15f, 1f);
                    _weaponRightRenderer.transform.localPosition = Vector3.zero;
                    _weaponRightRenderer.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
                    _secondaryRenderer.transform.localPosition = Vector3.zero;
                    _secondaryRenderer.transform.localScale = new Vector3(1.70f, 1.70f, 1f);
                    if (_shieldRingRenderer != null) _shieldRingRenderer.transform.localScale = new Vector3(2.05f, 2.05f, 1f);
                    break;
            }

            if (_avatarFrameRenderer != null)
                _avatarFrameRenderer.transform.localPosition = new Vector3(0f, topBadgeY, 0f);
            if (_nameLabel != null)
                _nameLabel.transform.localPosition = new Vector3(0f, topBadgeY + 0.34f, 0f);
            if (_hpBarBgTransform != null)
                _hpBarBgTransform.localPosition = new Vector3(0f, bottomBarY, 0f);

            CurrentHp = MaxHp;
            _visualRoot.localScale = _baseScale;
            _visualRoot.localPosition = Vector3.zero;
            // Tint white-gold sprite subtly with faction color while keeping bright highlights
            _bodyRenderer.color = Color.Lerp(Palette.PureTint, secondary, 0.25f);
            _weaponLeftRenderer.color = Palette.WithAlpha(Palette.ShadowDark, 0.58f);
            _weaponRightRenderer.color = Palette.WithAlpha(primary, 0.88f);
            _secondaryRenderer.color = Palette.WithAlpha(primary, 0.24f);

            _avatarFrameRenderer.color = tier >= UnitTier.Tank ? Palette.AccentGold : primary;
            _avatarRenderer.sprite = avatarSprite != null ? avatarSprite : Palette.GetCircleSprite(64, 2);
            _avatarRenderer.color = Palette.PureTint;

            _hpBarFillRenderer.color = primary;
            UpdateHpBar();

            string shortName = OwnerNickname.Length > 11 ? OwnerNickname.Substring(0, 11) : OwnerNickname;
            _nameLabel.text = tier >= UnitTier.Tank ? $"[VIP] {shortName}" : shortName;
            _nameLabel.color = tier >= UnitTier.Tank ? Palette.AccentGold : Palette.NeutralLight;
        }

        public void SetAvatarSprite(Sprite s)
        {
            if (_avatarRenderer != null && s != null)
            {
                _avatarRenderer.sprite = s;
            }
        }

        public void ApplyPowerBuff(float bonusHp, float bonusAttack)
        {
            MaxHp += bonusHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + bonusHp);
            AttackDamage += bonusAttack;
            _hitPopTimer = 0.18f;
            UpdateHpBar();
        }

        public void GrantShield(float seconds)
        {
            ShieldTimer = Mathf.Max(ShieldTimer, seconds);
            if (_shieldRingRenderer != null)
                _shieldRingRenderer.gameObject.SetActive(true);
        }

        public void TickUnit(float dt, FactionWarManager manager)
        {
            if (!IsAlive) return;

            if (ShieldTimer > 0f)
            {
                ShieldTimer -= dt;
                if (_shieldRingRenderer != null)
                {
                    _shieldRingRenderer.transform.Rotate(0f, 0f, 120f * dt);
                    if (ShieldTimer <= 0f) _shieldRingRenderer.gameObject.SetActive(false);
                }
            }

            // 2d-games/SKILL.md Animation Principles:
            // 1. Idle/Walk breathing sine bob
            // 2. Squash & stretch on impact (_hitPopTimer)
            // 3. Anticipation + Lunge + Follow-through on attack (_attackAnimTimer)
            float bobPhase = (Time.time + _animSeed) * (MoveSpeed * 3.8f);
            float scaleX = _baseScale.x * (1f + Mathf.Sin(bobPhase) * 0.045f);
            float scaleY = _baseScale.y / (1f + Mathf.Sin(bobPhase) * 0.045f);
            float tiltDeg = Mathf.Cos(bobPhase * 0.5f) * 4.5f;
            float lungeOffsetY = 0f;
            float dirSign = FactionId == 1 ? -1f : 1f;

            if (_attackAnimTimer > 0f)
            {
                _attackAnimTimer -= dt;
                float norm = 1f - Mathf.Clamp01(_attackAnimTimer / 0.24f);
                if (norm < 0.30f)
                {
                    // Anticipation: wind-up pull back + squash
                    float t = norm / 0.30f;
                    lungeOffsetY = -dirSign * 0.10f * t;
                    scaleX *= Mathf.Lerp(1f, 1.14f, t);
                    scaleY *= Mathf.Lerp(1f, 0.86f, t);
                }
                else if (norm < 0.65f)
                {
                    // Strike lunge: stretch forward
                    float t = (norm - 0.30f) / 0.35f;
                    lungeOffsetY = dirSign * Mathf.Lerp(-0.10f, 0.22f, t);
                    scaleX *= Mathf.Lerp(1.14f, 0.86f, t);
                    scaleY *= Mathf.Lerp(0.86f, 1.22f, t);
                }
                else
                {
                    // Follow-through: elastic recovery
                    float t = (norm - 0.65f) / 0.35f;
                    lungeOffsetY = dirSign * Mathf.Lerp(0.22f, 0f, Ease.OutBack(t));
                    scaleX *= Mathf.Lerp(0.86f, 1f, t);
                    scaleY *= Mathf.Lerp(1.22f, 1f, t);
                }
            }

            if (_hitPopTimer > 0f)
            {
                _hitPopTimer -= dt;
                scaleX *= 1.18f;
                scaleY *= 0.85f;
            }

            _visualRoot.localPosition = new Vector3(0f, lungeOffsetY, 0f);
            _visualRoot.localScale = new Vector3(scaleX, scaleY, 1f);
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);

            _attackCooldown -= dt;

            // Find nearest enemy unit or enemy base
            FactionUnit targetUnit = manager.FindNearestEnemy(this, AttackRange);
            float enemyBaseY = FactionId == 1 ? FactionWarManager.BottomBaseY : FactionWarManager.TopBaseY;
            float distToBase = Mathf.Abs(transform.position.y - enemyBaseY);

            if (targetUnit != null)
            {
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = AttackInterval;
                    _attackAnimTimer = 0.24f;
                    PerformAttackOnUnit(targetUnit, manager);
                }
            }
            else if (distToBase <= AttackRange + 0.55f)
            {
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = AttackInterval;
                    _attackAnimTimer = 0.24f;
                    manager.DamageBase(FactionId == 1 ? 2 : 1, AttackDamage, OwnerUserId, OwnerNickname, FactionId);
                    _hitPopTimer = 0.12f;
                }
            }
            else
            {
                float dir = FactionId == 1 ? -1f : 1f;
                Vector3 pos = transform.position;
                pos.y += dir * MoveSpeed * dt;
                pos.x = Mathf.Clamp(pos.x, -3.4f, 3.4f);
                transform.position = pos;
            }
        }

        private void PerformAttackOnUnit(FactionUnit target, FactionWarManager manager)
        {
            _hitPopTimer = 0.12f;
            bool isAoE = Tier >= UnitTier.Tank;

            if (isAoE)
            {
                manager.DamageEnemiesInRadius(target.transform.position, FactionId, AttackDamage, 1.15f, OwnerUserId, OwnerNickname);
                if (VfxPool.Instance != null)
                {
                    VfxPool.Instance.PlayBurst(target.transform.position, Palette.GetFactionPrimary(FactionId), 10, 1.1f);
                }
                if (GameAudio.Instance != null)
                {
                    GameAudio.Instance.PlayImpactHit();
                }
            }
            else
            {
                target.TakeDamage(AttackDamage);
                manager.RecordContribution(OwnerUserId, OwnerNickname, FactionId, Mathf.RoundToInt(AttackDamage));
                if (VfxPool.Instance != null)
                {
                    VfxPool.Instance.PlayBurst(target.transform.position, Palette.GetFactionSecondary(FactionId), 5, 0.7f);
                }
            }
        }

        public bool TakeDamage(float dmg)
        {
            if (!IsAlive) return false;
            if (ShieldTimer > 0f) dmg *= 0.25f;

            CurrentHp -= dmg;
            _hitPopTimer = 0.1f;
            UpdateHpBar();

            return CurrentHp <= 0f;
        }

        private void UpdateHpBar()
        {
            if (_hpBarFillRenderer == null) return;
            float ratio = Mathf.Clamp01(CurrentHp / Mathf.Max(1f, MaxHp));
            _hpBarFillRenderer.transform.localScale = new Vector3(0.94f * ratio, 0.72f, 1f);
            _hpBarFillRenderer.transform.localPosition = new Vector3(-0.47f * (1f - ratio), 0f, 0f);
        }
    }
}
