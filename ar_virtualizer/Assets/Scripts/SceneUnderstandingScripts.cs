﻿//System
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//Unity
using UnityEngine;
using UnityEngine.Events;

using Microsoft.MixedReality.SceneUnderstanding;
using Microsoft.Windows.Perception.Spatial;
using Microsoft.Windows.Perception.Spatial.Preview;
// using Microsoft.Storage;

/// <summary>
/// Different rendering modes available for scene objects.
/// </summary>
public enum RenderMode
{
    Quad,
    QuadWithMask,
    Mesh,
    Wireframe
}

[StructLayout(LayoutKind.Sequential)]
public struct HolograhicFrameData
{
    public uint VersionNumber;
    public uint MaxNumberOfCameras;
    public IntPtr ISpatialCoordinateSystemPtr; // Windows::Perception::Spatial::ISpatialCoordinateSystem
    public IntPtr IHolographicFramePtr; // Windows::Graphics::Holographic::IHolographicFrame
    public IntPtr IHolographicCameraPtr; // // Windows::Graphics::Holographic::IHolographicCamera
}

public class SceneUnderstandingScripts : MonoBehaviour
{
    #region Public Variables
    [Header("Data Loader Mode")]
    [Tooltip("When enabled, the scene will be queried from a device (e.g Hololens). Otherwise, a previously saved, serialized scene will be loaded and served from your PC.")]
    public bool QuerySceneFromDevice = true;
    [Tooltip("The scene to load when not running on the device (e.g SU_Kitchen in Resources/SerializedScenesForPCPath).")]
    public List<TextAsset> SUSerializedScenePaths = new List<TextAsset>(0);

    [Header("Root GameObject")]
    [Tooltip("GameObject that will be the parent of all Scene Understanding related game objects. If field is left empty an empty gameobject named 'Root' will be created.")]
    public GameObject SceneRoot = null;

    [Header("On Device Request Settings")]
    [Tooltip("Radius of the sphere around the camera, which is used to query the environment.")]
    [Range(5f, 100f)]
    public float BoundingSphereRadiusInMeters = 10.0f;
    [Tooltip("When enabled, the latest data from Scene Understanding data provider will be displayed periodically (controlled by the AutoRefreshIntervalInSeconds float).")]
    public bool AutoRefresh = true;
    [Tooltip("Interval to use for auto refresh, in seconds.")]
    [Range(1f, 60f)]
    public float AutoRefreshIntervalInSeconds = 10.0f;

    [Header("Request Settings")]
    [Tooltip("Type of visualization to use for scene objects.")]
    public RenderMode SceneObjectRequestMode = RenderMode.Mesh;
    [Tooltip("Level Of Detail for the scene objects.")]
    public SceneMeshLevelOfDetail MeshQuality = SceneMeshLevelOfDetail.Medium;
    [Tooltip("When enabled, requests observed and inferred regions for scene objects. When disabled, requests only the observed regions for scene objects.")]
    public bool RequestInferredRegions = true;

    [Header("Render Colors")]
    [Tooltip("Colors for the Scene Understanding Background objects")]
    public Color ColorForBackgroundObjects = new Color(0.953f, 0.475f, 0.875f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Wall objects")]
    public Color ColorForWallObjects = new Color(0.953f, 0.494f, 0.475f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Floor objects")]
    public Color ColorForFloorObjects = new Color(0.733f, 0.953f, 0.475f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Ceiling objects")]
    public Color ColorForCeilingObjects = new Color(0.475f, 0.596f, 0.953f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Platform objects")]
    public Color ColorForPlatformsObjects = new Color(0.204f, 0.792f, 0.714f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Unknown objects")]
    public Color ColorForUnknownObjects = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    [Tooltip("Colors for the Scene Understanding Inferred objects")]
    public Color ColorForInferredObjects = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [Tooltip("Colors for the World mesh")]
    public Color ColorForWorldObjects = new Color(0.0f, 1.0f, 1.0f, 1.0f);

    [Header("Layers")]
    [Tooltip("Layer for Scene Understanding Background objects")]
    public int LayerForBackgroundObjects;
    [Tooltip("Layer for the Scene Understanding Wall objects")]
    public int LayerForWallObjects;
    [Tooltip("Layer for the Scene Understanding Floor objects")]
    public int LayerForFloorObjects;
    [Tooltip("Layer for the Scene Understanding Ceiling objects")]
    public int LayerForCeilingObjects;
    [Tooltip("Layer for the Scene Understanding Platform objects")]
    public int LayerForPlatformsObjects;
    [Tooltip("Layer for the Scene Understanding Unknown objects")]
    public int LayerForUnknownObjects;
    [Tooltip("Layer for the Scene Understanding Inferred objects")]
    public int LayerForInferredObjects;
    [Tooltip("Layer for the World mesh")]
    public int LayerForWorldObjects;

    [Header("Scene Object Mesh Materials")]
    [Tooltip("Material for Scene Understanding Background in Mesh Mode")]
    public Material SceneObjectBackgroundMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Wall in Mesh Mode")]
    public Material SceneObjectWallMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Floor in Mesh Mode")]
    public Material SceneObjectFloorMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Ceiling in Mesh Mode")]
    public Material SceneObjectCeilingMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Platform in Mesh Mode")]
    public Material SceneObjectPlatformMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Unknown in Mesh Mode")]
    public Material SceneObjectUnknownMeshMaterial = null;
    [Tooltip("Material for Scene Understanding Inferred in Mesh Mode")]
    public Material SceneObjectInferredMeshMaterial = null;

    [Header("Scene Object Quad Materials")]
    [Tooltip("Material for Scene Understanding Background in Quad Mode")]
    public Material SceneObjectBackgroundQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Wall in Mesh Mode")]
    public Material SceneObjectWallQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Floor in Mesh Mode")]
    public Material SceneObjectFloorQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Ceiling in Mesh Mode")]
    public Material SceneObjectCeilingQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Platform in Mesh Mode")]
    public Material SceneObjectPlatformQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Unknown in Mesh Mode")]
    public Material SceneObjectUnknownQuadMaterial = null;
    [Tooltip("Material for Scene Understanding Inferred in Mesh Mode")]
    public Material SceneObjectInferredQuadMaterial = null;

    [Header("Scene Object WireFrame and Occlussion Materials")]
    [Tooltip("Material for scene object mesh wireframes.")]
    public Material SceneObjectWireframeMaterial = null;
    [Tooltip("Material for scene objects when in Ghost mode (invisible object with occlusion)")]
    public Material TransparentOcclussion = null;

    [Header("Filters")]
    [Tooltip("Toggles display of all scene objects, except for the world mesh.")]
    public bool FilterAllSceneObjects = false;
    [Tooltip("Toggles display of large, horizontal scene objects, aka 'Platform'.")]
    public bool FilterPlatformSceneObjects = false;
    [Tooltip("Toggles the display of background scene objects.")]
    public bool FilterBackgroundSceneObjects = false;
    [Tooltip("Toggles the display of unknown scene objects.")]
    public bool FilterUnknownSceneObjects = false;
    [Tooltip("Toggles the display of the world mesh.")]
    public bool FilterWorldMesh = true;
    [Tooltip("Toggles the display of completely inferred scene objects.")]
    public bool FilterCompletelyInferredSceneObjects = false;
    [Tooltip("Toggles the display of wall scene objects.")]
    public bool FilterWallSceneObjects = false;
    [Tooltip("Toggles the display of wall scene objects.")]
    public bool FilterFloorSceneObjects = false;
    [Tooltip("Toggles the display of wall scene objects.")]
    public bool FilterCeilingSceneObjects = false;

    [Header("Physics")]
    [Tooltip("Toggles the creation of platform objects with collider components")]
    public bool AddCollidersInPlatformSceneObjects = false;
    [Tooltip("Toggles the creation of background objects with collider components")]
    public bool AddCollidersInBackgroundSceneObjects = false;
    [Tooltip("Toggles the creation of unknown objects with collider components")]
    public bool AddCollidersInUnknownSceneObjects = false;
    [Tooltip("Toggles the creation of the world mesh with collider components")]
    public bool AddCollidersInWorldMesh = false;
    [Tooltip("Toggles the creation of completely inferred objects with collider components")]
    public bool AddCollidersInCompletelyInferredSceneObjects = false;
    [Tooltip("Toggles the creation of wall objects with collider components")]
    public bool AddCollidersInWallSceneObjects = false;
    [Tooltip("Toggles the creation of floor objects with collider components")]
    public bool AddCollidersInFloorSceneObjects = false;
    [Tooltip("Toggles the creation of ceiling objects with collider components")]
    public bool AddCollidersCeilingSceneObjects = false;

    [Header("Occlussion")]
    [Tooltip("Toggle Ghost Mode, (invisible objects that still occlude)")]
    public bool IsInGhostMode = false;

    [Header("Alignment")]
    [Tooltip("Align SU Objects Normal to Unity's Y axis")]
    public bool AlignSUObjectsNormalToUnityYAxis = true;

    [Header("Events")]
    [Tooltip("User function that get called when a Scene Understanding event happens")]
    public UnityEvent OnLoadStarted;
    [Tooltip("User function that get called when a Scene Understanding event happens")]
    public UnityEvent OnLoadFinished;

    #endregion

    #region Private Variables
    private Dictionary<SceneObjectKind, Dictionary<RenderMode, Material>> materialCache;

    private readonly float MinBoundingSphereRadiusInMeters = 5f;
    private readonly float MaxBoundingSphereRadiusInMeters = 100f;
    private byte[] LatestSUSceneData = null;
    private readonly object SUDataLock = new object();
    private Guid LatestSceneGuid;
    private Guid LastDisplayedSceneGuid;
    private Task displayTask = null;
    [HideInInspector]
    public float TimeElapsedSinceLastAutoRefresh = 0.0f;
    private bool DisplayFromDiskStarted = false;
    private bool RunOnDevice;
    private readonly int NumberOfSceneObjectsToLoadPerFrame = 5;
    private Scene cachedDeserializedScene = null;
    private readonly object cachedDeserializedSceneLock = new object();
    #endregion

    #region Unity Start and Update
    // #region Unity Start and Update
    // Start is called before the first frame update
    private async void Start()
    {
        SceneRoot = SceneRoot == null ? new GameObject("Scene Root") : SceneRoot;

        // Considering that device is currently not supported in the editor means that
        // if the application is running in the editor it is for sure running on PC and
        // not a device. this assumption, for now, is always true.
        RunOnDevice = !Application.isEditor;

        if (QuerySceneFromDevice)
        {
            // Figure out if the application is setup to allow querying a scene from device

            // The app must not be running in the editor
            if (Application.isEditor)
            {
                Debug.LogError("SceneUnderstandingScripts.Start: Running in editor while quering scene from a device is not supported.\n" +
                               "To run on editor disable the 'QuerySceneFromDevice' Flag in the SceneUnderstandingScripts Component");
                return;
            }

            if (!SceneObserver.IsSupported())
            {
                Debug.LogError("SceneUnderstandingScripts.Start: Scene Understanding not supported.");
                return;
            }

            SceneObserverAccessStatus access = await SceneObserver.RequestAccessAsync();
            if (access != SceneObserverAccessStatus.Allowed)
            {
                Debug.LogError("SceneUnderstandingScripts.Start: Access to Scene Understanding has been denied.\n" +
                               "Reason: " + access);
                return;
            }

            // If the application is capable of querying a scene from the device,
            // start and endless task that queries for the lastest scene at all times
            try
            {
#pragma warning disable CS4014
                Task.Run(() => RetrieveDataContinuously());
#pragma warning restore CS4014
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private async void Update()
    {
        // If the scene is being queried from the device, then allow for autorefresh
        if (QuerySceneFromDevice)
        {
            if (AutoRefresh)
            {
                TimeElapsedSinceLastAutoRefresh += Time.deltaTime;
                if (TimeElapsedSinceLastAutoRefresh >= AutoRefreshIntervalInSeconds)
                {
                    try
                    {
                        await DisplayDataAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in {nameof(SceneUnderstandingScripts)} {nameof(AutoRefresh)}: {ex.Message}");
                    }
                    TimeElapsedSinceLastAutoRefresh = 0.0f;
                }
            }
        }
        // If the scene is pre-loaded from disk, display it only once, as consecutive renders
        // will only bring the same result
        else if (!DisplayFromDiskStarted)
        {
            DisplayFromDiskStarted = true;
            try
            {
                await DisplayDataAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in {nameof(SceneUnderstandingScripts)} DisplayFromDisk: {ex.Message}");
            }
        }
    }

    #endregion

    #region Data Querying and Consumption

    // It is recommended to deserialize a scene from scene fragments
    // consider all scenes as made up of scene fragments, even if only one.
    private SceneFragment GetLatestSceneSerialization()
    {
        SceneFragment fragmentToReturn = null;

        lock (SUDataLock)
        {
            if (LatestSUSceneData != null)
            {
                byte[] sceneBytes = null;
                int sceneLength = LatestSUSceneData.Length;
                sceneBytes = new byte[sceneLength];

                Array.Copy(LatestSUSceneData, sceneBytes, sceneLength);

                // Deserialize the scene into a Scene Fragment
                fragmentToReturn = SceneFragment.Deserialize(sceneBytes);
            }
        }

        return fragmentToReturn;
    }

    private Guid GetLatestSUSceneId()
    {
        Guid suSceneIdToReturn;

        lock (SUDataLock)
        {
            // Return the GUID for the latest scene
            suSceneIdToReturn = LatestSceneGuid;
        }

        return suSceneIdToReturn;
    }

    public Scene GetLatestDeserializedScene()
    {
        Scene sceneToReturn = null;

        lock (cachedDeserializedSceneLock)
        {
            if (cachedDeserializedScene != null)
            {
                sceneToReturn = cachedDeserializedScene;
            }
        }

        return sceneToReturn;
    }

    /// <summary>
    /// Retrieves Scene Understanding data continuously from the runtime.
    /// </summary>
    private void RetrieveDataContinuously()
    {
        // At the beginning, retrieve only the observed scene object meshes.
        RetrieveData(BoundingSphereRadiusInMeters, false, true, false, false, SceneMeshLevelOfDetail.Coarse);

        while (true)
        {
            // Always request quads, meshes and the world mesh. SceneUnderstandingScripts will take care of rendering only what the user has asked for.
            RetrieveData(BoundingSphereRadiusInMeters, true, true, RequestInferredRegions, true, MeshQuality);
        }
    }

    /// <summary>
    /// Calls into the Scene Understanding APIs, to retrieve the latest scene as a byte array.
    /// </summary>
    /// <param name="enableQuads">When enabled, quad representation of scene objects is retrieved.</param>
    /// <param name="enableMeshes">When enabled, mesh representation of scene objects is retrieved.</param>
    /// <param name="enableInference">When enabled, both observed and inferred scene objects are retrieved. Otherwise, only observed scene objects are retrieved.</param>
    /// <param name="enableWorldMesh">When enabled, retrieves the world mesh.</param>
    /// <param name="lod">If world mesh is enabled, lod controls the resolution of the mesh returned.</param>
    private void RetrieveData(float boundingSphereRadiusInMeters, bool enableQuads, bool enableMeshes, bool enableInference, bool enableWorldMesh, SceneMeshLevelOfDetail lod)
    {
        //Debug.Log("SceneUnderstandingScripts.RetrieveData: Started.");

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        try
        {
            SceneQuerySettings querySettings;
            querySettings.EnableSceneObjectQuads = enableQuads;
            querySettings.EnableSceneObjectMeshes = enableMeshes;
            querySettings.EnableOnlyObservedSceneObjects = !enableInference;
            querySettings.EnableWorldMesh = enableWorldMesh;
            querySettings.RequestedMeshLevelOfDetail = lod;

            // Ensure that the bounding radius is within the min/max range.
            boundingSphereRadiusInMeters = Mathf.Clamp(boundingSphereRadiusInMeters, MinBoundingSphereRadiusInMeters, MaxBoundingSphereRadiusInMeters);

            // Make sure the scene query has completed swap with latestSUSceneData under lock to ensure the application is always pointing to a valid scene.
            SceneBuffer serializedScene = SceneObserver.ComputeSerializedAsync(querySettings, boundingSphereRadiusInMeters).GetAwaiter().GetResult();
            lock (SUDataLock)
            {
                // The latest data queried from the device is stored in these variables
                LatestSUSceneData = new byte[serializedScene.Size];
                serializedScene.GetData(LatestSUSceneData);
                LatestSceneGuid = Guid.NewGuid();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        stopwatch.Stop();
        /*
        Debug.Log(string.Format("SceneUnderstandingScripts.RetrieveData: Completed. Radius: {0}; Quads: {1}; Meshes: {2}; Inference: {3}; WorldMesh: {4}; LOD: {5}; Bytes: {6}; Time (secs): {7};",
                                boundingSphereRadiusInMeters,
                                enableQuads,
                                enableMeshes,
                                enableInference,
                                enableWorldMesh,
                                lod,
                                (LatestSUSceneData == null ? 0 : LatestSUSceneData.Length),
                                stopwatch.Elapsed.TotalSeconds));
        */
    }

    #endregion

    #region Display Data into Unity

    /// <summary>
    /// Displays the most recently updated SU data as Unity game objects.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the operation.
    /// </returns>
    public Task DisplayDataAsync()
    {
        // See if we already have a running task
        if ((displayTask != null) && (!displayTask.IsCompleted))
        {
            // Yes we do. Return the already running task.
            Debug.Log($"{nameof(SceneUnderstandingScripts)}.{nameof(DisplayDataAsync)} already in progress.");
            return displayTask;
        }
        // We have real work to do. Time to start the coroutine and track it.
        else
        {
            // Create a completion source
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            // Store the task
            displayTask = completionSource.Task;

            // Run Callbacks for On Load Started
            OnLoadStarted.Invoke();

            // Start the coroutine and pass in the completion source
            StartCoroutine(DisplayDataRoutine(completionSource));

            // Return the newly running task
            return displayTask;
        }
    }

    /// <summary>
    /// This coroutine will deserialize the latest SU data, either queried from the device
    /// or from disk and use it to create Unity Objects that represent that geometry
    /// </summary>
    /// <param name="completionSource">
    /// The <see cref="TaskCompletionSource{TResult}"/> that can be used to signal the coroutine is complete.
    /// </param>
    private IEnumerator DisplayDataRoutine(TaskCompletionSource<bool> completionSource)
    {
        Debug.Log("SceneUnderstandingScripts.DisplayData: About to display the latest set of Scene Objects");

        //We are about to deserialize a new Scene, if we have a cached scene, dispose it.
        if (cachedDeserializedScene != null)
        {
            cachedDeserializedScene.Dispose();
            cachedDeserializedScene = null;
        }

        if (QuerySceneFromDevice)
        {
            // Get Latest Scene and Deserialize it
            // Scenes Queried from a device are Scenes composed of one Scene Fragment
            SceneFragment sceneFragment = GetLatestSceneSerialization();
            SceneFragment[] sceneFragmentsArray = new SceneFragment[1] { sceneFragment };
            cachedDeserializedScene = Scene.FromFragments(sceneFragmentsArray);

            SceneObject firstFloor = null;

            // Find the first floor object
            foreach (var sceneObject in cachedDeserializedScene.SceneObjects)
            {
                if (sceneObject.Kind == SceneObjectKind.Floor)
                {
                    firstFloor = sceneObject;
                    break;
                }
            }

            // Get Latest Scene GUID
            Guid latestGuidSnapShot = GetLatestSUSceneId();
            LastDisplayedSceneGuid = latestGuidSnapShot;
        }
        else
        {
            // Store all the fragments and build a Scene with them
            SceneFragment[] sceneFragments = new SceneFragment[SUSerializedScenePaths.Count];
            int index = 0;
            foreach (TextAsset serializedScene in SUSerializedScenePaths)
            {
                if (serializedScene != null)
                {
                    byte[] sceneData = serializedScene.bytes;
                    SceneFragment frag = SceneFragment.Deserialize(sceneData);
                    sceneFragments[index++] = frag;
                }
            }

            try
            {
                cachedDeserializedScene = Scene.FromFragments(sceneFragments);
                lock (SUDataLock)
                {
                    // Store new GUID for data loaded
                    LatestSceneGuid = Guid.NewGuid();
                    LastDisplayedSceneGuid = LatestSceneGuid;
                }
            }
            catch (Exception inner)
            {
                // Wrap the exception
                Exception outer = new FileLoadException("Scene from PC path couldn't be loaded, verify scene fragments are not null and that they all come from the same scene.", inner);
                Debug.LogWarning(outer.Message);
                completionSource.SetException(outer);
            }
        }

        if (cachedDeserializedScene != null)
        {
            // Retrieve a transformation matrix that will allow us orient the Scene Understanding Objects into
            // their correct corresponding position in the unity world
            System.Numerics.Matrix4x4? sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(cachedDeserializedScene);

            if (sceneToUnityTransformAsMatrix4x4 != null)
            {
                // If there was previously a scene displayed in the game world, destroy it
                // to avoid overlap with the new scene about to be displayed
                DestroyAllGameObjectsUnderParent(SceneRoot.transform);

                // Allow from one frame to yield the coroutine back to the main thread
                yield return null;

                // Using the transformation matrix generated above, port its values into the tranform of the scene root (Numerics.matrix -> GameObject.Transform)
                SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4.Value, RunOnDevice);

                if (!RunOnDevice)
                {
                    // If the scene is not running on a device, orient the scene root relative to the floor of the scene
                    // and unity's up vector
                    OrientSceneForPC(SceneRoot, cachedDeserializedScene);
                }


                // After the scene has been oriented, loop through all the scene objects and
                // generate their corresponding Unity Object
                IEnumerable<SceneObject> sceneObjects = cachedDeserializedScene.SceneObjects;

                int i = 0;
                foreach (SceneObject sceneObject in sceneObjects)
                {
                    if (DisplaySceneObject(sceneObject))
                    {
                        if (++i % NumberOfSceneObjectsToLoadPerFrame == 0)
                        {
                            // Allow a certain number of objects to load before yielding back to main thread
                            yield return null;
                        }
                    }
                }
            }

            // When all objects have been loaded, finish.
            Debug.Log("SceneUnderstandingScripts.DisplayData: Display Completed");

            // Run CallBacks for Onload Finished
            OnLoadFinished.Invoke();

            // Let the task complete
            completionSource.SetResult(true);
        }
    }

    /// <summary>
    /// Create a Unity Game Object for an individual Scene Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private bool DisplaySceneObject(SceneObject suObject)
    {
        if (suObject == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.DisplaySceneObj: Object is null");
            return false;
        }

        // If requested, scene objects can be excluded from the generation, the World Mesh is considered
        // a separate object hence is not affected by this filter
        if (FilterAllSceneObjects == true && suObject.Kind != SceneObjectKind.World)
        {
            return false;
        }

        // If an individual type of object is requested to not be rendered, avoid generation of unity object
        SceneObjectKind kind = suObject.Kind;
        switch (kind)
        {
            case SceneObjectKind.World:
                if (FilterWorldMesh)
                    return false;
                break;
            case SceneObjectKind.Platform:
                if (FilterPlatformSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Background:
                if (FilterBackgroundSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Unknown:
                if (FilterUnknownSceneObjects)
                    return false;
                break;
            case SceneObjectKind.CompletelyInferred:
                if (FilterCompletelyInferredSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Wall:
                if (FilterWallSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Floor:
                if (FilterFloorSceneObjects)
                    return false;
                break;
            case SceneObjectKind.Ceiling:
                if (FilterCeilingSceneObjects)
                    return false;
                break;
        }

        // This gameobject will hold all the geometry that represents the Scene Understanding Object
        GameObject unityParentHolderObject = new GameObject(suObject.Kind.ToString());
        unityParentHolderObject.transform.parent = SceneRoot.transform;

        // Scene Understanding uses a Right Handed Coordinate System and Unity uses a left handed one, convert.
        System.Numerics.Matrix4x4 converted4x4LocationMatrix = ConvertRightHandedMatrix4x4ToLeftHanded(suObject.GetLocationAsMatrix());
        // From the converted Matrix pass its values into the unity transform (Numerics -> Unity.Transform)
        SetUnityTransformFromMatrix4x4(unityParentHolderObject.transform, converted4x4LocationMatrix, true);

        // This list will keep track of all the individual objects that represent the geometry of
        // the Scene Understanding Object
        List<GameObject> unityGeometryObjects = null;
        switch (kind)
        {
            // Create all the geometry and store it in the list
            case SceneObjectKind.World:
                unityGeometryObjects = CreateWorldMeshInUnity(suObject);
                break;
            default:
                unityGeometryObjects = CreateSUObjectInUnity(suObject);
                break;
        }

        // For all the Unity Game Objects that represent The Scene Understanding Object
        // Of this iteration, make sure they are all children of the UnityParent object
        // And that their local postion and rotation is relative to their parent
        foreach (GameObject geometryObject in unityGeometryObjects)
        {
            geometryObject.transform.parent = unityParentHolderObject.transform;
            geometryObject.transform.localPosition = Vector3.zero;

            if (AlignSUObjectsNormalToUnityYAxis)
            {
                // If our Vertex Data is rotated to have it match its Normal to Unity's Y axis, we need to offset the rotation
                // in the parent object to have the object face the right direction
                geometryObject.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
            }
            else
            {
                //Otherwise don't rotate
                geometryObject.transform.localRotation = Quaternion.identity;
            }
        }

        // Add a SceneUnderstandingProperties Component to the Parent Holder Object
        // this component will hold a GUID and a SceneObjectKind that correspond to this
        // specific Object 

        SceneUnderstandingProperties properties = unityParentHolderObject.AddComponent<SceneUnderstandingProperties>();
        properties.suObjectGUID = suObject.Id;
        properties.suObjectKind = suObject.Kind;

        //Return that the Scene Object was indeed represented as a unity object and wasn't skipped
        return true;
    }

    /// <summary>
    /// Create a world Mesh Unity Object that represents the World Mesh Scene Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private List<GameObject> CreateWorldMeshInUnity(SceneObject suObject)
    {
        // The World Mesh Object is different from the rest of the Scene Understanding Objects
        // in the Sense that its unity representation is not affected by the filters or Request Modes
        // in this component, the World Mesh Renders even of the Scene Objects are disabled and
        // the World Mesh is always represented with a WireFrame Material, different to the Scene
        // Understanding Objects whose materials vary depending on the Settings in the component

        IEnumerable<SceneMesh> suMeshes = suObject.Meshes;
        Mesh unityMesh = GenerateUnityMeshFromSceneObjectMeshes(suMeshes);

        GameObject gameObjToReturn = new GameObject(suObject.Kind.ToString());
        gameObjToReturn.layer = LayerForWorldObjects;
        Material tempMaterial = GetMaterial(SceneObjectKind.World, RenderMode.Wireframe);
        AddMeshToUnityObject(gameObjToReturn, unityMesh, ColorForWorldObjects, tempMaterial);

        if (AddCollidersInWorldMesh)
        {
            // Generate a unity mesh for physics
            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(suObject.ColliderMeshes);

            MeshCollider col = gameObjToReturn.AddComponent<MeshCollider>();
            col.sharedMesh = unityColliderMesh;
        }

        // Also the World Mesh is represented as one big Mesh in Unity, different to the rest of SceneObjects
        // Where their multiple meshes are represented in separate game objects
        return new List<GameObject> { gameObjToReturn };
    }
    
    /// <summary>
    /// Create a list of Unity GameObjects that represent all the Meshes/Geometry in a Scene
    /// Understanding Object
    /// </summary>
    /// <param name="suObject">The Scene Understanding Object to generate in Unity</param>
    private List<GameObject> CreateSUObjectInUnity(SceneObject suObject)
    {
        // Each SU object has a specific type, query for its correspoding color
        // according to its type
        Color? color = GetColor(suObject.Kind);
        int layer = GetLayer(suObject.Kind);

        List<GameObject> listOfGeometryGameObjToReturn = new List<GameObject>();
        //Create the Quad SceneObjects first
        {
            // If the Request Settings are requesting quads, create a gameobject in unity for
            // each quad in the Scene Object
            foreach (SceneQuad quad in suObject.Quads)
            {
                Mesh unityMesh = GenerateUnityMeshFromSceneObjectQuad(quad);

                Material tempMaterial = GetMaterial(suObject.Kind, SceneObjectRequestMode);

                GameObject gameObjectToReturn = new GameObject(suObject.Kind.ToString() + "Quad");
                gameObjectToReturn.layer = layer;
                AddMeshToUnityObject(gameObjectToReturn, unityMesh, color, tempMaterial);

                if (SceneObjectRequestMode == RenderMode.QuadWithMask)
                {
                    ApplyQuadRegionMask(quad, gameObjectToReturn, color.Value);
                }

                switch (suObject.Kind)
                {
                    case SceneObjectKind.Background:
                        if (AddCollidersInBackgroundSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.Ceiling:
                        if (AddCollidersCeilingSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.CompletelyInferred:
                        if (AddCollidersInCompletelyInferredSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.Floor:
                        if (AddCollidersInFloorSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.Platform:
                        if (AddCollidersInPlatformSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.Unknown:
                        if (AddCollidersInUnknownSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                    case SceneObjectKind.Wall:
                        if (AddCollidersInWallSceneObjects)
                        {
                            gameObjectToReturn.AddComponent<BoxCollider>();
                        }
                        break;
                }

                //If the render mode isn't Quad mode disable the gameobject
                if (SceneObjectRequestMode != RenderMode.Quad && SceneObjectRequestMode != RenderMode.QuadWithMask)
                {
                    gameObjectToReturn.SetActive(false);
                }

                // Add to list
                listOfGeometryGameObjToReturn.Add(gameObjectToReturn);
            }
        }
        // Then Create the Planar Meshes Scene Objects
        {
            // If the Request Settings are requesting Meshes or WireFrame, create a gameobject in unity for
            // each Mesh, and apply either the default material or the wireframe material
            for (int i = 0; i < suObject.Meshes.Count; i++)
            {
                SceneMesh suGeometryMesh = suObject.Meshes[i];
                SceneMesh suColliderMesh = suObject.ColliderMeshes[i];

                // Generate the unity mesh for the Scene Understanding mesh.
                Mesh unityMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suGeometryMesh });
                GameObject gameObjectToReturn = new GameObject(suObject.Kind.ToString() + "Mesh");
                gameObjectToReturn.layer = layer;

                Material tempMaterial = GetMaterial(suObject.Kind, SceneObjectRequestMode);

                // Add the created Mesh into the Unity Object
                AddMeshToUnityObject(gameObjectToReturn, unityMesh, color, tempMaterial);

                switch (suObject.Kind)
                {
                    case SceneObjectKind.Background:
                        if (AddCollidersInBackgroundSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.Ceiling:
                        if (AddCollidersCeilingSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.CompletelyInferred:
                        if (AddCollidersInCompletelyInferredSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.Floor:
                        if (AddCollidersInFloorSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.Platform:
                        if (AddCollidersInPlatformSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.Unknown:
                        if (AddCollidersInUnknownSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                    case SceneObjectKind.Wall:
                        if (AddCollidersInWallSceneObjects)
                        {
                            // Generate a unity mesh for physics
                            Mesh unityColliderMesh = GenerateUnityMeshFromSceneObjectMeshes(new List<SceneMesh> { suColliderMesh });
                            MeshCollider col = gameObjectToReturn.AddComponent<MeshCollider>();
                            col.sharedMesh = unityColliderMesh;
                        }
                        break;
                }

                //If the render mode isn't Mesh or WireFrame mode disable the gameobject
                if (SceneObjectRequestMode != RenderMode.Mesh && SceneObjectRequestMode != RenderMode.Wireframe)
                {
                    gameObjectToReturn.SetActive(false);
                }

                // Add to list
                listOfGeometryGameObjToReturn.Add(gameObjectToReturn);
            }
        }

        // Return all the Geometry GameObjects that represent a Scene
        // Understanding Object
        return listOfGeometryGameObjToReturn;
    }

    /// <summary>
    /// Create a unity Mesh from a set of Scene Understanding Meshes
    /// </summary>
    /// <param name="suMeshes">The Scene Understanding mesh to generate in Unity</param>
    private Mesh GenerateUnityMeshFromSceneObjectMeshes(IEnumerable<SceneMesh> suMeshes)
    {
        if (suMeshes == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.GenerateUnityMeshFromSceneObjectMeshes: Meshes is null.");
            return null;
        }

        // Retrieve the data and store it as Indices and Vertices
        List<int> combinedMeshIndices = new List<int>();
        List<Vector3> combinedMeshVertices = new List<Vector3>();

        foreach (SceneMesh suMesh in suMeshes)
        {
            if (suMesh == null)
            {
                Debug.LogWarning("SceneUnderstandingScripts.GenerateUnityMeshFromSceneObjectMeshes: Mesh is null.");
                continue;
            }

            uint[] meshIndices = new uint[suMesh.TriangleIndexCount];
            suMesh.GetTriangleIndices(meshIndices);

            System.Numerics.Vector3[] meshVertices = new System.Numerics.Vector3[suMesh.VertexCount];
            suMesh.GetVertexPositions(meshVertices);

            uint indexOffset = (uint)combinedMeshVertices.Count;

            // Store the Indices and Vertices
            for (int i = 0; i < meshVertices.Length; i++)
            {
                // Here Z is negated because Unity Uses Left handed Coordinate system and Scene Understanding uses Right Handed
                combinedMeshVertices.Add(new Vector3(meshVertices[i].X, meshVertices[i].Y, -meshVertices[i].Z));
            }

            for (int i = 0; i < meshIndices.Length; i++)
            {
                combinedMeshIndices.Add((int)(meshIndices[i] + indexOffset));
            }
        }

        Mesh unityMesh = new Mesh();

        // Unity has a limit of 65,535 vertices in a mesh.
        // This limit exists because by default Unity uses 16-bit index buffers.
        // Starting with 2018.1, Unity allows one to use 32-bit index buffers.
        if (combinedMeshVertices.Count > 65535)
        {
            Debug.Log("SceneUnderstandingScripts.GenerateUnityMeshForSceneObjectMeshes: CombinedMeshVertices count is " + combinedMeshVertices.Count + ". Will be using a 32-bit index buffer.");
            unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        if (AlignSUObjectsNormalToUnityYAxis)
        {
            //Rotate our Vertex Data to match our Object's Normal vector to Unity's coordinate system Up Axis (Y axis)
            Quaternion rot = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            for (int i = 0; i < combinedMeshVertices.Count; i++)
            {
                combinedMeshVertices[i] = rot * combinedMeshVertices[i];
            }
        }

        // Apply the Indices and Vertices
        unityMesh.SetVertices(combinedMeshVertices);
        unityMesh.SetIndices(combinedMeshIndices.ToArray(), MeshTopology.Triangles, 0);
        unityMesh.RecalculateNormals();

        return unityMesh;
    }

    /// <summary>
    /// Create a Unity Mesh from a Scene Understanding Quad
    /// </summary>
    /// <param name="suQuad">The Scene Understanding quad to generate in Unity</param>
    private Mesh GenerateUnityMeshFromSceneObjectQuad(SceneQuad suQuad)
    {
        if (suQuad == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.GenerateUnityMeshForSceneObjectQuad: Quad is null.");
            return null;
        }

        float widthInMeters = suQuad.Extents.X;
        float heightInMeters = suQuad.Extents.Y;

        // Bounds of the quad.
        List<Vector3> vertices = new List<Vector3>()
            {
                new Vector3(-widthInMeters / 2, -heightInMeters / 2, 0),
                new Vector3( widthInMeters / 2, -heightInMeters / 2, 0),
                new Vector3(-widthInMeters / 2,  heightInMeters / 2, 0),
                new Vector3( widthInMeters / 2,  heightInMeters / 2, 0)
            };

        List<int> triangles = new List<int>()
            {
                1, 3, 0,
                3, 2, 0
            };

        List<Vector2> uvs = new List<Vector2>()
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

        if (AlignSUObjectsNormalToUnityYAxis)
        {
            // Rotate our Vertex Data to match our Object's Normal vector to Unity's coordinate system Up Axis (Y axis)
            Quaternion rot = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = rot * vertices[i];
                uvs[i] = rot * uvs[i];
            }

        }

        Mesh unityMesh = new Mesh();
        unityMesh.SetVertices(vertices);
        unityMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
        unityMesh.SetUVs(0, uvs);
        unityMesh.RecalculateNormals();

        return unityMesh;
    }

    /// <summary>
    /// Get the corresponding color for each SceneObject Kind
    /// </summary>
    /// <param name="kind">The Scene Understanding kind from which to query the color</param>
    private Color? GetColor(SceneObjectKind kind)
    {
        switch (kind)
        {
            case SceneObjectKind.Background:
                return ColorForBackgroundObjects;
            case SceneObjectKind.Wall:
                return ColorForWallObjects;
            case SceneObjectKind.Floor:
                return ColorForFloorObjects;
            case SceneObjectKind.Ceiling:
                return ColorForCeilingObjects;
            case SceneObjectKind.Platform:
                return ColorForPlatformsObjects;
            case SceneObjectKind.Unknown:
                return ColorForUnknownObjects;
            case SceneObjectKind.CompletelyInferred:
                return ColorForInferredObjects;
            case SceneObjectKind.World:
                return ColorForWorldObjects;
            default:
                return null;
        }
    }

    /// <summary>
    /// Get the corresponding layer for each SceneObject Kind
    /// </summary>
    /// <param name="kind">The Scene Understanding kind from which to query the layer</param>
    private int GetLayer(SceneObjectKind kind)
    {
        switch (kind)
        {
            case SceneObjectKind.Background:
                return LayerForBackgroundObjects;
            case SceneObjectKind.Wall:
                return LayerForWallObjects;
            case SceneObjectKind.Floor:
                return LayerForFloorObjects;
            case SceneObjectKind.Ceiling:
                return LayerForCeilingObjects;
            case SceneObjectKind.Platform:
                return LayerForPlatformsObjects;
            case SceneObjectKind.Unknown:
                return LayerForUnknownObjects;
            case SceneObjectKind.CompletelyInferred:
                return LayerForInferredObjects;
            case SceneObjectKind.World:
                return LayerForWorldObjects;
            default:
                return 0;
        }
    }


    /// <summary>
    /// Get the cached material for each SceneObject Kind
    /// </summary>
    /// <param name="kind">
    /// The <see cref="SceneObjectKind"/> to obtain the material for.
    /// </param>
    /// <param name="mode">
    /// The <see cref="RenderMode"/> to obtain the material for.
    /// </param>
    /// <remarks>
    /// If <see cref="IsInGhostMode"/> is true, the ghost material will be returned.
    /// </remarks>
    private Material GetMaterial(SceneObjectKind kind, RenderMode mode)
    {
        // If in ghost mode, just return transparent
        if (IsInGhostMode) { return TransparentOcclussion; }

        // Make sure we have a cache
        if (materialCache == null) { materialCache = new Dictionary<SceneObjectKind, Dictionary<RenderMode, Material>>(); }

        // Find or create cache specific to this Kind
        Dictionary<RenderMode, Material> kindModeCache;
        if (!materialCache.TryGetValue(kind, out kindModeCache))
        {
            kindModeCache = new Dictionary<RenderMode, Material>();
            materialCache[kind] = kindModeCache;
        }

        // Find or create material specific to this Mode
        Material mat;
        if (!kindModeCache.TryGetValue(mode, out mat))
        {
            // Determine the source material by kind
            Material sourceMat;
            switch (mode)
            {
                case RenderMode.Quad:
                case RenderMode.QuadWithMask:
                    sourceMat = GetSceneObjectSourceMaterial(RenderMode.Quad, kind);
                    break;
                case RenderMode.Wireframe:
                    sourceMat = SceneObjectWireframeMaterial;
                    break;
                default:
                    sourceMat = GetSceneObjectSourceMaterial(RenderMode.Mesh, kind);
                    break;
            }

            // Create an instance
            mat = Instantiate(sourceMat);

            // Set color to match the kind
            Color? color = GetColor(kind);
            if (color != null)
            {
                mat.color = color.Value;
                mat.SetColor("_WireColor", color.Value);
            }

            // Store
            kindModeCache[mode] = mat;
        }

        // Return the found or created material
        return mat;
    }

    /// <summary>
    /// Returns the correct Material for Rendering an SU Object as 
    /// a GameObject corresponding to its SU type and the Rendering Mode
    /// of the app
    /// </summary>
    /// <param name="mode">The Render Mode of the app, Mesh mode or Quad mode </param>
    /// <param name="kind">The Type of SU Object (Wall, Floor etc)</param>
    /// <returns></returns>
    Material GetSceneObjectSourceMaterial(RenderMode mode, SceneObjectKind kind)
    {
        if (mode == RenderMode.Quad)
        {
            switch (kind)
            {
                case SceneObjectKind.World:
                    return SceneObjectWireframeMaterial;
                case SceneObjectKind.Platform:
                    return SceneObjectPlatformQuadMaterial;
                case SceneObjectKind.Background:
                    return SceneObjectBackgroundQuadMaterial;
                case SceneObjectKind.Unknown:
                    return SceneObjectUnknownQuadMaterial;
                case SceneObjectKind.CompletelyInferred:
                    return SceneObjectInferredQuadMaterial;
                case SceneObjectKind.Ceiling:
                    return SceneObjectCeilingQuadMaterial;
                case SceneObjectKind.Floor:
                    return SceneObjectFloorQuadMaterial;
                case SceneObjectKind.Wall:
                    return SceneObjectWallQuadMaterial;
                default:
                    return SceneObjectWireframeMaterial;
            }
        }
        else // RenderMode == Mesh
        {
            switch (kind)
            {
                case SceneObjectKind.World:
                    return SceneObjectWireframeMaterial;
                case SceneObjectKind.Platform:
                    return SceneObjectPlatformMeshMaterial;
                case SceneObjectKind.Background:
                    return SceneObjectBackgroundMeshMaterial;
                case SceneObjectKind.Unknown:
                    return SceneObjectUnknownMeshMaterial;
                case SceneObjectKind.CompletelyInferred:
                    return SceneObjectInferredMeshMaterial;
                case SceneObjectKind.Ceiling:
                    return SceneObjectCeilingMeshMaterial;
                case SceneObjectKind.Floor:
                    return SceneObjectFloorMeshMaterial;
                case SceneObjectKind.Wall:
                    return SceneObjectWallMeshMaterial;
                default:
                    return SceneObjectWireframeMaterial;
            }
        }
    }


    /// <summary>
    /// Function to add a Mesh to a Unity Object
    /// </summary>
    /// <param name="unityObject">The unity object to where the mesh will be applied </param>
    /// <param name="mesh"> Mesh to be applied                                       </param>
    /// <param name="color"> Color to apply to the Mesh                              </param>
    /// <param name="material"> Material to apply to the unity Mesh Renderer         </param>
    private void AddMeshToUnityObject(GameObject unityObject, Mesh mesh, Color? color, Material material)
    {
        if (unityObject == null || mesh == null || material == null)
        {
            Debug.Log("SceneUnderstandingScripts.AddMeshToUnityObject: One or more arguments are null");
        }

        MeshFilter mf = unityObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = unityObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
    }

    /// <summary>
    /// Apply Region mask to a Scene Object
    /// </summary>
    private void ApplyQuadRegionMask(SceneQuad quad, GameObject gameobject, Color color)
    {
        if (quad == null || gameobject == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.ApplyQuadRegionMask: One or more arguments are null.");
            return;
        }

        // Resolution of the mask.
        ushort width = 256;
        ushort height = 256;

        byte[] mask = new byte[width * height];
        quad.GetSurfaceMask(width, height, mask);

        MeshRenderer meshRenderer = gameobject.GetComponent<MeshRenderer>();
        if (meshRenderer == null || meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.HasProperty("_MainTex") == false)
        {
            Debug.LogWarning("SceneUnderstandingScripts.ApplyQuadRegionMask: Mesh renderer component is null or does not have a valid material.");
            return;
        }

        // Create a new texture.
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        // Transfer the invalidation mask onto the texture.
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; ++i)
        {
            byte value = mask[i];

            if (value == (byte)SceneRegionSurfaceKind.NotSurface)
            {
                pixels[i] = Color.clear;
            }
            else
            {
                pixels[i] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true);

        // Set the texture on the material.
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Function to destroy all children under a Unity Transform
    /// </summary>
    /// <param name="parentTransform"> Parent Transform to remove children from </param>
    private void DestroyAllGameObjectsUnderParent(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.DestroyAllGameObjectsUnderParent: Parent is null.");
            return;
        }

        foreach (Transform child in parentTransform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Function to return the correspoding transformation matrix to pass geometry
    /// from the Scene Understanding Coordinate System to the Unity one
    /// </summary>
    /// <param name="scene"> Scene from which to get the Scene Understanding Coordinate System </param>
    private System.Numerics.Matrix4x4? GetSceneToUnityTransformAsMatrix4x4(Scene scene)
    {
        System.Numerics.Matrix4x4? sceneToUnityTransform = System.Numerics.Matrix4x4.Identity;

        if (RunOnDevice)
        {
            SpatialCoordinateSystem sceneCoordinateSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(scene.OriginSpatialGraphNodeId);
            SpatialCoordinateSystem unityCoordinateSystem = SpatialCoordinateSystem.FromNativePtr(UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem);

            sceneToUnityTransform = sceneCoordinateSystem.TryGetTransformTo(unityCoordinateSystem);

            if (sceneToUnityTransform != null)
            {
                sceneToUnityTransform = ConvertRightHandedMatrix4x4ToLeftHanded(sceneToUnityTransform.Value);
            }
            else
            {
                Debug.LogWarning("SceneUnderstandingScripts.GetSceneToUnityTransform: Scene to Unity transform is null.");
            }
        }

        return sceneToUnityTransform;
    }

    /// <summary>
    /// Converts a right handed tranformation matrix into a left handed one
    /// </summary>
    /// <param name="matrix"> Matrix to convert </param>
    private System.Numerics.Matrix4x4 ConvertRightHandedMatrix4x4ToLeftHanded(System.Numerics.Matrix4x4 matrix)
    {
        matrix.M13 = -matrix.M13;
        matrix.M23 = -matrix.M23;
        matrix.M43 = -matrix.M43;

        matrix.M31 = -matrix.M31;
        matrix.M32 = -matrix.M32;
        matrix.M34 = -matrix.M34;

        return matrix;
    }

    /// <summary>
    /// Passes all the values from a 4x4 tranformation matrix into a Unity Tranform
    /// </summary>
    /// <param name="targetTransform"> Transform to pass the values into                                    </param>
    /// <param name="matrix"> Matrix from which the values to pass are gathered                             </param>
    /// <param name="updateLocalTransformOnly"> Flag to update local transform or global transform in unity </param>
    private void SetUnityTransformFromMatrix4x4(Transform targetTransform, System.Numerics.Matrix4x4 matrix, bool updateLocalTransformOnly = false)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.SetUnityTransformFromMatrix4x4: Unity transform is null.");
            return;
        }

        Vector3 unityTranslation;
        Quaternion unityQuat;
        Vector3 unityScale;

        System.Numerics.Vector3 vector3;
        System.Numerics.Quaternion quaternion;
        System.Numerics.Vector3 scale;

        System.Numerics.Matrix4x4.Decompose(matrix, out scale, out quaternion, out vector3);

        unityTranslation = new Vector3(vector3.X, vector3.Y, vector3.Z);
        unityQuat = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        unityScale = new Vector3(scale.X, scale.Y, scale.Z);

        if (updateLocalTransformOnly)
        {
            targetTransform.localPosition = unityTranslation;
            targetTransform.localRotation = unityQuat;
        }
        else
        {
            targetTransform.SetPositionAndRotation(unityTranslation, unityQuat);
        }
    }

    /// <summary>
    /// Orients a GameObject relative to Unity's Up vector and Scene Understanding's Largest floor's normal vector
    /// </summary>
    /// <param name="sceneRoot"> Unity object to orient                       </param>
    /// <param name="suScene"> SU object to obtain the largest floor's normal </param>
    private void OrientSceneForPC(GameObject sceneRoot, Scene suScene)
    {
        if (suScene == null)
        {
            Debug.Log("SceneUnderstandingScripts.OrientSceneForPC: Scene Understanding Scene Data is null.");
        }

        IEnumerable<SceneObject> sceneObjects = suScene.SceneObjects;

        float largestFloorAreaFound = 0.0f;
        SceneObject suLargestFloorObj = null;
        SceneQuad suLargestFloorQuad = null;
        foreach (SceneObject sceneObject in sceneObjects)
        {
            if (sceneObject.Kind == SceneObjectKind.Floor)
            {
                IEnumerable<SceneQuad> quads = sceneObject.Quads;

                if (quads != null)
                {
                    foreach (SceneQuad quad in quads)
                    {
                        float quadArea = quad.Extents.X * quad.Extents.Y;

                        if (quadArea > largestFloorAreaFound)
                        {
                            largestFloorAreaFound = quadArea;
                            suLargestFloorObj = sceneObject;
                            suLargestFloorQuad = quad;
                        }
                    }
                }
            }
        }

        if (suLargestFloorQuad != null)
        {
            float quadWith = suLargestFloorQuad.Extents.X;
            float quadHeight = suLargestFloorQuad.Extents.Y;

            System.Numerics.Vector3 p1 = new System.Numerics.Vector3(-quadWith / 2, -quadHeight / 2, 0);
            System.Numerics.Vector3 p2 = new System.Numerics.Vector3(quadWith / 2, -quadHeight / 2, 0);
            System.Numerics.Vector3 p3 = new System.Numerics.Vector3(-quadWith / 2, quadHeight / 2, 0);

            System.Numerics.Matrix4x4 floorTransform = suLargestFloorObj.GetLocationAsMatrix();
            floorTransform = ConvertRightHandedMatrix4x4ToLeftHanded(floorTransform);

            System.Numerics.Vector3 tp1 = System.Numerics.Vector3.Transform(p1, floorTransform);
            System.Numerics.Vector3 tp2 = System.Numerics.Vector3.Transform(p2, floorTransform);
            System.Numerics.Vector3 tp3 = System.Numerics.Vector3.Transform(p3, floorTransform);

            System.Numerics.Vector3 p21 = tp2 - tp1;
            System.Numerics.Vector3 p31 = tp3 - tp1;

            System.Numerics.Vector3 floorNormal = System.Numerics.Vector3.Cross(p31, p21);

            Vector3 floorNormalUnity = new Vector3(floorNormal.X, floorNormal.Y, floorNormal.Z);

            Quaternion rotation = Quaternion.FromToRotation(floorNormalUnity, Vector3.up);
            SceneRoot.transform.rotation = rotation;
        }
    }


    #endregion

    #region Out of PlayMode Functions

    /// <summary>
    /// This function will generate the Unity Scene that represents the Scene
    /// Understanding Scene without needing to use the play button
    /// </summary>
    public void BakeScene()
    {
        Debug.Log("[IN EDITOR] SceneUnderstandingScripts.BakeScene: Bake Started");
        DestroyImmediate(SceneRoot.gameObject);
        if (!QuerySceneFromDevice)
        {
            SceneRoot = SceneRoot == null ? new GameObject("Scene Root") : SceneRoot;
            Scene suScene = null;

            foreach (TextAsset serializedScene in SUSerializedScenePaths)
            {
                if (serializedScene)
                {
                    byte[] sceneBytes = serializedScene.bytes;
                    SceneFragment frag = SceneFragment.Deserialize(sceneBytes);
                    SceneFragment[] sceneFragmentsArray = new SceneFragment[1] { frag };
                    suScene = Scene.FromFragments(sceneFragmentsArray);
                }
            }

            if (suScene != null)
            {
                System.Numerics.Matrix4x4? sceneToUnityTransformAsMatrix4x4 = GetSceneToUnityTransformAsMatrix4x4(suScene);

                if (sceneToUnityTransformAsMatrix4x4 != null)
                {
                    SetUnityTransformFromMatrix4x4(SceneRoot.transform, sceneToUnityTransformAsMatrix4x4.Value, RunOnDevice);

                    if (!RunOnDevice)
                    {
                        OrientSceneForPC(SceneRoot, suScene);
                    }

                    IEnumerable<SceneObject> sceneObjects = suScene.SceneObjects;
                    foreach (SceneObject sceneObject in sceneObjects)
                    {
                        DisplaySceneObject(sceneObject);
                    }
                }
            }

            Debug.Log("[IN EDITOR] SceneUnderstandingScripts.BakeScene: Display Completed");
        }
    }

    #endregion

    #region Save To Disk Functions

    /// <summary>
    /// Get the latest bytes from a Scene Queried from device
    /// </summary>
    private byte[] GetLatestSceneBytes()
    {
        byte[] sceneBytes = null;
        lock (SUDataLock)
        {
            if (LatestSUSceneData != null)
            {
                int sceneLength = LatestSUSceneData.Length;
                sceneBytes = new byte[sceneLength];

                Array.Copy(LatestSUSceneData, sceneBytes, sceneLength);
            }
        }

        return sceneBytes;
    }

    /// <summary>
    /// Save a serialized scene bytes to disk
    /// </summary>
    // Await is conditionally compiled out based on platform but needs to be awaitable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task SaveBytesToDiskAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        DateTime currentDate = DateTime.Now;
        int year = currentDate.Year;
        int month = currentDate.Month;
        int day = currentDate.Day;
        int hour = currentDate.Hour;
        int min = currentDate.Minute;
        int sec = currentDate.Second;

        if (QuerySceneFromDevice)
        {
            string fileName = string.Format("SU_{0}-{1}-{2}_{3}-{4}-{5}.bytes",
                                            year, month, day, hour, min, sec);

            byte[] OnDeviceBytes = GetLatestSceneBytes();

#if WINDOWS_UWP
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                await Windows.Storage.FileIO.WriteBytesAsync(file, OnDeviceBytes);
#else
            Debug.Log("Save on Device is only supported in Universal Windows Applications");
#endif
        }
        else
        {
            int fragmentNumber = 0;
            foreach (TextAsset serializedScene in SUSerializedScenePaths)
            {
                byte[] fragmentBytes = serializedScene.bytes;

                string fileName = string.Format("SU_Frag{0}-{1}-{2}-{3}_{4}-{5}-{6}.bytes",
                                                fragmentNumber++, year, month, day, hour, min, sec);

                string folder = Path.GetTempPath();
                string file = Path.Combine(folder, fileName);
                File.WriteAllBytes(file, fragmentBytes);
                Debug.Log("SceneUnderstandingScripts.SaveBytesToDisk: Scene Fragment saved at " + file);
            }
        }
    }

    /// <summary>
    /// Save the generated Unity Objects from Scene Understanding as Obj files
    /// to disk
    /// </summary>
    public async Task SaveObjsToDiskAsync()
    {
        DateTime currentDate = DateTime.Now;
        int year = currentDate.Year;
        int month = currentDate.Month;
        int day = currentDate.Day;
        int hour = currentDate.Hour;
        int min = currentDate.Minute;
        int sec = currentDate.Second;

        // List of all SceneObjectKind enum values.
        List<SceneObjectKind> sceneObjectKinds = new List<SceneObjectKind>();
        sceneObjectKinds.Add(SceneObjectKind.Background);
        sceneObjectKinds.Add(SceneObjectKind.Ceiling);
        sceneObjectKinds.Add(SceneObjectKind.CompletelyInferred);
        sceneObjectKinds.Add(SceneObjectKind.Floor);
        sceneObjectKinds.Add(SceneObjectKind.Platform);
        sceneObjectKinds.Add(SceneObjectKind.Unknown);
        sceneObjectKinds.Add(SceneObjectKind.Wall);
        sceneObjectKinds.Add(SceneObjectKind.World);

        List<Task> tasks = new List<Task>();
        Scene scene = null;
        if (QuerySceneFromDevice)
        {
            SceneFragment sceneFragment = GetLatestSceneSerialization();
            if (sceneFragment == null)
            {
                Debug.LogWarning("SceneUnderstandingScripts.SaveObjsToDisk: Nothing to save.");
                return;
            }

            // Deserialize the scene.
            SceneFragment[] sceneFragmentsArray = new SceneFragment[1] { sceneFragment };
            scene = Scene.FromFragments(sceneFragmentsArray);
        }
        else
        {
            SceneFragment[] sceneFragments = new SceneFragment[SUSerializedScenePaths.Count];
            int index = 0;
            foreach (TextAsset serializedScene in SUSerializedScenePaths)
            {
                if (serializedScene != null)
                {
                    byte[] sceneData = serializedScene.bytes;
                    SceneFragment frag = SceneFragment.Deserialize(sceneData);
                    sceneFragments[index++] = frag;
                }
            }

            // Deserialize the scene.
            scene = Scene.FromFragments(sceneFragments);
        }

        if (scene == null)
        {
            Debug.LogWarning("SceneUnderstandingScripts.SaveObjsToDiskAsync: Scene is null");
            return;
        }

        foreach (SceneObjectKind soKind in sceneObjectKinds)
        {
            List<SceneObject> allObjectsOfAKind = new List<SceneObject>();
            foreach (SceneObject sceneObject in scene.SceneObjects)
            {
                if (sceneObject.Kind == soKind)
                {
                    allObjectsOfAKind.Add(sceneObject);
                }
            }

            string fileName = string.Format("SU_{0}_{1}-{2}-{3}_{4}-{5}-{6}.obj",
                                            soKind.ToString(), year, month, day, hour, min, sec);

            if (allObjectsOfAKind.Count > 0)
            {
                tasks.Add(SaveAllSceneObjectsOfAKindAsOneObj(allObjectsOfAKind, GetColor(soKind), fileName));
            }
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Save the generated Unity Objects from Scene Understanding as Obj files
    /// to disk (all objects of one kind as one obj file)
    /// </summary>
    private async Task SaveAllSceneObjectsOfAKindAsOneObj(List<SceneObject> sceneObjects, Color? color, string fileName)
    {
        if (sceneObjects == null)
        {
            return;
        }

        List<System.Numerics.Vector3> combinedMeshVertices = new List<System.Numerics.Vector3>();
        List<uint> combinedMeshIndices = new List<uint>();

        // Go through each scene object, retrieve its meshes and add them to the combined lists, defined above.
        foreach (SceneObject so in sceneObjects)
        {
            if (so == null)
            {
                continue;
            }

            IEnumerable<SceneMesh> meshes = so.Meshes;
            if (meshes == null)
            {
                continue;
            }

            foreach (SceneMesh mesh in meshes)
            {
                // Get the mesh vertices.
                var mvList = new System.Numerics.Vector3[mesh.VertexCount];
                mesh.GetVertexPositions(mvList);

                // Transform the vertices using the transformation matrix.
                TransformVertices(so.GetLocationAsMatrix(), mvList);

                // Store the current set of vertices in the combined list. As we add indices, we'll offset it by this value.
                uint indexOffset = (uint)combinedMeshVertices.Count;

                // Add the new set of mesh vertices to the existing set.
                combinedMeshVertices.AddRange(mvList);

                // Get the mesh indices.
                uint[] mi = new uint[mesh.TriangleIndexCount];
                mesh.GetTriangleIndices(mi);

                // Add the new set of mesh indices to the existing set.
                for (int i = 0; i < mi.Length; ++i)
                {
                    combinedMeshIndices.Add((uint)(mi[i] + indexOffset));
                }
            }
        }

        // Write as string to file.
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < combinedMeshVertices.Count; ++i)
        {
            sb.Append(string.Format("v {0} {1} {2} {3} {4} {5}\n", combinedMeshVertices[i].X, combinedMeshVertices[i].Y, combinedMeshVertices[i].Z, color.Value.r, color.Value.g, color.Value.b));
        }

        for (int i = 0; i < combinedMeshIndices.Count; i += 3)
        {
            // Indices start at index 1 (as opposed to 0) in objs.
            sb.Append(string.Format("f {0} {1} {2}\n", combinedMeshIndices[i] + 1, combinedMeshIndices[i + 1] + 1, combinedMeshIndices[i + 2] + 1));
        }

        await SaveStringToDiskAsync(sb.ToString(), fileName);
    }

    /// <summary>
    /// Save a string to disk
    /// this string is the obj file that represents the SU Geometry
    /// </summary>
    // Await is conditionally compiled out based on platform but needs to be awaitable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task SaveStringToDiskAsync(string data, string fileName)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        if (string.IsNullOrEmpty(data))
        {
            Debug.LogWarning("SceneUnderstandingScripts.SaveStringToDiskAsync: Nothing to save.");
            return;
        }

        if (QuerySceneFromDevice)
        {
#if WINDOWS_UWP
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
                await Windows.Storage.FileIO.AppendTextAsync(file, data);
#else
            Debug.Log("Save on Device is only supported in Universal Windows Applications");
#endif
        }
        else
        {
            string folder = Path.GetTempPath();
            string file = Path.Combine(folder, fileName);
            File.WriteAllText(file, data);
            Debug.Log("SceneUnderstandingScripts.SaveStringToDiskAsync: Scene Objects saved at " + file);
        }
    }

    private void TransformVertices(System.Numerics.Matrix4x4 transformationMatrix, System.Numerics.Vector3[] vertices)
    {
        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i] = System.Numerics.Vector3.Transform(vertices[i], transformationMatrix);
        }
    }

    #endregion

}
