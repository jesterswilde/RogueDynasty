using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

public class UpgradeManager : SerializedMonoBehaviour {
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
    HashSet<string> _takenUpgrades = new();
    [SerializeField]
    List<(string, AttackDesc)> _upgradeAttacks;

    public AttackDesc GetAttackByName(string name) {
        return _upgradeAttacks.First((a) => a.Item1 == name).Item2;
    }

    public List<UpgradeChoice> GetChoices() {
        HashSet<string> pickedChoices = new();
        List<UpgradeChoice> choices = new();
        while(choices.Count < 3) {
            var i = UnityEngine.Random.Range(0, allUpgardes.Count);
            var option = allUpgardes[i];
            if(option.RequiredIDs != null && option.RequiredIDs.Count > 0) {
                var meetsReqs = option.RequiredIDs.All((r) => _takenUpgrades.Contains(r));
                if (!meetsReqs)
                    continue;
            }
            if (!pickedChoices.Contains(option.ID)) {
                pickedChoices.Add(option.ID);
                choices.Add(option);
            }
        }
        return choices;
    }
    public void PickedUpgrade(string id) {
        _takenUpgrades.Add(id);
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
        },
        new() {
            ID = "BA3",
            Title = "Attack: Slam",
            Text = "Gain 3rd Attack in Basic Combo",
            OnPurchase = (player) => {
                var attack = T.GetAttackByName("b3");
                if(attack == null)
                    throw new Exception($"No attack named b3, need basic attack 3 ");
                var a = player.GetComponent<Attacker>().BasicAttacks;
                if(a.Count < 2)
                    throw new Exception("Can't add to a smaller list than 2");
                else if(a.Count == 2) {
                    a.Add(attack);
                }
                else {
                    a[2] = attack;
                }
            }
        },
        new() {
            ID = "BA4",
            Title = "Attack: Slam",
            Text = "Gain 4th Attack in Basic Combo",
            RequiredIDs = new(){"BA3" },
            OnPurchase = (player) => {
                var attack = T.GetAttackByName("b4");
                if(attack == null)
                    throw new Exception($"No attack named b4, need basic attack 4 ");
                var a = player.GetComponent<Attacker>().BasicAttacks;
                if(a.Count < 3)
                    throw new Exception("Can't add to a smaller list than 3");
                else if(a.Count == 3) {
                    a.Add(attack);
                }
                else {
                    a[3] = attack;
                }
            }
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