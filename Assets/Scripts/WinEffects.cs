using UnityEngine;

public class WinEffects : MonoBehaviour
{
    private ParticleSystem confetti;
    private AudioSource audioSource;
    private AudioSource pickupSource;
    [SerializeField] private AudioClip confettiClip;
    [SerializeField] private AudioClip pickupClip;
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
        audioSource.clip = confettiClip;
    }

    private void CreatePickupSound()
    {
        pickupSource = gameObject.AddComponent<AudioSource>();
        pickupSource.playOnAwake = false;
        pickupSource.spatialBlend = 1f;
        pickupSource.volume = 0.5f;
    }
}
