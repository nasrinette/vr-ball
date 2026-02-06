using UnityEngine;

public class WinEffects : MonoBehaviour
{
    private ParticleSystem confetti;
    private AudioSource audioSource;
    private AudioSource pickupSource;
    private AudioClip pickupClip;
    private bool hasPlayed;

    void Start()
    {
        CreateConfetti();
        CreateAudio();
        CreatePickupSound();
    }

    public void Play()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        confetti.Play();
        audioSource.Play();
    }

    public void PlayPickup()
    {
        pickupSource.PlayOneShot(pickupClip);
    }

    private void CreateConfetti()
    {
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.SetParent(transform, false);
        confettiObj.transform.localPosition = Vector3.up * 3f;

        confetti = confettiObj.AddComponent<ParticleSystem>();
        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = confetti.main;
        main.duration = 3f;
        main.loop = false;
        main.startLifetime = 2.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.gravityModifier = 0.6f;
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.1f),
            new Color(0.1f, 0.6f, 1f)
        );

        var emission = confetti.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 300)
        });

        var shape = confetti.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.5f;

        var colorOverLifetime = confetti.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.8f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = confetti.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        var rotation = confetti.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var renderer = confettiObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = Color.white;
    }

    private void CreateAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0.7f;

        int sampleRate = 44100;
        float duration = 1.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 659.25f };
        float noteDelay = 0.15f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float noteStart = n * noteDelay;
                if (t >= noteStart)
                {
                    float noteT = t - noteStart;
                    float envelope = Mathf.Exp(-noteT * 3f);
                    float wave = Mathf.Sin(2f * Mathf.PI * notes[n] * noteT);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * noteT) * 0.3f;
                    sample += (wave + harmonic) * envelope * 0.15f;
                }
            }

            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("WinJingle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.clip = clip;
    }

    private void CreatePickupSound()
    {
        pickupSource = gameObject.AddComponent<AudioSource>();
        pickupSource.playOnAwake = false;
        pickupSource.spatialBlend = 1f;
        pickupSource.volume = 0.5f;

        int sampleRate = 44100;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float freq = 880f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            envelope *= envelope;
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
            float wave2 = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.4f;
            samples[i] = (wave + wave2) * envelope * 0.3f;
        }

        pickupClip = AudioClip.Create("PickupBip", sampleCount, 1, sampleRate, false);
        pickupClip.SetData(samples, 0);
    }
}
