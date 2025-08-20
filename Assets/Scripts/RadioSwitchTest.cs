using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class RadioSwitchTest : MonoBehaviour
{
    public AudioClip resource1;
    public AudioClip resource2;

    public AudioMixerGroup group1;
    public AudioMixerGroup group2;

    public AudioSource source;

    public float switchTime;

    private void Start()
    {
        StartCoroutine(SwitchLoop());
    }

    private IEnumerator SwitchLoop()
    {
        while (true) 
        {
            yield return new WaitForSeconds(switchTime);
            source.outputAudioMixerGroup = group1;
            source.PlayOneShot(resource2);

            yield return new WaitForSeconds(switchTime);
            source.outputAudioMixerGroup = group2;
            source.PlayOneShot(resource1);
        }
    }
}
