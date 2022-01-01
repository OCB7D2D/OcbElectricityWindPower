using UnityEngine;

#pragma warning disable IDE0051 // Remove unused private members

public class ElectricityWindPowerScript : MonoBehaviour
{

    public Transform rotor;
    public Transform housing;

    // Wind seed coordinates
    public int SeedX = 0;
    public int SeedZ = 0;

    // After which degrees to start the swoosh
    public float SwooshOffset = 60f;
    // Basically 360° divided by the blades
    public float SwooshInterval = 120f;
    // Delay playing the audio
    public float SwooshDelay = 0f;
    // Factor to scale swoosh pitch
    // Relative to current RotorSpeed
    public float SwooshPitch = 50f;

    // Set to true when stopping
    private bool Stopped = false;

    // Values to reach
    public float WindSpeed = 0f;
    public float WindDirection = 0f;

    // Current (runtime) values
    private float Direction = 0f;
    private float RotorSpeed = 0f;
    private float RotateSpeed = 0f;

    // Configuration settings
    private float MaxRotateSpeed = 4f;
    private float RotorAcceleration = 1.2f;
    private float RotorDeceleration = 0.9f;

    // private static read-only float PerlinTimeFactor = 0.9f / 45f;
    public static readonly float PerlinRotateFactor = 0.00005f;
    public static readonly float PerlinSpeedFactor = 0.0005f;

    // Scale world coordinates to Perlin coordinates
    // Wind should be similar for field close together
    public static readonly float WorldPerlinScale = 0.001f;

    // Variables used to play swooshes
    private AudioSource audioSwoosh;
    private float audioAnglePlayed = 0;

    // Stop the windmill script
    // Slowly stop and then deactivate
    // Will need activation afterwards
    public void Disable()
    {
        Stopped = true;
        WindSpeed = 0f;
    }

    public void Enable()
    {
        Stopped = false;
        enabled = true;
    }

    // Called when component is created
    void Awake()
    {
        // Only enable script on clients
        enabled = !GameManager.IsDedicatedServer;
        // Try to find the audio sample for the swoosh
        audioSwoosh = gameObject.transform.GetComponentInChildren<AudioSource>();
    }

    // Called after awake only if enabled
    void OnEnable()
    {
        // Audio.Manager.AddPlayingAudioSource(audioSwoosh);
        // audioSwoosh.Play();
    }

    void OnDisable()
    {
        // Audio.Manager.RemovePlayingAudioSource(audioSwoosh);
        // audioSwoosh.Stop();
    }

    void UpdateRotorSpeed()
    {
        if (rotor == null) return;
        // Rotor needs to speed down
        if (RotorSpeed > WindSpeed)
        {
            RotorSpeed -= RotorDeceleration * Time.deltaTime;
            RotorSpeed = Mathf.Max(RotorSpeed, WindSpeed);
        }
        // Rotor needs to speed up
        else if (RotorSpeed < WindSpeed)
        {
            RotorSpeed += RotorAcceleration * Time.deltaTime;
            RotorSpeed = Mathf.Min(RotorSpeed, WindSpeed);
        }
        // Finally rotate the rotor by given speed
        rotor.Rotate(0, 0, - RotorSpeed * Time.deltaTime);
        // Pitch the audio source
        if (audioSwoosh != null)
        {
            audioAnglePlayed += RotorSpeed * Time.deltaTime;
            // Swoosh offset
            if (audioAnglePlayed > SwooshOffset)
            {
                audioAnglePlayed -= SwooshInterval;
                audioSwoosh.PlayDelayed(SwooshDelay);
            }
            // Pitch swoosh audio for faster rotor speed
            audioSwoosh.pitch = RotorSpeed / SwooshPitch;
            // Fade volume in when rotor is accelerating
            audioSwoosh.volume = Mathf.Min(1f, audioSwoosh.pitch);
        }
    }

    void UpdateRotation()
    {
        if (housing == null) return;
        // Get the current bearing via Euler angles
        // Costs a little to get from quaternion?
        Direction = housing.rotation.eulerAngles.y;

        // Get shortest distance and also indicate direction
        float distance = (WindDirection - Direction + 540f) % 360f - 180f;

        // Add acceleration factor to get to needed bearing
        RotateSpeed += distance / 100f * Time.deltaTime;
        // Add deceleration factor if close to target
        if (Mathf.Abs(distance) < 10f)
        {
            RotateSpeed *= 0.99f;
        }
        // Obey max rotation speed
        if (RotateSpeed > MaxRotateSpeed)
        {
            RotateSpeed = MaxRotateSpeed;
        }
        else if (RotateSpeed < -MaxRotateSpeed)
        {
            RotateSpeed = -MaxRotateSpeed;
        }
        // Finally rotate the housing by given speed
        housing.Rotate(0, RotateSpeed * Time.deltaTime, 0);
        // housing.rotation = Quaternion.Euler(0, WindDirection, 0);
    }

    // Called for every frame update
    // Called as many times as possible
    void Update()
    {
        UpdateRotorSpeed();
        UpdateRotation();
        // Auto disable if fully stopped
        if (Stopped && RotateSpeed == 0)
        {
            enabled = false;
        }
    }

    public static float GetWindSpeed(int x, int z)
    {
        World world = GameManager.Instance.World;
        return Mathf.PerlinNoise(
            z * WorldPerlinScale,
            x * WorldPerlinScale
            + world.worldTime * PerlinSpeedFactor);
    }
    public static float GetWindBearing(int x, int z)
    {
        World world = GameManager.Instance.World;
        return (Mathf.Abs(Mathf.PerlinNoise(
                z * WorldPerlinScale
                + world.worldTime * PerlinRotateFactor,
                x * WorldPerlinScale
            ) - 0.5f) * 720f) % 360f;
    }

    // Called for every physics update
    // Called in a fixed interval
    void FixedUpdate()
    {
        if (Stopped) return;
        WindSpeed = GetWindSpeed(SeedX, SeedZ)
            * Random.Range(0.9f, 1.1f) * 100f;
        WindDirection = GetWindBearing(SeedX, SeedZ);
    }

}
