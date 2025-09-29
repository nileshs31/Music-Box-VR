using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class Bar : MonoBehaviour
{
    AudioSource aud;
    // Start is called before the first frame update
    void Start()
    {
        aud = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("boop"))
        {
            aud.Play();
        }
    }
}
