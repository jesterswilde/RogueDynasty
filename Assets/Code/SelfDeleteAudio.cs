using System.Collections;
using UnityEngine;

public class SelfDeleteAudio : MonoBehaviour {
    AudioSource _audio;

    IEnumerator DestroySelf() {
        yield return new WaitForSeconds(_audio.clip.length);
        Destroy(gameObject);
    }
    void Start() {
        _audio = GetComponent<AudioSource>();
        StartCoroutine(DestroySelf());
    }
}