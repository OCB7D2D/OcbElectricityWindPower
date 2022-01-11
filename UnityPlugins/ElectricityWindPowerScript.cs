using UnityEngine;

public class ElectricityWindPowerScript : MonoBehaviour
{

    // Housing to rotate into the wind
    public Transform housing;
    // The "axis" to animate transform rotation
    public Vector3 housingAxis = new Vector3(0, 1, 0);
    // The rotor to rotate with wind speed
    public Transform rotor;
    // The "axis" to animate transform rotation
    public Vector3 rotorAxis = new Vector3(0, 0, 1);

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
    public float LoopPitch = 50f;

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
