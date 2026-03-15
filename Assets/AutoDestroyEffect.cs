
using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    public float destroyDelay = 3f; // Adjust based on your effect duration
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
