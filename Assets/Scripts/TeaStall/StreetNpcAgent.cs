using System.Collections;
using TMPro;
using UnityEngine;
using TikTokFactionWar;

namespace TikTokTeaStall
{
    /// <summary>
    /// Điều khiển NPC Bà Tám Trà Đá (quầy chính, quạt nan, bưng trà, đối đáp cực duyên) 
    /// và 6 NPC Đường Phố Việt Nam (Grab, Vé số, Bánh mì, Đánh giày, Loa kẹo kéo, Trật tự phường).
    /// </summary>
    public class StreetNpcAgent : MonoBehaviour
    {
        public bool IsTeaLady = false;
        public StreetNpcType NpcType;

        // Visual Components
        private SpriteRenderer _bodySr;
        private SpriteRenderer _propSr;       // Quạt nan, Xấp vé số, Thùng đánh giày, Loa kẹo kéo...
        private SpriteRenderer _vehicleSr;    // Xe máy Grab, Xe đẩy bánh mì...

        // Bong bóng thoại NPC
        private Transform _speechBubbleRoot;
        private SpriteRenderer _speechBubbleSr;
        private TextMeshPro _speechTmp;
        private Coroutine _speechCoroutine;

        // Vị trí gốc (cho Bà Tám)
        private Vector3 _homeStallPosition;
        private bool _isServing = false;

        private void Awake()
        {
            BuildNpcHierarchy();
        }

        private void BuildNpcHierarchy()
        {
            // Xe hoặc phương tiện (nếu có)
            GameObject vehGo = new GameObject("VehicleVisual");
            vehGo.transform.SetParent(transform, false);
            vehGo.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            _vehicleSr = vehGo.AddComponent<SpriteRenderer>();
            _vehicleSr.sortingOrder = 14;
            _vehicleSr.gameObject.SetActive(false);

            // Thân NPC
            GameObject bodyGo = new GameObject("BodyVisual");
            bodyGo.transform.SetParent(transform, false);
            _bodySr = bodyGo.AddComponent<SpriteRenderer>();
            _bodySr.sortingOrder = 16;

            // Đạo cụ cầm tay (Quạt nan, xấp vé số, loa kẹo kéo...)
            GameObject propGo = new GameObject("PropVisual");
            propGo.transform.SetParent(transform, false);
            propGo.transform.localPosition = new Vector3(0.35f, 0.05f, 0f);
            _propSr = propGo.AddComponent<SpriteRenderer>();
            _propSr.sortingOrder = 18;

            // Bong bóng thoại
            GameObject bubRoot = new GameObject("NpcSpeechBubble");
            bubRoot.transform.SetParent(transform, false);
            bubRoot.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            _speechBubbleRoot = bubRoot.transform;

            GameObject bubBg = new GameObject("BubbleBg");
            bubBg.transform.SetParent(_speechBubbleRoot, false);
            bubBg.transform.localScale = new Vector3(3.2f, 1.1f, 1f);
            _speechBubbleSr = bubBg.AddComponent<SpriteRenderer>();
            _speechBubbleSr.sortingOrder = 45;
            _speechBubbleSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            _speechBubbleSr.color = new Color(1.0f, 0.95f, 0.85f); // Màu vàng cam ấm áp

            GameObject bubTxt = new GameObject("BubbleText");
            bubTxt.transform.SetParent(_speechBubbleRoot, false);
            bubTxt.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _speechTmp = bubTxt.AddComponent<TextMeshPro>();
            _speechTmp.alignment = TextAlignmentOptions.Center;
            _speechTmp.fontSize = 2.5f;
            _speechTmp.color = new Color(0.12f, 0.10f, 0.12f);
            _speechTmp.fontStyle = FontStyles.Bold;
            _speechTmp.sortingOrder = 46;
            _speechTmp.rectTransform.sizeDelta = new Vector2(3.0f, 1.0f);
            _speechTmp.text = "";

            _speechBubbleRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Khởi tạo NPC Bà Tám Trà Đá tại quầy trà chính.
        /// </summary>
        public void SetupAsTeaLady(Vector3 stallPos)
        {
            IsTeaLady = true;
            _homeStallPosition = stallPos;
            transform.position = stallPos;

            _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_ba_tam", FallbackSpriteType.TeaLadyAvatar);
            _bodySr.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            // Quạt nan phe phẩy
            _propSr.sprite = TeaStallPalette.GetFallbackSprite(FallbackSpriteType.RoundedRect);
            _propSr.color = new Color(0.85f, 0.72f, 0.45f); // Quạt nan vàng
            _propSr.transform.localScale = new Vector3(0.3f, 0.45f, 1f);

            // Đổi màu bong bóng thoại Bà Tám thành Vàng Cam rực rỡ đặc trưng
            _speechBubbleSr.color = new Color(1.0f, 0.90f, 0.65f);
            _speechBubbleRoot.localPosition = new Vector3(0f, 2.0f, 0f);

            StartCoroutine(TeaLadyIdleAnimation());
        }

        /// <summary>
        /// Khởi tạo NPC Đường Phố vãng lai.
        /// </summary>
        public void SetupAsStreetNpc(StreetNpcType type)
        {
            IsTeaLady = false;
            NpcType = type;

            switch (type)
            {
                case StreetNpcType.GrabDriver:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_grab_driver", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                    break;

                case StreetNpcType.LotteryVendor:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_lottery_lady", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
                    break;

                case StreetNpcType.BreadVendor:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_banh_mi_cart", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                    break;

                case StreetNpcType.ShoeShiner:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("char_tier1_toong", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                    break;

                case StreetNpcType.StreetSinger:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_keo_keo_singer", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                    break;

                case StreetNpcType.WardPatrol:
                    _bodySr.sprite = TeaStallPalette.GetOrLoadSprite("npc_trat_tu_phuong", FallbackSpriteType.Circle);
                    _bodySr.color = Color.white;
                    _bodySr.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                    break;
            }
                    _propSr.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
                    break;
            }
        }

        /// <summary>
        /// Hiển thị lời thoại của NPC trong bong bóng nổi bật
        /// </summary>
        public void Say(string text, float duration = 5.0f)
        {
            if (_speechCoroutine != null) StopCoroutine(_speechCoroutine);
            _speechCoroutine = StartCoroutine(SpeechCoroutine(text, duration));
        }

        private IEnumerator SpeechCoroutine(string text, float duration)
        {
            _speechTmp.text = text;
            _speechBubbleRoot.gameObject.SetActive(true);

            if (TeaStallAudio.Instance != null) TeaStallAudio.Instance.PlayChatPop();

            // Nảy lên
            float elapsed = 0f;
            float popDur = 0.25f;
            _speechBubbleRoot.localScale = Vector3.zero;

            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / popDur;
                float s = Ease.OutBack(progress);
                _speechBubbleRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _speechBubbleRoot.localScale = Vector3.one;

            yield return new WaitForSeconds(duration);

            // Thu nhỏ ẩn đi
            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float s = 1f - Mathf.Clamp01(elapsed / 0.2f);
                _speechBubbleRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            _speechBubbleRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Hoạt ảnh nhàn rỗi của Bà Tám: Quạt nan phe phẩy mát rượi
        /// </summary>
        private IEnumerator TeaLadyIdleAnimation()
        {
            while (true)
            {
                if (!_isServing)
                {
                    // Phe phẩy quạt nan
                    float fanAngle = Mathf.Sin(Time.time * 4f) * 22f;
                    _propSr.transform.localRotation = Quaternion.Euler(0f, 0f, fanAngle);

                    // Thân nhún nhảy nhẹ
                    float bob = Mathf.Sin(Time.time * 2f) * 0.02f;
                    _bodySr.transform.localPosition = new Vector3(0f, bob, 0f);
                }
                yield return null;
            }
        }

        /// <summary>
        /// Bà Tám chạy lon ton ra bưng trà đá hoặc đĩa nem chua tới bàn của khách rồi quay về quầy
        /// </summary>
        public void ServeItemToCustomer(Vector3 tablePos, string itemName)
        {
            if (_isServing) return;
            StartCoroutine(ServeCustomerCoroutine(tablePos, itemName));
        }

        private IEnumerator ServeCustomerCoroutine(Vector3 tablePos, string itemName)
        {
            _isServing = true;
            Say($"Bà Tám mang ngay {itemName} tới đây!", 3.0f);

            // 1. Chạy tới bàn
            yield return StartCoroutine(MoveTo(tablePos + new Vector3(0.5f, 0f, 0f), 0.7f));
            yield return new WaitForSeconds(0.4f);

            // 2. Chạy về quầy
            yield return StartCoroutine(MoveTo(_homeStallPosition, 0.7f));
            _isServing = false;
        }

        /// <summary>
        /// Kịch bản đi ngang vỉa hè của NPC Đường Phố: Đi từ mép đường, dừng trước quán tương tác, rồi đi tiếp
        /// </summary>
        public void WalkAcrossStreet(Vector3 startPos, Vector3 stallStopPos, Vector3 exitPos, System.Action onComplete = null)
        {
            StartCoroutine(WalkAcrossStreetCoroutine(startPos, stallStopPos, exitPos, onComplete));
        }

        private IEnumerator WalkAcrossStreetCoroutine(Vector3 startPos, Vector3 stallStopPos, Vector3 exitPos, System.Action onComplete)
        {
            transform.position = startPos;

            // 1. Đi tới trước quán trà đá
            yield return StartCoroutine(MoveTo(stallStopPos, 2.5f));

            // 2. Dừng lại giao lưu tung thoại đặc trưng & kích hoạt âm thanh
            TriggerStreetNpcInteraction();
            yield return new WaitForSeconds(4.5f);

            // 3. Tiếp tục đi ra khỏi màn hình
            yield return StartCoroutine(MoveTo(exitPos, 2.5f));

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void TriggerStreetNpcInteraction()
        {
            string line = TeaLadyDialogueBank.GetStreetNpcLine(NpcType);
            Say(line, 4.0f);

            if (TeaStallAudio.Instance == null) return;

            switch (NpcType)
            {
                case StreetNpcType.GrabDriver:
                    TeaStallAudio.Instance.PlayGrabTingTing();
                    break;
                case StreetNpcType.BreadVendor:
                    TeaStallAudio.Instance.PlayBanhMiCry();
                    break;
                case StreetNpcType.WardPatrol:
                    TeaStallAudio.Instance.PlayPoliceWhistle();
                    // Kích hoạt toàn quán ôm ghế chạy
                    if (TeaStallManager.Instance != null)
                    {
                        TeaStallManager.Instance.TriggerPatrolPanicEvent();
                    }
                    break;
                case StreetNpcType.StreetSinger:
                    // Bật beat vũ trường kẹo kéo
                    if (TeaStallManager.Instance != null)
                    {
                        TeaStallManager.Instance.TriggerKeoKeoPartyEvent();
                    }
                    break;
            }
        }

        private IEnumerator MoveTo(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Nhún nhảy bước đi
                float stepHop = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.08f;
                transform.position = Vector3.Lerp(start, target, t) + new Vector3(0f, stepHop, 0f);
                yield return null;
            }
            transform.position = target;
        }
    }
}
