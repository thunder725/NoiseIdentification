using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class noiseIdentificationScript : MonoBehaviour {

    // General Data
    public KMAudio AudioComponent;
    public KMSelectable buttonCrystal;
    public KMSelectable buttonLiquid;
    public KMSelectable buttonMoisture;
    public KMSelectable buttonPerlin;
    public KMSelectable buttonVoronoi;
    public KMSelectable buttonWhite;
    public KMBombModule module;

    public Material ledOnMaterial;
    Material ledOffMaterial;
    public MeshRenderer noisePlaneRenderer;

    public MeshRenderer stageOneLedRenderer;
    public MeshRenderer stageTwoLedRenderer;
    public MeshRenderer stageThreeLedRenderer;


    // Animation & Feedback Variables
    Vector3 NoisePlaneEndScale;
    Vector3 NoisePlaneEndPosition;

    // 6 types * 5 textures each so total of 30
    // index 0-4 are for Crystal, 5-9 are for Liquid, then Moisture, Perlin, Voronoi, and 24-29 are White.
    public Texture2D[] noiseTextures;


    // Define all 6 possible Noise Types
    public enum NoiseType : short { Crystal, Liquid, Moisture, Perlin, Voronoi, White };


    // Logging Data - Formatting & naming from Royal_Flu$h
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private int currentStageNumber;
    private NoiseType currentStageType;

    // Souvenir-Accessible data
    public NoiseType stageOneNoise;
    public NoiseType stageTwoNoise;
    public NoiseType stageThreeNoise;




    // Data Gathering and stuff
    void Awake()
    {
        moduleId = moduleIdCounter++;

        // Initialize Delegates for button presses
        buttonCrystal.OnInteract += delegate () { NoiseButtonPressed(NoiseType.Crystal, buttonCrystal); return false; };
        buttonLiquid.OnInteract += delegate () { NoiseButtonPressed(NoiseType.Liquid, buttonLiquid); return false; };
        buttonMoisture.OnInteract += delegate () { NoiseButtonPressed(NoiseType.Moisture, buttonMoisture); return false; };
        buttonPerlin.OnInteract += delegate () { NoiseButtonPressed(NoiseType.Perlin, buttonPerlin); return false; };
        buttonVoronoi.OnInteract += delegate () { NoiseButtonPressed(NoiseType.Voronoi, buttonVoronoi); return false; };
        buttonWhite.OnInteract += delegate () { NoiseButtonPressed(NoiseType.White, buttonWhite); return false; };

        // Cache the target Transform for animations.
        NoisePlaneEndScale = noisePlaneRenderer.transform.localScale;
        NoisePlaneEndPosition = noisePlaneRenderer.transform.localPosition;

        ledOffMaterial = stageThreeLedRenderer.material;
    }


    // Module Initialization
    void Start() {

        GenerateStages();
        currentStageNumber = 1;
        currentStageType = stageOneNoise;

        ApplyTextureVisually();

        Debug.LogFormat("[Noise Identification #{0}] Initialization finished.", moduleId);
        Debug.LogFormat("[Noise Identification #{0}] Stage One (1) will be of type {1}.", moduleId, stageOneNoise.ToString());
        Debug.LogFormat("[Noise Identification #{0}] Stage Two (2) will be of type {1}.", moduleId, stageTwoNoise.ToString());
        Debug.LogFormat("[Noise Identification #{0}] Stage Three (3) will be of type {1}.", moduleId, stageThreeNoise.ToString());

    }


    void GenerateStages()
    {
        // Stage One is completely random: an integer in range [0-5] for the 6 total types
        stageOneNoise = (NoiseType)Random.Range(0, 6);


        // Stage Two shouldn't repeat what Stage One did
        stageTwoNoise = (NoiseType)Random.Range(0, 6);
        if (stageTwoNoise == stageOneNoise)
        {
            // Offset the noise type by a random offset from 1 to 5.
            // No 0 nor 6 to avoid landing on the same Noise Type again.
            int tempNewNoise = ((int)stageTwoNoise + Random.Range(1, 6));
            tempNewNoise %= 6;
            stageTwoNoise = (NoiseType)tempNewNoise;
        }

        // And Stage Three shouldn't repeat what Stage Two did, but can repeat what Stage One did.
        stageThreeNoise = (NoiseType)Random.Range(0, 6);
        if (stageThreeNoise == stageTwoNoise)
        {
            // Offset the noise type by a random offset from 1 to 5.
            // No 0 nor 6 to avoid landing on the same Noise Type again.
            int tempNewNoise = ((int)stageThreeNoise + Random.Range(1, 6));
            tempNewNoise %= 6;
            stageThreeNoise = (NoiseType)tempNewNoise;
        }
    }


    void ApplyTextureVisually()
    {
        // type is within 0-5. This should give a random texture within the 5 of the correct type.
        int textureIndexToApply = (int)currentStageType * 5 + Random.Range(0, 5);

        noisePlaneRenderer.material.mainTexture = noiseTextures[textureIndexToApply];
    }



    // Gets called when a button gets pressed
    void NoiseButtonPressed(NoiseType type, KMSelectable buttonType)
    {
        // Do not give any strike nor stage solve after the module is solved
        if (moduleSolved)
        {
            return;
        }

        // Add some feedback on the bomb
        // We can have a value because we won't do multiple presses in a row, but not too big since those are small buttons
        buttonType.AddInteractionPunch(0.7f);

        // Play button pressed sound
        AudioComponent.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttonType.transform);


        if (currentStageType == type)
        {
            Debug.LogFormat("[Noise Identification #{0}] Expected Type {1}. You pressed Button Type {2}. That was correct.", moduleId, currentStageType.ToString(), type.ToString());


            // Go to the next stage
            currentStageNumber++;

            switch (currentStageNumber)
            {
                // Either a regular stage
                case 2:
                    currentStageType = stageTwoNoise;

                    // Light up Stage 2 LED and reveal next stage display
                    StartCoroutine(TurnOnStageLight(stageOneLedRenderer));
                    StartCoroutine(RevealNextStageDisplay());
                    break;

                case 3:
                    currentStageType = stageThreeNoise;

                    // Light up Stage 2 LED and reveal next stage display
                    StartCoroutine(TurnOnStageLight(stageTwoLedRenderer));
                    StartCoroutine(RevealNextStageDisplay());
                    break;

                // Or a finished module
                case 4:
                    // Light up Stage 3 LED
                    StartCoroutine(TurnOnStageLight(stageThreeLedRenderer));

                    // Hide the screen because Souvenir might come into action later down the line.
                    noisePlaneRenderer.transform.localScale = new Vector3(0.001f, 1f, 0.001f);
                    noisePlaneRenderer.transform.localPosition = new Vector3(0f, 0.45f, 0f);


                    moduleSolved = true;
                    Debug.LogFormat("[Noise Identification #{0}] Module Solved.", moduleId);

                    // Transmit solve to bomb
                    module.HandlePass();
                    break;

                // This should never happen, but just in case I'll write this error.
                default:
                    Debug.LogFormat("[Noise Identification #{0}] Arrived to unknown Stage Number: {1}. Please report this to 'thunder725' on Discord with the log. Solving module to prevent Soft-locks.", moduleId, currentStageNumber);

                    // Act like a solve to prevent soft-locks due to errors.
                    // Light up all stage LEDs
                    StartCoroutine(TurnOnStageLight(stageOneLedRenderer));
                    StartCoroutine(TurnOnStageLight(stageTwoLedRenderer));
                    StartCoroutine(TurnOnStageLight(stageThreeLedRenderer));

                    // Hide the screen because Souvenir might come into action later down the line.
                    noisePlaneRenderer.transform.localScale = new Vector3(0.001f, 1f, 0.001f);
                    noisePlaneRenderer.transform.localPosition = new Vector3(0f, 0.45f, 0f);

                    moduleSolved = true;
                    Debug.LogFormat("[Noise Identification #{0}] Module Solved.", moduleId);

                    // Transmit solve to bomb
                    module.HandlePass();

                    break;
            }

        }
        // Incorrect type? Log it!
        else
        {
            Debug.LogFormat("[Noise Identification #{0}] !!STRIKE!! Expected Type {1}. You pressed Button Type {2}. That was incorrect.", moduleId, currentStageType.ToString(), type.ToString());

            module.HandleStrike();
        }

    }

    // Coroutine for updating the Display with the next texture
    IEnumerator RevealNextStageDisplay()
    {
        // Hide the plane for a couple of seconds
        noisePlaneRenderer.transform.localScale = new Vector3(0.001f, 1f, 0.001f);
        noisePlaneRenderer.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        // With the correct next texture applied.
        ApplyTextureVisually();


        yield return new WaitForSeconds(0.2f);


        // Then reveal it gradually over a couple of seconds
        float lerpTimer = 0f;

        while (lerpTimer < 1f)
        {
            noisePlaneRenderer.transform.localScale = new Vector3(Mathf.Lerp(0.001f, NoisePlaneEndScale.x, lerpTimer), 1, Mathf.Lerp(0.001f, NoisePlaneEndScale.z, lerpTimer));
            noisePlaneRenderer.transform.localPosition = new Vector3(Mathf.Lerp(0.001f, NoisePlaneEndPosition.x, lerpTimer), 1, Mathf.Lerp(0.001f, NoisePlaneEndPosition.z, lerpTimer));

            lerpTimer += Time.deltaTime * 1.3f;
            yield return null;
        }

        // Afterwards, set the transform to make sure it didn't overshoot due to frame interpolation errors
        noisePlaneRenderer.transform.localScale = NoisePlaneEndScale;
        noisePlaneRenderer.transform.localPosition = NoisePlaneEndPosition;
    }



    // Coroutine for turning on the Stage Light
    // It'll blink, starting as off obviously, turning on, then turning off and then turning back on
    // A simple blinking to make it more juicy
    IEnumerator TurnOnStageLight(MeshRenderer _lightToTurnOn)
    {
        _lightToTurnOn.material = ledOnMaterial;

        yield return new WaitForSeconds(0.09f);

        _lightToTurnOn.material = ledOffMaterial;

        yield return new WaitForSeconds(0.06f);

        _lightToTurnOn.material = ledOnMaterial;
    }


    // =-=-=-=-=-= TWITCH PLAYS =-=-=-=-=-=

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Press a button with “!{0} crystal” or “!{0} c”. Valid commands are “!{0} crystal”, “!{0} liquid”, “!{0} moisture”, “!{0} perlin”, “!{0} voronoi”, “!{0} white”, as well as their initials. ";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();


        if (command.Equals("c") || command.Equals("crystal"))
        {
            return new KMSelectable[] { buttonCrystal };
        }
        else if (command.Equals("l") || command.Equals("liquid"))
        {
            return new KMSelectable[] { buttonLiquid };
        }
        else if (command.Equals("m") || command.Equals("moisture"))
        {
            return new KMSelectable[] { buttonMoisture };
        }
        else if (command.Equals("p") || command.Equals("perlin"))
        {
            return new KMSelectable[] { buttonPerlin };
        }
        else if (command.Equals("v") || command.Equals("voronoi"))
        {
            return new KMSelectable[] { buttonVoronoi };
        }
        else if (command.Equals("w") || command.Equals("white"))
        {
            return new KMSelectable[] { buttonWhite };
        }

        return null;


    }

}
