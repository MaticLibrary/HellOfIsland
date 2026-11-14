using UnityEngine;

public class Campfire : MonoBehaviour
{
    private bool isLit;
    private Renderer[] flameRenderers;
    private ParticleSystem[] fireSystems;
    private Light fireLight;

    public bool IsLit => isLit;

    private void Awake()
    {
        Transform flameRoot = transform.Find("CampfireFlame");
        flameRenderers = flameRoot != null ? flameRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        fireSystems = GetComponentsInChildren<ParticleSystem>(true);
        Transform lightTransform = transform.Find("CampfireGlow");
        fireLight = lightTransform != null ? lightTransform.GetComponent<Light>() : null;

        foreach (Renderer flameRenderer in flameRenderers)
            flameRenderer.enabled = false;

        foreach (ParticleSystem particles in fireSystems)
        {
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (fireLight != null)
            fireLight.enabled = false;

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    public void LightFire()
    {
        if (isLit)
            return;

        isLit = true;

        foreach (Renderer flameRenderer in flameRenderers)
            flameRenderer.enabled = true;

        foreach (ParticleSystem particles in fireSystems)
            particles.Play();

        if (fireLight != null)
            fireLight.enabled = true;

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
}
