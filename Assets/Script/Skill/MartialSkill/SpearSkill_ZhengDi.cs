﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpearSkill_ZhengDi : BaseMartialSkill
{
    // Start is called before the first frame update
    protected override void Awake()
    {
        base.Awake();
        castingTimer = gameObject.AddComponent<Timer>();
        transform.parent = null;

    }
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        castingTimer.Duration = castingDuration;
        castingTimer.Run();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if(castingTimer.Finished)
        {
            TryDestory();
        }
    }

    public override void Release()
    {
        base.Release();
        HitCheck(true);
    }


}
