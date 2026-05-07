using UnityEngine;
using DG.Tweening;
public class DotweenTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.DOMove(Vector3.up, 5);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
