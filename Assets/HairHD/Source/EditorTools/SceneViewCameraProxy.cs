using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SceneViewCameraProxy : MonoBehaviour
{
    #if UNITY_EDITOR
    private SceneView SceneView;
	private Camera Camera;

    public void OnEnable()
    {
		Camera = GetCamera();

		if (Camera != null)
			UpdateComponents();

		EditorApplication.update += TryUpdate;
	}

	void TryUpdate()
	{
		SceneView	= SceneView.lastActiveSceneView;
		Camera		= GetCamera();

		if (SceneView != null)
			EditorApplication.update -= TryUpdate; 
	}

	private Camera GetCamera()
    {
		SceneView = SceneView.lastActiveSceneView;

		if (SceneView == null)
			return null; 

		return SceneView.camera;
    }
 
    private Component[] GetComponents()
    {
        var result = GetComponents<Component>();
        return result;
    }
 
    private void UpdateComponents()
    {
        var components = GetComponents();
        if (components != null && components.Length > 1)
        {
            var cameraGo = Camera.gameObject;

			for (var i = 0; i < components.Length; i++)
            {
                var c = components[i];
                Type cType = c.GetType();
 
                var existing = cameraGo.GetComponent(cType) ?? cameraGo.AddComponent(cType);

				if (cType.ToString() == "HairRenderer" || cType.ToString() == "Camera")
				{
					EditorUtility.CopySerialized(c, existing);
				}
            }
        }
    }
    #endif
}
