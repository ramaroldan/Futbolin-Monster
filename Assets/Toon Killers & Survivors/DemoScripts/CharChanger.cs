using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharChanger : MonoBehaviour
{
    public List<Transform> chars = new List<Transform>();
    public Text infoText;
    int currentChar = -1;
    // Use this for initialization
    void Start()
    {
        Next();
    }

    public void Next()
    {
        currentChar++;
        if (currentChar >= chars.Count)
            currentChar = 0;
        Create();
    }

    public void Back()
    {
        currentChar--;
        if (currentChar < 0)
            currentChar = chars.Count -1;
        Create();
    }

    Transform current;
    void Create()
    {
        if (current != null)
            Destroy(current.gameObject);

        current = Instantiate(chars[currentChar], null);

        infoText.text = (currentChar + 1) + "/" + chars.Count; 
    }
}
