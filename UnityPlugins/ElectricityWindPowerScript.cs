using UnityEngine;

public class ElectricityWindPowerScript : MonoBehaviour
{

    public Transform housing;
    public Transform rotor;

    // Audio sample to play in loop
    public AudioSource AudioLoop;

    // Audio sample to play per "swoosh"
    public AudioSource AudioSwoosh;

    // After which degrees to start the swoosh
    public float SwooshOffset = 60f;
    // Basically 360° divided by the blades
    public float SwooshInterval = 120f;
    // Delay playing the audio
    public float SwooshDelay = 0f;
    // Factor to scale swoosh pitch
    // Relative to current RotorSpeed
    public float SwooshPitch = 50f;

    void Awake()
    {
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
    }

}
