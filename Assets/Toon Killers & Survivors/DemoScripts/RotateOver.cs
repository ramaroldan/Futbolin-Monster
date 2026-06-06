using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOver : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    public float speed = 1f;
	// Update is called once per frame
	void Update () {
        transform.Rotate(new Vector3(0, speed, 0));
	}
}
