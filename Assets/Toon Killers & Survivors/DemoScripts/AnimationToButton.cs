using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimationToButton : MonoBehaviour
{
    Animator anim;
    // Use this for initialization
    void Start()
    {
        anim = GetComponent<Animator>();
        RuntimeAnimatorController rac = anim.runtimeAnimatorController;

        Transform panel = GameObject.Find("aPanel").transform;
        Transform prefab = panel.GetChild(0);

        bool parsed = false;
        foreach (Transform c in panel)
        {
            if (parsed)
                Destroy(c.gameObject);

            parsed = true;
        }


        foreach (AnimationClip ac in rac.animationClips)
        {
            Transform t = Instantiate(prefab, panel);
            t.GetComponentInChildren<Text>().text = ac.name;
            t.GetComponent<Button>().onClick.AddListener(() => PlayAnim(ac.name));
            t.gameObject.SetActive(true);
        }
    }

    public void PlayAnim(string s)
    {
        anim.Play(s);
    }
}
