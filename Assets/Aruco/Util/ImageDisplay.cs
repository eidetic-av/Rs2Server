using UnityEngine;
using Eidetic.Rs2;

public class ImageDisplay : MonoBehaviour
{
    public MeshRenderer displayMesh;
    public ArucoGenerator generator;

    // Use this for initialization
    void Start ()
    {
        //Expects that something called runner.init() during Awake, so that the provider has been initialised by now
        displayMesh.material.mainTexture = generator.Image;
    }
}
