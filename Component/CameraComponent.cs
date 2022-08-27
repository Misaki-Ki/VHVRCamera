using UnityEngine;

namespace VHVRCamera
{

    public class CameraComponent : MonoBehaviour
    {
        GameObject followerCameraObject;
        Transform targetTransform;
        Camera followCam;

        private Vector3 velocity = Vector3.zero;


        void Start()
        {
            followerCameraObject = this.gameObject;
            targetTransform = new GameObject().transform;

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
                    Debug.Log("Camera is Enabled.");
                    Debug.Log(followerCameraObject);

                    if (Player.m_localPlayer != null)
                    {
                        transform.position = Player.m_localPlayer.transform.position;
                       // transform.position = Player.m_localPlayer.m_animator.GetBoneTransform(HumanBodyBones.Head).transform.position;
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

                Debug.Log("Spec Position: " + transform.position);
                Debug.Log("Target position : " + targetTransform.position);




                Vector3 offset = new Vector3(0, 0, -2);
                targetPosition = targetTransform.position + offset;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.6f);
                transform.LookAt(targetTransform);

                //  transform.position = Vector3.Lerp(transform.position, targetTransform.position, 0.9f * Time.unscaledDeltaTime);
                // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetTransform.rotation, 50f * Time.unscaledDeltaTime);

            }

        }






    }



}






