using UnityEngine;

#pragma warning disable IDE0051 // Remove unused private members

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

    // Wind seed coordinates
    public int SeedX = 0;
    public int SeedZ = 0;

    // Audio sample to play per "swoosh"
    public AudioSource AudioSwoosh;
    // Audio sample to play in loop
    public AudioSource AudioLoop;

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
    }

    // Called after awake only if enabled
    void OnEnable()
    {
        if (AudioLoop != null)
        {
            Audio.Manager.AddPlayingAudioSource(AudioLoop);
            AudioLoop.Play();
        }
    }

    void OnDisable()
    {
        if (AudioLoop != null)
        {
            Audio.Manager.RemovePlayingAudioSource(AudioLoop);
            AudioLoop.Stop();
        }
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
        rotor.Rotate(rotorAxis * - RotorSpeed * Time.deltaTime);
        // Pitch the audio source
        if (AudioSwoosh != null)
        {
            audioAnglePlayed += RotorSpeed * Time.deltaTime;
            // Swoosh offset
            if (audioAnglePlayed > SwooshOffset)
            {
                audioAnglePlayed -= SwooshInterval;
                AudioSwoosh.PlayDelayed(SwooshDelay);
            }
            // Pitch swoosh audio for faster rotor speed
            AudioSwoosh.pitch = RotorSpeed / SwooshPitch;
            // Fade volume in when rotor is accelerating
            AudioSwoosh.volume = Mathf.Min(1f, AudioSwoosh.pitch);
        }
        if (AudioLoop != null)
        {
            // Pitch swoosh audio for faster rotor speed
            AudioLoop.pitch = RotorSpeed / LoopPitch;
            // Fade volume in when rotor is accelerating
            AudioLoop.volume = Mathf.Min(1f, AudioLoop.pitch);
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
        housing.Rotate(housingAxis * RotateSpeed * Time.deltaTime);
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
