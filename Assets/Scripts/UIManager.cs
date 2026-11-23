using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private Image _timerImage;
    [SerializeField] private Sprite _timerOnImage;
    [SerializeField] private Sprite _timerOffImage;
    [SerializeField] private float _moneyAnimationDuration = 0.5f;

    private bool isBombActive;
    private Coroutine _moneyAnimation;
    private int _displayedMoney;
    private Animator animator;

    private void Awake()
    {
        _displayedMoney = 0;
        _moneyText.text = "0";
        animator = _timerImage.gameObject.GetComponent<Animator>();
    }
    public void Init()
    {
        _displayedMoney = 0;
        _moneyText.text = "0";
        isBombActive = true;
        _timerImage.sprite = _timerOnImage;
        animator.SetBool("Bomb",isBombActive);
    }
    public void BombStop()
    {
        _timerImage.sprite = _timerOffImage;
        isBombActive = false;
        animator.SetBool("Bomb", isBombActive);
    }

    public void UpdateTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        _timerText.text = $"{minutes:00}:{seconds:00}";
    }
    void Update()
    {
        if(isBombActive)
        {
            
        }
        else
        {
            _timerText.color = Color.white;
        }
    }

    public void UpdateMoney(int money)
    {
        if (_moneyAnimation != null)
        {
            StopCoroutine(_moneyAnimation);
        }

        _moneyAnimation = StartCoroutine(AnimateMoney(money));
    }

    private IEnumerator AnimateMoney(int targetMoney)
    {
        int startMoney = _displayedMoney;
        float elapsed = 0f;
        float duration = Mathf.Max(0f, _moneyAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            progress = 1f - Mathf.Pow(1f - progress, 3f);

            _displayedMoney = Mathf.RoundToInt(Mathf.Lerp(startMoney, targetMoney, progress));
            _moneyText.text = _displayedMoney.ToString();
            yield return null;
        }

        _displayedMoney = targetMoney;
        _moneyText.text = targetMoney.ToString();
        _moneyAnimation = null;
    }
}
