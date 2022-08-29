using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.PostProcessing;
using System;
using System.Reflection;


namespace VHVRCamera
{

    public class CameraComponent : MonoBehaviour
    {
        GameObject followerCameraObject;
        Transform targetTransform;
        Camera followCam;

        private Vector3 velocity = Vector3.zero;
        private Vector3 offset = new Vector3(0, 2, -2);
        private static float maxRange = 4;


        private enum cameraFollowType
        {
            SuperHot,
            VanityFollow
        }
        private static cameraFollowType _cameraFollowType = cameraFollowType.SuperHot;
        private static int cameraFollowTypeLength;


        void Start()
        {
            followerCameraObject = this.gameObject;
            targetTransform = new GameObject().transform;
            cameraFollowTypeLength = Enum.GetNames(typeof(cameraFollowType)).Length;

            followCam = GetComponent<Camera>();
            followCam.stereoTargetEye = StereoTargetEyeMask.None;
            followCam.fieldOfView = 100f;
            followCam.targetDisplay = 0;
            followCam.depth = 500;
            followCam.nearClipPlane = 0.01f;
            followCam.enabled = false;

            foreach (Camera camera in FindObjectsOfType<Camera>())
            {
                Debug.Log(camera.name);
                if (camera.name == "Main Camera")
                {
                    Debug.Log("Main Camera Found");

                    followCam.farClipPlane = camera.farClipPlane;
                    followCam.clearFlags = camera.clearFlags;
                    followCam.renderingPath = camera.renderingPath;
                    followCam.clearStencilAfterLightingPass = camera.clearStencilAfterLightingPass;
                    followCam.depthTextureMode = camera.depthTextureMode;
                    followCam.layerCullDistances = camera.layerCullDistances;
                    followCam.layerCullSpherical = camera.layerCullSpherical;
                    followCam.cullingMask = camera.cullingMask;
                    followCam.useOcclusionCulling = camera.useOcclusionCulling;
                    followCam.allowHDR = true;
                    followCam.backgroundColor = camera.backgroundColor;



                }

            }

        }




        void Update()
        {

            if (Input.GetKeyDown("h"))
            {
                Debug.Log("Key H Press");
                followCam.enabled = !followCam.enabled;

                if (followCam.enabled == true)
                {

                    _cameraFollowType++;
                    if (cameraFollowTypeLength == (int)_cameraFollowType)
                    {
                        _cameraFollowType = 0;

                    }
                    Debug.Log("Camera " + _cameraFollowType.ToString() + " is Enabled.");
                    Debug.Log(followerCameraObject);

                    if (Player.m_localPlayer != null)
                    {
                        Vector3 playerPosition = Player.m_localPlayer.transform.position;
                        transform.position = new Vector3(playerPosition.x, playerPosition.y + offset.y, playerPosition.z + offset.z);
                        // transform.position = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head).transform.position;

                        // transform.position = playerPosition;
                    }

                    if (followCam.gameObject.GetComponent<PostProcessingBehaviour>() == null)
                    {
                        foreach (Camera camera in FindObjectsOfType<Camera>())
                        {
                            Debug.Log(camera.name);
                            if (camera.name == "Main Camera")
                            {
                                maybeCopyPostProcessingEffects(followCam, camera);
                            }
                        }
                    }

                    else
                    {
                        Debug.Log("Camera is Disabled.");
                    }
                }


            }
        }

        void LateUpdate()
        {
            Vector3 targetPosition;
            if (targetTransform == null)
            {
                if (Player.m_localPlayer != null)
                {
                    targetTransform = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head);
                }

            }

            else if (followCam.enabled)
            {

                float distanceFromPlayer;
                Transform localPlayerTransform = Player.m_localPlayer.GetTransform();
                Transform headTransform = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head);

                switch (_cameraFollowType)
                {

                    // The camera used in Super Hot's Spectator Camera. I reduced the FOV slightly, since it was absurdly high for Valheim. 
                    // This one doesn't look good in bases, but it's great for sailing and action. 
                    case (cameraFollowType.SuperHot):
                        followCam.fieldOfView = 100f;
                        targetTransform = localPlayerTransform;
                        offset.y = 2;
                        targetPosition = targetTransform.position + targetTransform.forward * offset.z + targetTransform.up * offset.y;
                        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.9f * Time.unscaledDeltaTime);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTransform.rotation, 50f * Time.unscaledDeltaTime);
                        break;

                    // This camera is a standard third person camera that follows the player. It'll stop it's position follow when you're within a certain range of the camera.
                    // It seems to have issues with jittering in some situations, such as a slow moving boat that's heaving a lot. 

                    case (cameraFollowType.VanityFollow):
                        followCam.fieldOfView = 70f;
                        offset.y = 1.5f;
                        targetTransform = localPlayerTransform;
                        distanceFromPlayer = (transform.position - targetTransform.position).sqrMagnitude;
                        if (distanceFromPlayer > maxRange * maxRange)
                        {
                            targetPosition = targetTransform.position + (targetTransform.forward * offset.z) + (targetTransform.up * offset.y);
                            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.9f);
                        }

                        transform.LookAt(headTransform);
                        break;

                }

            }

        }


        // Shamelessly taken from <https://github.com/brandonmousseau/vhvr-mod>
        private void maybeCopyPostProcessingEffects(Camera targetCamera, Camera sourceCamera)
        {
            if (targetCamera == null || sourceCamera == null)
            {
                Debug.LogWarning("Null Camera");
                return;
            }
            if (targetCamera.gameObject.GetComponent<PostProcessingBehaviour>() != null)
            {
                Debug.LogWarning("Not Null Post Processing Behaviour");
                return;
            }
            PostProcessingBehaviour postProcessingBehavior = null;
            bool foundMainCameraPostProcesor = false;
            foreach (var ppb in GameObject.FindObjectsOfType<PostProcessingBehaviour>())
            {
                if (ppb.name == "Main Camera")
                {
                    foundMainCameraPostProcesor = true;
                    postProcessingBehavior = targetCamera.gameObject.AddComponent<PostProcessingBehaviour>();
                    Debug.Log("Copying Main Camera PostProcessingBehaviour");
                    var profileClone = Instantiate(ppb.profile);
                    //Need to copy only the profile and jitterFuncMatrix, everything else will be instanciated when enabled
                    postProcessingBehavior.profile = profileClone;
                    postProcessingBehavior.jitteredMatrixFunc = ppb.jitteredMatrixFunc;
                    if (ppb.enabled) ppb.enabled = false;
                }
            }
            if (!foundMainCameraPostProcesor)
            {
                Debug.LogWarning("Main Camera Post Processor not found.");
                return;
            }
            var mainCamDepthOfField = sourceCamera.gameObject.GetComponent<DepthOfField>();
            var followCamDepthOfField = targetCamera.gameObject.AddComponent<DepthOfField>();
            if (mainCamDepthOfField != null)
            {
                CopyClassFields(mainCamDepthOfField, ref followCamDepthOfField);
            }
            var followCamSunshaft = targetCamera.gameObject.AddComponent<SunShafts>();
            var mainCamSunshaft = sourceCamera.gameObject.GetComponent<SunShafts>();
            if (mainCamSunshaft != null)
            {
                CopyClassFields(mainCamSunshaft, ref followCamSunshaft);
            }
            var followCamEffects = targetCamera.gameObject.AddComponent<CameraEffects>();
            var mainCamEffects = sourceCamera.gameObject.GetComponent<CameraEffects>();
            if (mainCamEffects != null)
            {
                // Need to copy over only the DOF fields
                followCamEffects.m_forceDof = mainCamEffects.m_forceDof;
                followCamEffects.m_dofRayMask = mainCamEffects.m_dofRayMask;
                followCamEffects.m_dofAutoFocus = mainCamEffects.m_dofAutoFocus;
                followCamEffects.m_dofMinDistance = mainCamEffects.m_dofMinDistance;
                followCamEffects.m_dofMaxDistance = mainCamEffects.m_dofMaxDistance;
            }
        }

        private void CopyClassFields<T>(T source, ref T dest)
        {
            FieldInfo[] fieldsToCopy = source.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fieldsToCopy)
            {
                var value = field.GetValue(source);
                field.SetValue(dest, value);
            }
        }



    }



}






