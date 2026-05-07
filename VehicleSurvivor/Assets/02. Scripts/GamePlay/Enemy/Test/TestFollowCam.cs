using UnityEngine;

public class TestFollowCam : MonoBehaviour
{

    public GameObject playerOb;
    
    // Update is called once per frame
    void Update()
    {
        transform.position = playerOb.transform.position + new Vector3(0, 80, -40);
    }
}
