using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ExampleNullable
{
	public int nullableInt;
	public string nullableString;
}

public class Example : MonoBehaviour
{
	public GameObject testing;

    // Start is called before the first frame update
    void Start()
    {
        testing.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
