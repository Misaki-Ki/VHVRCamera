using BepInEx;
using UnityEngine;
using System;

namespace VHVRCamera
{

    [BepInPlugin("org.bepinex.plugins.VHVRCamera", "Valheim VR Camera", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Debug.Log("Camera Mod is Awake");


            // This section is borrowed from: <https://github.com/octoberU/SuperHotVRSpectatorCamera/blob/main/SpectatorCameraMod.cs>

            var CamObject = new GameObject("FollowCamera", new Type[]
            {
                typeof(CameraComponent),
                typeof(Camera)
            }
            );
            CamObject.GetComponent<CameraComponent>().enabled = true;

            CamObject.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            GameObject.DontDestroyOnLoad(CamObject);


        }

       






    }



    }





