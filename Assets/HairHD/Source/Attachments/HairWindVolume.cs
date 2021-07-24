using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HairWindVolume : MonoBehaviour
{
    public enum VOLUME_TYPE
    {
        Global,
        Local
    }

    public enum VOLUME_DISTRIBUTION
    {
        Fade,
        Full
    }

    public VOLUME_TYPE          VolumeType              = VOLUME_TYPE.Global;
    public VOLUME_DISTRIBUTION  VolumeDistribution      = VOLUME_DISTRIBUTION.Fade;

}
