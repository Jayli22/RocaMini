﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAttackPrefab : MonoBehaviour
{
    public AnimationClip[] attack_1;
    public AnimationClip[] attack_2;
    public AnimationClip[] attack_3;
    public GameObject[] effectPrefabs;
    [SerializeField]
    public float[] preCastTime;
    [SerializeField]
    public float[] animationSpeed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
