using UnityEngine;
using System;

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
                    followCam.cullingMask = camera.cullingMask;
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
                        transform.position =  new Vector3(playerPosition.x, playerPosition.y + offset.y, playerPosition.z + offset.z);
                        // transform.position = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head).transform.position;

                       // transform.position = playerPosition;
                    }
                }

                else
                {
                    Debug.Log("Camera is Disabled.");
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

                switch (_cameraFollowType) {

                    case (cameraFollowType.SuperHot):

                        targetTransform = localPlayerTransform;
                        offset.y = 2;
                        targetPosition = targetTransform.position + targetTransform.forward * offset.z + targetTransform.up * offset.y;
                        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.9f * Time.unscaledDeltaTime);
                        // transform.position = Vector3.Lerp(transform.position, targetTransform.position + offset, 0.9f * Time.unscaledDeltaTime);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTransform.rotation, 50f * Time.unscaledDeltaTime);
                        break;

                    case (cameraFollowType.VanityFollow):

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






    }



}






