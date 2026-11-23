using UnityEngine;

public class CoconutTidyObject : MonoBehaviour
{
    [SerializeField] private float removeTime = 4f;

    private void Start()
    {
        Destroy(gameObject, removeTime);
    }
}
