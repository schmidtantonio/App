using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Top UI")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text passiveIncomeText;
    [SerializeField] private TMP_Text prestigeText;

    [Header("Match UI")]
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button clickBurstButton;
    [SerializeField] private Slider matchProgressSlider;
    [SerializeField] private TMP_Text matchRewardText;

    [Header("Prestige UI")]
    [SerializeField] private Button prestigeButton;
    [SerializeField] private TMP_Text prestigeRequirementText;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found in scene.");
            return;
        }

        GameManager.Instance.OnDataChanged += RefreshAll;
        GameManager.Instance.OnMatchProgressChanged += UpdateMatchProgress;
        GameManager.Instance.OnMatchStateChanged += UpdateMatchState;

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(GameManager.Instance.StartMatch);

        if (clickBurstButton != null)
            clickBurstButton.onClick.AddListener(GameManager.Instance.ClickBurst);

        if (prestigeButton != null)
            prestigeButton.onClick.AddListener(GameManager.Instance.PrestigeReset);

        RefreshAll();
        UpdateMatchState(GameManager.Instance.MatchRunning);
        UpdateMatchProgress(0f);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnDataChanged -= RefreshAll;
        GameManager.Instance.OnMatchProgressChanged -= UpdateMatchProgress;
        GameManager.Instance.OnMatchStateChanged -= UpdateMatchState;
    }

    private void RefreshAll()
    {
        var gm = GameManager.Instance;

        if (coinsText != null)
            coinsText.text = $"Coins: {GameManager.FormatNumber(gm.Coins)}";

        if (passiveIncomeText != null)
            passiveIncomeText.text = $"Passiv / s: {GameManager.FormatNumber(gm.GetPassiveIncomePerSecond())}";

        if (prestigeText != null)
            prestigeText.text = $"Prestige: {gm.PrestigeLevel} | x{gm.PrestigeMultiplier:0.00}";

        if (matchRewardText != null)
            matchRewardText.text = $"Match Reward: {GameManager.FormatNumber(gm.GetMatchReward())}";

        if (prestigeRequirementText != null)
            prestigeRequirementText.text = $"Prestige ab: {GameManager.FormatNumber(gm.GetPrestigeRequirement())}";

        if (prestigeButton != null)
            prestigeButton.interactable = gm.Coins >= gm.GetPrestigeRequirement();
    }

    private void UpdateMatchProgress(float progress)
    {
        if (matchProgressSlider != null)
            matchProgressSlider.value = progress;
    }

    private void UpdateMatchState(bool isRunning)
    {
        if (startMatchButton != null)
            startMatchButton.interactable = !isRunning;

        if (clickBurstButton != null)
            clickBurstButton.interactable = isRunning;
    }
}
