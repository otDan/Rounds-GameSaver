using UnityEngine;

namespace GameSaver.Component;

public class AnimationAutoDestroy : MonoBehaviour
{
    private Animator _animator;
    private int _nextUpdate = 1;
    private int _play;

    private void Start () {
        _animator = GetComponentInChildren<Animator>();
        _play = (int) _animator.GetCurrentAnimatorStateInfo(0).length;
    }

    private void Update()
    {
        if (!(Time.time >= _nextUpdate)) return;
        _nextUpdate = Mathf.FloorToInt(Time.time) + 1;
        UpdateSecond();
    }

    private void UpdateSecond()
    {
        if (!(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1) || _animator.IsInTransition(0)) return;
        if (_play > 0)
        {
            _play--;
            return;
        }
        gameObject.SetActive(false);
        Destroy(this);
    }
}