using System.Collections;
using UnityEngine;

public class Baubble : MonoBehaviour {
    [Header("Motion Settings")]
    [SerializeField]
    AnimationCurve _bobCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    float _bobDuration = 1f;

    [SerializeField]
    float _bobHeight = 0.5f;

    [SerializeField]
    float _rotationSpeed = 20f;

    Vector3 _startPosition;

    IEnumerator Bob() {
        float timer = 0f;
        while (true) {
            timer += Time.deltaTime;
            if (timer > _bobDuration)
                timer -= _bobDuration;

            float normalizedTime = timer / _bobDuration;
            float curveValue = _bobCurve.Evaluate(normalizedTime);

            Vector3 pos = _startPosition;
            pos.y += curveValue * _bobHeight;
            transform.position = pos;

            // Rotate gently
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            yield return null;
        }
    }

    void Start() {
        _startPosition = transform.position;
        StartCoroutine(Bob());
    }
}
