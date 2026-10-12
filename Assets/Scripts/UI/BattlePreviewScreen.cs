using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePreviewScreen : MonoBehaviour
{
    [SerializeField] GameObject GraphicsParent;
    [SerializeField] TMP_Text headerText; //formatted as "Battle of ___"
    [SerializeField] Image attackerGeneral;
    [SerializeField] Image defenderGeneral;
    [SerializeField] TMP_Text atkGenName;
    [SerializeField] TMP_Text defGenName;
    [SerializeField] TMP_Text attackerManpower;
    [SerializeField] TMP_Text defenderManpower;

    [SerializeField] Button FightBattleButton;
    [SerializeField] Button AutoresolveButton;

    private void OnEnable()
    {
        FightBattleButton?.onClick.AddListener(OnFightBattle);
        AutoresolveButton?.onClick.AddListener(OnAutoresolveBattle);
    }

    private void OnDisable()
    {
        FightBattleButton?.onClick.RemoveListener(OnFightBattle);
        AutoresolveButton?.onClick.RemoveListener(OnAutoresolveBattle);
    }

    /// <summary>
    /// Populates the preview screen UI for the coming battle
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="defender"></param>
    /// <param name="currentRegion"></param>
    public void PopulatePreviewScreen(FieldArmy attacker, FieldArmy defender, Region currentRegion)
    {
        headerText.text = $"Battle of {currentRegion.RegionName}"; //add "siege of City" logic if this is a city assault

        attackerGeneral.sprite = attacker.General.noble.Portrait;
        defenderGeneral.sprite = defender.General.noble.Portrait;
        atkGenName.text = attacker.General.FullName;
        defGenName.text = defender.General.FullName;

        float attackerManpowerValue = attacker.units.Sum(unit => unit.UnitHealth);
        float defenderManpowerValue = defender.units.Sum(unit => unit.UnitHealth);
        attackerManpower.text = $"{attackerManpowerValue} men";
        defenderManpower.text = $"{defenderManpowerValue} men";
    }

    public void ToggleBattleUI(bool enable)
    {
        GraphicsParent.SetActive(enable);
    }

    private void OnFightBattle()
    {
        CampaignMapManager.i.FightPendingBattle();
    }

    private void OnAutoresolveBattle()
    {
        CampaignMapManager.i.AutoresolvePendingBattle();
    }
}