using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private string upgradeId;

    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button buyButton;

    private void Start()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(BuyUpgrade);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDataChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDataChanged -= Refresh;
        }
    }

    private void BuyUpgrade()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.BuyUpgrade(upgradeId);
        Refresh();
    }

    private void Refresh()
    {
        if (GameManager.Instance == null)
            return;

        var def = GameManager.Instance.GetUpgradeDefinition(upgradeId);

        if (def == null)
        {
            Debug.LogError($"UpgradeDefinition not found for id: {upgradeId}");
            return;
        }

        int level = GameManager.Instance.GetUpgradeLevel(upgradeId);
        double cost = GameManager.Instance.GetUpgradeCost(upgradeId);

        if (titleText != null)
            titleText.text = def.displayName;

        if (levelText != null)
            levelText.text = $"Lv. {level}";

        if (costText != null)
            costText.text = $"Kosten: {GameManager.FormatNumber(cost)}";

        if (descText != null)
            descText.text = def.description;

        if (buyButton != null)
            buyButton.interactable = GameManager.Instance.CanAffordUpgrade(upgradeId);
    }
}
