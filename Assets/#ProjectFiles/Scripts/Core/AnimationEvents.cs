using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public void Move() { AudioManager.Instance.PlaySfx(SoundEffect.Move, transform.position, 0.25f); }

    public void Reload() { AudioManager.Instance.PlaySfx(SoundEffect.Reload, transform.position, 0.75f); }
}
