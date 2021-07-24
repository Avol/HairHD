using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float rn;

    void Start()
    {
        //Matrix4x4 PV = GL.GetGPUProjectionMatrix(GetComponent<Camera>().projectionMatrix, false) * GetComponent<Camera>().worldToCameraMatrix;
        //Debug.Log(PV.m22);

        Matrix4x4[]  s = Shader.GetGlobalMatrixArray("_AdditionalLightsWorldToShadow");

      //  Debug.Log(s[0]);

      /*  float far   = 5.0f;
        float near  = 0.1f;
        float x = (1.0f - far / near);
        float y = far / near;

        // Debug.Log(x + " : " + -(1.0f / s[0].m21));
        //Debug.Log(y + " : " + 1.0f / s[0].m21);


        float x2 = -(1.0f / s[0].m21);
        float t = -x2 * (1.0f - 1.0f / x2);


        Debug.Log(x2 + " " + t);*/

        /*float LinearDepth(float z)
        {
            float near = 0.1f;
            float far = 5.0f;

            float x = (1.0 - far / near);
            float y = far / near;
            return 1.0 / (x * z + y);
        }*/


       // 0.20408163
       // 4.9
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalFloat("_Test2", rn);
    }   
}
