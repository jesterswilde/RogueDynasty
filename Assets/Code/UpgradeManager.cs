using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class UpgradeManager : MonoBehaviour {
    static UpgradeManager t;
    [SerializeField]
    float _initialPlayerSpeed = 0.8f;
    [SerializeField]
    float _perLevelHealthIncrease = 5;
    public float PerLevelhealthIncrease => _perLevelHealthIncrease;
    [SerializeField]
    float _attackSpeedup = 0.05f;
    public float AttackSpeedup => _attackSpeedup;
    public static UpgradeManager T => t;

    public List<UpgradeChoice> GetChoices() {
        HashSet<string> pickedChoices = new();
        List<UpgradeChoice> choices = new();
        while(choices.Count < 3) {
            var i = UnityEngine.Random.Range(0, allUpgardes.Count);
            var option = allUpgardes[i];
            if (!pickedChoices.Contains(option.ID)) {
                pickedChoices.Add(option.ID);
                choices.Add(option);
            }
        }
        return choices;
    }

    List<UpgradeChoice> allUpgardes = new() {
        new() {
            ID =  "PowerIncreaseI",
            Title = "Power Increase I",
            Text = "Increases Damage of all attacks by 5",
            OnPurchase = (player) => {
                player.GetComponentInChildren<Weapon>().DamageIncrease += 5;
            },
            Repeatable = true
        },
        new() {
            ID = "SpeedIncreaseI",
            Title = "Speed Increase I",
            Text = "Increase your speed by 1",
            OnPurchase = (player)=> player.GetComponent<Character>().Speed += 1,
            Repeatable = true
        },
        new() {
            ID = "VampirismI",
            Title = "Vampirism",
            Text = "Drink the spilled essence of the fallen. Heal 3 health per kill.",
            RequiredIDs = new(){"SpeedIncreaseI" },
            OnPurchase = (player)=> GameManager.T.OnKillHealthGain += 3,
            Repeatable = true
        },
        new() {
            ID = "AttackSpeedI",
            Title = "Attack Speed Increase",
            Text = "Increase your attack speed",
            OnPurchase = (player)=> player.GetComponentInChildren<Attacker>().AttackSpeedMod += 0.2f,
            Repeatable = true
        },
        new() {
            ID = "HPBoost",
            Title = "Health Boost",
            Text = "Gain 20 HP",
            OnPurchase = (player)=> player.Char.MaxHealth += 20,
            Repeatable = true
        },
        new() {
            ID = "BiggerWeapon",
            Title = "Bigger Weapon",
            Text = "Increase your weapon's size.",
            OnPurchase = (player)=> player.GetComponentInChildren<Weapon>().WeaponXForm.localScale *= 1.2f,
            Repeatable = true
        },
        new() {
            ID = "CorpseDamage",
            Title = "Lethal Corpoe Boom",
            Text = "Your Corpse Explosion does more damage",
            OnPurchase = (player)=> GameManager.T.CorpseExplosionDamage += 3,
            Repeatable = true
        },
        new() {
            ID = "CorpseBoom",
            Title = "Bigger Corpoe Boom",
            Text = "Your Corpse Explose radius is bigger!",
            OnPurchase = (player)=> GameManager.T.CorpseExplosionRadius += 3,
            Repeatable = true
        }
    };

    void Start() {
        GameManager.T.Player.GetComponent<Attacker>().AttackSpeedMod = _initialPlayerSpeed;
    }
    void Awake() {
        if (t != null) {
            Destroy(this);
            Debug.LogWarning($"Multiple Upgrade Managers, one on {name}");
            return;
        }
        t = this;
    }
}
public class UpgradeChoice {
    public string ID;
    public string Title;
    public string Text;
    public Action<Player> OnPurchase;
    public List<string> RequiredIDs;
    public bool Repeatable = false;
}