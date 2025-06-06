using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public class AnimationControl : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] TMP_Text animText;
    [SerializeField] PlayableDirector playableDirector;


    private IEnumerator Start()
    {
        playableDirector.Play();

        animText.text = "IDLE";

        yield return new WaitForSeconds(7f);

        // playableDirector.Stop();
        // playableDirector.Play();

        animText.text = "RUN";
        animator.SetBool("move", true);

        yield return new WaitForSeconds(7f);

        // playableDirector.Stop();
        // playableDirector.Play();

        animator.SetBool("move", false);
        animText.text = "SHOOT";

        for (int i = 0; i < 7; i++)
        {
            animator.Play("Shoot");

            yield return new WaitForSeconds(2.25f);
        }

        yield return new WaitForSeconds(1f);

        // playableDirector.Stop();
        // playableDirector.Play();

        animText.text = "TAKE DAMAGE";

        for (int i = 0; i < 7; i++)
        {
            animator.Play("TakingDamage");

            yield return new WaitForSeconds(2f);
        }

        yield return new WaitForSeconds(1f);

        // playableDirector.Stop();
        // playableDirector.Play();

        animText.text = "DIE";

        for (int i = 0; i < 7; i++)
        {
            animator.Play("Die");

            yield return new WaitForSeconds(2f);
        }

        yield return new WaitForSeconds(1f);

        playableDirector.Stop();
    }
}
