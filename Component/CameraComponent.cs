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
        Camera followCam;
        Transform vrCameraTransform;

        private Vector3 velocity = Vector3.zero;
        private Vector3 offset = new Vector3(0, 2, -2);
        private static float maxRange = 4;


        private enum cameraFollowType
        {
            SuperHot,
            VanityFollow,
            StabilizedFPV
        }
        private static cameraFollowType _cameraFollowType = cameraFollowType.SuperHot;
        private static int cameraFollowTypeLength;


        void Start()
        {
            InitalizeCamera();
        }




        void Update()
        {

            if (Input.GetKeyDown("h"))
            {
                followCam.enabled = !followCam.enabled;

                if (followCam.enabled == true)
                {
                    ActiveCamera();
                }


                else
                {
                    Debug.Log("Camera is Disabled.");
                }


            }

            if (Input.GetKeyDown("j") & followCam.enabled)
            {
                _cameraFollowType++;

                if (cameraFollowTypeLength == (int)_cameraFollowType)
                {
                    _cameraFollowType = 0;

                }


                String currentCam = _cameraFollowType.ToString();
                Debug.Log(string.Format("Camera set to {0}", currentCam));
            }

        }

        void LateUpdate()
        {

            if (followCam.enabled && Player.m_localPlayer != null)
            {


                Transform HeadtargetTransform = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head);
                Transform localPlayerTransform = Player.m_localPlayer.GetTransform();
          

                switch (_cameraFollowType)
                {


                    case (cameraFollowType.SuperHot):
                        ActionCamera(localPlayerTransform, 100f, 2f, -2f);
                        break;



                    case (cameraFollowType.VanityFollow):
                        if (Player.m_localPlayer.IsAttachedToShip())
                        {
                            // Vector3 playerVelocity = Player.m_localPlayer.GetVelocity();
                            ActionCamera(localPlayerTransform, 100f, 3f, -3f);
                        }


                        else
                        {
                            // Character.GetCharactersInRange(localPlayerTransform, )
                            CameraCloseFollow(localPlayerTransform, 70f);
                        }
                        break;



                    case (cameraFollowType.StabilizedFPV):
                        if (vrCameraTransform == null)
                        {
                            vrCameraTransform = HeadtargetTransform;

                            foreach (Camera camera in FindObjectsOfType<Camera>())
                            {
                                if (camera.name == "VRCamera")
                                {
                                    Debug.Log("VRCamera Found");
                                    vrCameraTransform = camera.transform;
                                    Debug.Log("vrCameraTransform: "+ vrCameraTransform);
                                }

                               
                            }
                        }

                      
                       CameraStabilizedFPV(vrCameraTransform, 60f, false, 0.18f, 0.01f, 0.08f);
                        
                        break;
                }

            }

        }


        // The camera used in Super Hot's Spectator Camera. I reduced the FOV slightly, since it was absurdly high for Valheim. 
        // This one doesn't look good in bases, but it's great for sailing and action.

        private void ActionCamera(Transform targetTransform, float fieldOfView, float yOffset, float zOffset)
        {
            Vector3 targetPosition;

            followCam.fieldOfView = fieldOfView;
            targetPosition = targetTransform.position + targetTransform.forward * zOffset + targetTransform.up * yOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.9f * Time.unscaledDeltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTransform.rotation, 50f * Time.unscaledDeltaTime);

        }

        private void ActionCamera(Transform targetTransform, float fieldOfView, float yOffset)
        {
            ActionCamera(targetTransform, fieldOfView, yOffset, 0f);
        }



        // This camera is a standard third person camera that follows the player. It'll stop it's position follow when you're within a certain range of the camera.
        // It seems to have issues with jittering in some situations with transform.lookat, such as a slow moving boat that's heaving a lot. 

        private void CameraCloseFollow(Transform targetTransform, float fieldOfView, float yOffset, float zOffset, bool closeFollowStopWhenClose)
        {
            followCam.fieldOfView = fieldOfView;
            followCam.nearClipPlane = 0.01f;
            float distanceFromPlayer;
            Vector3 targetPosition;
            Transform headTransform = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head);

            distanceFromPlayer = (transform.position - targetTransform.position).sqrMagnitude;
            if (closeFollowStopWhenClose == false || distanceFromPlayer > maxRange * maxRange)
            {
                targetPosition = targetTransform.position + (targetTransform.forward * zOffset) + (targetTransform.up * yOffset);
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.7f);
            }

            //transform.LookAt(headTransform);
            Vector3 directionHeadVector = headTransform.position - transform.position;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(directionHeadVector), 70f * Time.unscaledDeltaTime);
        }

        private void CameraCloseFollow(Transform targetTransform, float fieldOfView, float yOffset)
        {
            CameraCloseFollow(targetTransform, fieldOfView, yOffset, -2f, true);
        }

        private void CameraCloseFollow(Transform targetTransform, float fieldOfView)
        {
            CameraCloseFollow(targetTransform, fieldOfView, 1.5f, -2f, true);
        }


        // A smoother FPV camera. VR footage is pretty terrible to look at unless it's stabalized. 
        // Taken from <https://github.com/Wyattari/VRSmoothCamUnity/blob/main/VRSmoothCamUnityProject/Assets/VRSmoothCam/Scripts/SmoothCamMethods.cs>. 
        // Dampening from 0 to 0.1f

        private void CameraStabilizedFPV(Transform targetTransform, float fieldOfView, bool lockRotation, float zOffset, float positionDampening, float rotationDampening)
        {
            followCam.nearClipPlane = 0.16f;
            var velocity = Vector3.zero;
            transform.position = Vector3.SmoothDamp(transform.position, targetTransform.position + targetTransform.forward * zOffset, ref velocity, positionDampening);

            float angularVelocity = 0f;
            float delta = Quaternion.Angle(transform.rotation, targetTransform.rotation);
            if (delta > 0f)
            {
                float t = Mathf.SmoothDampAngle(delta, 0.0f, ref angularVelocity, rotationDampening);
                t = 1.0f - (t / delta);


               
                if (lockRotation) // Broken, still working on it.
                {
                    /*
                    float rollCorrectionSpeed = 1f;

                   
                    float roll = Vector3.Dot(transform.right, Vector3.up);  
                    transform.Rotate(0, 0, -roll * rollCorrectionSpeed);

                    Vector3 directionVector = targetTransform.position - transform.position;
                    Quaternion lookRotation = Quaternion.LookRotation(directionVector);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTransform.rotation, (rotationDampening * 1000) * Time.deltaTime);
                    */

                    transform.rotation = Quaternion.LookRotation(targetTransform.forward);
                    // broken when looking up and down
                    //float lockZ = 0f;
                    // transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, lockZ);


                }

                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetTransform.rotation, t);
                }
            }

            

        }

        private void CameraStabilizedFPV(Transform targetTransform, float fieldOfView)
        {
            CameraStabilizedFPV(targetTransform, fieldOfView, false, 0f, 0.05f, 0.05f);
        }

        private void InitalizeCamera()
        {
            followerCameraObject = this.gameObject;
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

                    followCam.nearClipPlane = camera.nearClipPlane;
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

                    Debug.Log("Near Plane Clip: " + camera.nearClipPlane);


                }

            }
        }

        private void ActiveCamera()
        {
            Debug.Log("Camera " + _cameraFollowType.ToString() + " is Enabled.");
            Debug.Log(followerCameraObject);

            if (Player.m_localPlayer != null)
            {
                Vector3 playerPosition = Player.m_localPlayer.transform.position;
                transform.position = new Vector3(playerPosition.x, playerPosition.y + offset.y, playerPosition.z + offset.z);

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
                    // if (ppb.enabled) ppb.enabled = false;
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






