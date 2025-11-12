using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;

public class GameManager : MonoBehaviour {
    static GameManager t;
    public static GameManager T => t;
    [SerializeField]
    Config _config;
    [SerializeField]
    Material _innards;
    [SerializeField]
    LayerMask _enemiesMask;
    [SerializeField]
    List<AudioClip> _bgm;
    AudioSource _audio;
    public Material Innards => _innards;
    public Config Config => _config;
    Player _player;
    public Player Player => _player;
    Camera _camera;
    public Camera Cam => _camera;
    int _kill = 0;
    int _nextUpgradeI = 0;
    [SerializeField]
    List<int> _upgradeKils;
    public float CorpseExplosionDamage { get; set; }
    public float CorpseExplosionRadius { get; set; } = 3;
    public float OnKillHealthGain { get; set; } = 0;
    public void DiedAt(Vector3 pos) {
        var chars = GetCharactersInRadius(pos, CorpseExplosionRadius, _enemiesMask);
        foreach (var c in chars) {
            if (c.IsDead)
                continue;
            var deathInfo = c.GetBisection();
            var attack = new AttackData() {
                HitPlane = deathInfo.Plane,
                HitPosition = deathInfo.Center,
                Damage = CorpseExplosionDamage,
                HitDirection = Vector3.up,
                FromOrigin = c.transform.position
            };
            c.GotHitBy(attack);
        }
    }
    public int Kill {
        get => _kill; set {
            _kill = value;
            OnKill?.Invoke(_kill);
            _player.Char.ModifyHealth(OnKillHealthGain);
            if(_upgradeKils.Count > _nextUpgradeI && _upgradeKils[_nextUpgradeI] == _kill) {
                _nextUpgradeI++;
                _player.Char.MaxHealth += UpgradeManager.T.PerLevelhealthIncrease;
                _player.GetComponent<Attacker>().AttackSpeedMod += UpgradeManager.T.AttackSpeedup;
                var next = "MAX";
                if (_upgradeKils.Count > _nextUpgradeI)
                    next = _upgradeKils[_nextUpgradeI].ToString();
                UIManager.T.UpdateNextUpgrade(next);
                PickUpgrade();
            }
        }
    }
    public void PickUpgrade() {
        Time.timeScale = 0;
        var choices = UpgradeManager.T.GetChoices();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        UIManager.T.ShowUpgrades(choices, (c) => {
            c.OnPurchase(_player);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            UIManager.T.CloseScreen();
            UpgradeManager.T.PickedUpgrade(c.ID);
        });
    }
    public event Action<int> OnKill;
    public void PlayAudio(AudioClip _clip) {
        if (_clip == null)
            return;
        var go = new GameObject();
        var a = go.AddComponent<AudioSource>();
        a.clip = _clip;
        a.Play();
        go.AddComponent<SelfDeleteAudio>();
    }


    public static List<Character> GetCharactersInRadius(Vector3 position, float radius, LayerMask layerMask) {
        List<Character> characters = new List<Character>();
        Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);

        foreach (var collider in colliders) {
            // Try to get the Character component from each collider
            Character character = collider.GetComponent<Character>();
            if (character != null) {
                characters.Add(character);
            }
        }
        return characters;
    }

    IEnumerator PlayNextSong() {
        while (true) {
            var i = UnityEngine.Random.Range(0, _bgm.Count);
            var track = _bgm[i];
            _audio.clip = track;
            _audio.Play();
            yield return new WaitForSeconds(track.length);
        }
    }
    public bool IsStarting { get; set; } = true;
    IEnumerator ReturnControl() {
        yield return new WaitForSeconds(0.5f);
        IsStarting = false;
    }
    void Update() {
        if(IsStarting && Input.GetMouseButtonDown(0)) {
            UIManager.T.CloseScreen();
            StartCoroutine(ReturnControl());
        }        
    }
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(PlayNextSong());
        UIManager.T.UpdateNextUpgrade(_upgradeKils[0].ToString());
        UIManager.T.ShowStartGameScreen();
    }
    void Awake() {
        if (t != null) {
            Destroy(this);
            Debug.LogWarning($"Multiple Game Managers, one on {name}");
            return;
        }
        t = this;

        _audio = GetComponent<AudioSource>();
        _camera = FindFirstObjectByType<Camera>();
        _player = FindFirstObjectByType<Player>();
        _kill = 0;
    }
}
