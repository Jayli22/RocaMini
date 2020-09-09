﻿using SimpleInputNamespace;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Player : Character
{
    public Image HealthBar;
    public Image uiiSkill_1_CoolDown;
    public Image uiiSkill_2_CoolDown;
    public Image uiiSkill_3_CoolDown;
    public Image uiiSkill_4_CoolDown;
    public Text uitSkill_1_CoolDown;
    public Text uitSkill_2_CoolDown;
    public Text uitSkill_3_CoolDown;
    public Text uitSkill_4_CoolDown;
    /// <summary>
    /// 当前关卡标记
    /// </summary>
    public int levelTag; 
   
    protected Vector3 movement;
    private static Player instance;

    public GameObject[] skillPrefabs;
    public GameObject[] baseAttackPrefabs;
    public GameObject[] activeSkills;
    public GameObject[] releasedSkills;
    public float[] activeSkillsCd;
    AnimatorClipInfo[] m_CurrentClipInfo;
    //public int baseAttackIndex = 0;

    [Tooltip("鼠标位置参数是否更新")]
    public bool canChangeMouseDir;
    public Vector2 mouseDir;
    /// <summary>
    /// 鼠标相对角度
    /// </summary>
    public float mouseAngle;
    public float tmouseAngle;

    public GameObject[] attackEffect;
    //private GameObject curAttackEffect;
    //public Vector2 attackPosition;
    public GameObject guideArrow;
  
    public int clickCount; //combo次数判断标记
    public float maxComboDelayTime; //最大combo攻击间隔时间
    private float lastClickTime; //最后一次点击时间
    protected AnimationClipOverrides clipOverrides;

    private Timer skillCoolDownTimer_1;
    private Timer skillCoolDownTimer_2;
    private Timer skillCoolDownTimer_3;
    private Timer skillCoolDownTimer_4;
    private Timer castingTimer;

    private Buffer buffers;

    //public bool comboEffectFirstMark;
    //combo攻击判断标记
    public bool comboMark;//当前combo区间内是否有按下攻击键
    public bool comboEffectMark;
    public float[] baseAttackPreCastTime;


    public bool isCastingSkill; //释放技能
    public bool isBaseAttack; //基础三连击
    public bool canBaseAttack; //是否可以进行三连击
    public bool canSkill; //是否可以释放技能
    public PlayerCurrentState curStatus; //当前状态


    //攻击方向指示箭头资源素材
    public GameObject attackGuidePrefab;
    //技能文件位置
    private string internalSkillPath;

    public static Player MyInstance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Player>();
            }
            return instance;
        }
    }


    protected override void Awake()
    {
        base.Awake();
        UIBinding();
        DontDestroyOnLoad(gameObject);
    }
    //角色生成后初始化
    public override void Init()
    {
        base.Init();
        rb2d = GetComponent<Rigidbody2D>();
        levelTag = 0;
        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = animatorOverrideController;
        clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
        animatorOverrideController.GetOverrides(clipOverrides);

        comboMark = false;
        clickCount = 0;
        comboEffectMark = false;

        curStatus = PlayerCurrentState.Normal;
        StatusSwitch(curStatus);

        buffers = gameObject.AddComponent<Buffer>();
        skillCoolDownTimer_1 = gameObject.AddComponent<Timer>();
        skillCoolDownTimer_2 = gameObject.AddComponent<Timer>();
        skillCoolDownTimer_3 = gameObject.AddComponent<Timer>();
        skillCoolDownTimer_4 = gameObject.AddComponent<Timer>();


        castingTimer = gameObject.AddComponent<Timer>();
        castingTimer.Duration = 0.1f;
        castingTimer.Run();
        activeSkills = new GameObject[4];
        releasedSkills = new GameObject[4];
        activeSkillsCd = new float[4];
        isBaseAttack = false;
        BaseAttackSwitch(0);
        //SkillSwitch(0,Random.Range(0, 10));
        SkillSwitch(1, 5);
        SkillSwitch(2, 6);
        SkillSwitch(3, 7);
        SkillSwitch(0, 8);


        internalSkillPath = "Skill/InternalSkill/";

        activeSkill_id = -1;

        animator.SetFloat("IdleDirectionY",1.0f); //冲刺初始IDLE

    }
    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        Init();
       
    }
    protected override void FixedUpdate()
    {
        
    }


    /// <summary>
    /// 设置玩家朝向
    /// </summary>
    private void SetPlayerDirection()
    {
        animator.SetFloat("AttackHorizontal", mouseDir.x);
        animator.SetFloat("AttackVertical", mouseDir.y);
        mouseAngle = ToolsHub.GetAngleBetweenVectors(new Vector2(0, 1), mouseDir);
    }

    protected override void Update()
    {
        //if (stiffnessTimer.Finished && isStiffness)
        //{
        //    UndoStiffness();

        //}
        if (inhitableTimer.Finished && !hitableTag)
        {
            UnInhitable();
        }
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        BuffCalculate();
        if (isAlive)
        {
          
            GetInput();
            AttackGuide();
            
            ComboCheck();

            if (curStatus == PlayerCurrentState.Fangun && stateInfo.IsName("Fangun") && stateInfo.normalizedTime>=1.0f)
            {
                StatusSwitch(PlayerCurrentState.Normal);
            }
            if (canMove)
            {

                Move();

            }

            //if (stateInfo.IsName("Idle") || stateInfo.IsName("Movement"))
            //{
            //    SetPlayerDirection();
            //}
                       
               
            if (castingTimer.Finished && (curStatus == PlayerCurrentState.Skill || curStatus == PlayerCurrentState.MoveSkill))
            {
                StatusSwitch(PlayerCurrentState.Normal);
                //AbleToSetMousePos(true);
            }

            if(curStatus == PlayerCurrentState.Skill || curStatus == PlayerCurrentState.Normal)
            {
                DashOverTime();
            }
            // mouseDir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
            mouseDir = new Vector2(SimpleInput.GetAxis("Attackx"), SimpleInput.GetAxis("Attacky"));
            mouseDir = mouseDir.normalized;
            if (canChangeMouseDir)
            {
                SetPlayerDirection();
            }
            UIUpdate();
        }
        else
        {
            UIManager.MyInstance.deadHint.SetActive(true);

        }

    }


    /// <summary>
    /// 获取输入
    /// </summary>
    protected void GetInput()
    {
        movement = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            movement.y += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement.x -= 1;

        }
        if (Input.GetKey(KeyCode.S))
        {
            movement.y -= 1;

        }
        if (Input.GetKey(KeyCode.D))
        {
            movement.x += 1;

        }
        movement = new Vector2(SimpleInput.GetAxis("Horizontal"), SimpleInput.GetAxis("Vertical"));

        movement = movement.normalized;

       
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {

            BaseAttackSwitch(1);
            //skillInventory.Load();  //存储技能
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            BaseAttackSwitch(2);

            //skillInventory.Save();
        }
        if (canBaseAttack)
            {
            if (SimpleInput.GetButton("attack"))
            {
                lastClickTime = Time.time;
                comboMark = true;

                clickCount++;
                animator.SetFloat("IdleDirectionX", mouseDir.x);
                animator.SetFloat("IdleDirectionY", mouseDir.y);
                
                if (clickCount == 1)
                {
                    animator.SetInteger("AttackCMD", 1);
                    StatusSwitch(PlayerCurrentState.BaseAttack);
                    
                }

                clickCount = Mathf.Clamp(clickCount, 0, 3);
            }
        }
        if (canSkill)
        {
            if (Input.GetKeyDown(KeyCode.Q) || SimpleInput.GetButtonDown("button1")) //Skill1
            {
                if(activeSkills[0] == null)
                {

                }
                else if (skillCoolDownTimer_1.Finished)
                {
                    releasedSkills[0] = InstantiateEffectRotate(activeSkills[0], mouseAngle);
                    animator.SetTrigger("Skill_1");
                    skillCoolDownTimer_1.Run();
                    Debug.Log(releasedSkills[0].GetComponent<BaseMartialSkill>().skillType);
                    DetermineSkillType(releasedSkills[0]);

                }

            }
            if (Input.GetKeyDown(KeyCode.E) || SimpleInput.GetButtonDown("button2")) //Skill2
            {
                if (activeSkills[1] == null)
                {

                }
                else if (skillCoolDownTimer_2.Finished)
                {
                    releasedSkills[1] = InstantiateEffectRotate(activeSkills[1], mouseAngle);
                    Debug.Log(releasedSkills[1].GetComponent<BaseMartialSkill>().skillType);

                    skillCoolDownTimer_2.Run();
                    animator.SetTrigger("Skill_2");
                    DetermineSkillType(releasedSkills[1]);

                }
            }
            if (Input.GetKeyDown(KeyCode.R) || SimpleInput.GetButtonDown("button3")) //Skill3
            {
                if (activeSkills[2] == null)
                {

                }
                else if (skillCoolDownTimer_3.Finished)
                {
                    releasedSkills[2] = InstantiateEffectRotate(activeSkills[2], mouseAngle);
                    animator.SetTrigger("Skill_3");
                    skillCoolDownTimer_3.Run();         
                    DetermineSkillType(releasedSkills[2]);
                }
            }
            if (Input.GetKeyDown(KeyCode.T) || SimpleInput.GetButtonDown("button4")) //Skill4
            {
                if (activeSkills[3] == null)
                {

                }
                else if (skillCoolDownTimer_4.Finished)
                {
                    releasedSkills[3] = InstantiateEffectRotate(activeSkills[3],mouseAngle); 
                    skillCoolDownTimer_4.Run();
                    animator.SetTrigger("Skill_4");

                    DetermineSkillType(releasedSkills[3]);



                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StatusSwitch(PlayerCurrentState.Fangun);
        }
        if (curStatus == PlayerCurrentState.Fangun)
        {
            if (movement.magnitude < 0.1f)
            {
                movement.x = animator.GetFloat("IdleDirectionX");
                movement.y = animator.GetFloat("IdleDirectionY");
            }
        }
    }


    /// <summary>
    /// 移动
    /// </summary>
    public void Move()
    {
        if(curStatus == PlayerCurrentState.Fangun)
        {
            moveSpeed_f = (moveSpeed + moveSpeed_t) * moveSpeed_p + 1;
            
        }
        else
        {
            moveSpeed_f = (moveSpeed + moveSpeed_t) * moveSpeed_p;
        }
        gameObject.transform.position = gameObject.transform.position + movement * Time.deltaTime * moveSpeed_f;
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Magnitude", movement.magnitude);
        if(movement.magnitude > 0.1f)
        {
            animator.SetFloat("IdleDirectionX", movement.x);
            animator.SetFloat("IdleDirectionY", movement.y);
        }
    }


    /// <summary>
    /// 设置玩家Idel朝向
    /// </summary>
    void SetIdleDir()
    {
        if (stateInfo.IsName("Movement") && movement.magnitude > 0.1)
        {
            animator.SetFloat("IdleDirectionX", movement.x);
            animator.SetFloat("IdleDirectionY", movement.y);
        }
        else
        {
            animator.SetFloat("IdleDirectionX", mouseDir.x);
            animator.SetFloat("IdleDirectionY", mouseDir.y);
        }

    }

    public GameObject InstantiateEffectRotate(GameObject g, float mouseAngle )
    {

        //return Instantiate(g, SwitchAttackPosition(mouseAngle).transform.position, transform.rotation * Quaternion.Euler(0, 0, mouseAngle), transform);
        GameObject n = Instantiate(g, transform);


        if (n.GetComponent<ReleasedPrefab>().rotateTag)
        {
            n.transform.RotateAround(gameObject.transform.position, Vector3.forward, mouseAngle);
        }
        else
        {
            n.transform.RotateAround(gameObject.transform.position, Vector3.forward, mouseAngle);
            n.transform.Rotate(Vector3.forward, -mouseAngle);
        }
        return n;

    }


    public GameObject InstantiateEffectRotate(GameObject g)
    {
        //g.transform.RotateAround(transform.position, Vector3.forward, mouseAngle);
        //g.transform.Rotate(g.transform.position, Vector3.forward, mouseAngle);
        return Instantiate(g, transform);   


    }


    /// <summary>
    /// 三连击系统
    /// </summary>
    void ComboCheck()
    {
              
        if (Time.time - lastClickTime > maxComboDelayTime)
        {
            clickCount = 0;
            //comboEffectMark = false;
            comboMark = false;
            animator.SetInteger("AttackCMD", 0);
        }
        //noOfClicks = Mathf.Clamp(noOfClicks, 0, 4);
    }


    /// <summary>
    /// 获取玩家属性
    /// </summary>
    /// <returns></returns>
    public int[] GetPlayerStats()
    {
        int[] playerStats = new int[10];
        playerStats[0] = maxHp_f;
        return playerStats;
    }

    /// <summary>
    /// 动画片段替换
    /// </summary>
    public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    {
        public AnimationClipOverrides(int capacity) : base(capacity) { }

        public AnimationClip this[string name]
        {
            get { return this.Find(x => x.Key.name.Equals(name)).Value; }
            set
            {
                int index = this.FindIndex(x => x.Key.name.Equals(name));
                if (index != -1)
                    this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
            }
        }
    }


    public void BaseAttackSwitch(int index)
    {
        //switch (index)
        //{
        //    case 0:
                clipOverrides["BaseAttackUp_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[0];
                clipOverrides["BaseAttackUp_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[0];
                clipOverrides["BaseAttackUp_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[0];
                clipOverrides["BaseAttackUpRight_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[1];
                clipOverrides["BaseAttackUpRight_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[1];
                clipOverrides["BaseAttackUpRight_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[1];
                clipOverrides["BaseAttackRight_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[2];
                clipOverrides["BaseAttackRight_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[2];
                clipOverrides["BaseAttackRight_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[2];
                clipOverrides["BaseAttackDownRight_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[3];
                clipOverrides["BaseAttackDownRight_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[3];
                clipOverrides["BaseAttackDownRight_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[3];
                clipOverrides["BaseAttackDown_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[4];
                clipOverrides["BaseAttackDown_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[4];
                clipOverrides["BaseAttackDown_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[4];
                clipOverrides["BaseAttackDownLeft_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[5];
                clipOverrides["BaseAttackDownLeft_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[5];
                clipOverrides["BaseAttackDownLeft_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[5];
                clipOverrides["BaseAttackLeft_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[6];
                clipOverrides["BaseAttackLeft_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[6];
                clipOverrides["BaseAttackLeft_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[6];
                clipOverrides["BaseAttackUpLeft_1"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_1[7];
                clipOverrides["BaseAttackUpLeft_2"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_2[7];
                clipOverrides["BaseAttackUpLeft_3"] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().attack_3[7];
                animatorOverrideController.ApplyOverrides(clipOverrides);
                animator.SetFloat("BaseAttackSpeed_1", baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().animationSpeed[0]);
                animator.SetFloat("BaseAttackSpeed_2", baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().animationSpeed[1]);
                animator.SetFloat("BaseAttackSpeed_3", baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().animationSpeed[2]);

                attackEffect = new GameObject[3];
                attackEffect[0] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().effectPrefabs[0];
                attackEffect[1] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().effectPrefabs[1];
                attackEffect[2] = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().effectPrefabs[2];

                baseAttackPreCastTime = baseAttackPrefabs[index].GetComponent<BaseAttackPrefab>().preCastTime;
              //  break;
       // }
    }
    public void SkillSwitch(int barIndex, int skillId)
    {
        int index = 0 ;
        for(int i= 0; i < skillPrefabs.Length; i++)
        {
            if (skillPrefabs[i].GetComponent<BaseMartialSkill>().skillId == skillId)
            {
                index = i;
                break;
            }
        }
        for (int i = 0; i < 4; i ++)
        {
            if(activeSkills[i] !=null)
            {
                if (activeSkills[i].GetComponent<BaseMartialSkill>().skillId == skillId)
                {
                    if (i != barIndex)
                    {
                        activeSkills[i] = null;
                    }
                }
            }
           
        }
       switch (barIndex)
        {
            case 0:
                activeSkills[barIndex] = skillPrefabs[index];
                activeSkillsCd[0] = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;
                clipOverrides["Skill_1Up"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[0];
                clipOverrides["Skill_1UpRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[1];
                clipOverrides["Skill_1Right"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[2];
                clipOverrides["Skill_1DownRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[3];
                clipOverrides["Skill_1Down"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[4];
                clipOverrides["Skill_1DownLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[5];
                clipOverrides["Skill_1Left"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[6];
                clipOverrides["Skill_1UpLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[7];
                skillCoolDownTimer_1.Duration = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;
                skillCoolDownTimer_1.Run();
                skillCoolDownTimer_1.RemainTime = 0.1f;
                animator.SetFloat("SkillSpeed_1", skillPrefabs[index].GetComponent<BaseMartialSkill>().animatorSpeed);
                Debug.Log(barIndex + skillPrefabs[index].name);
                break;
            case 1:
                activeSkills[barIndex] = skillPrefabs[index];
                activeSkillsCd[1] = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;
                clipOverrides["Skill_2Up"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[0];
                clipOverrides["Skill_2UpRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[1];
                clipOverrides["Skill_2Right"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[2];
                clipOverrides["Skill_2DownRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[3];
                clipOverrides["Skill_2Down"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[4];
                clipOverrides["Skill_2DownLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[5];
                clipOverrides["Skill_2Left"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[6];
                clipOverrides["Skill_2UpLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[7];
                skillCoolDownTimer_2.Duration = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;

                skillCoolDownTimer_2.Run();
                skillCoolDownTimer_2.RemainTime = 0.1f;
                animator.SetFloat("SkillSpeed_2", skillPrefabs[index].GetComponent<BaseMartialSkill>().animatorSpeed);

                Debug.Log(barIndex + skillPrefabs[index].name);

                break;
            case 2:
                activeSkills[barIndex] = skillPrefabs[index];
                activeSkillsCd[2] = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;
                clipOverrides["Skill_3Up"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[0];
                clipOverrides["Skill_3UpRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[1];
                clipOverrides["Skill_3Right"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[2];
                clipOverrides["Skill_3DownRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[3];
                clipOverrides["Skill_3Down"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[4];
                clipOverrides["Skill_3DownLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[5];
                clipOverrides["Skill_3Left"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[6];
                clipOverrides["Skill_3UpLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[7];
                skillCoolDownTimer_3.Duration = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;

                skillCoolDownTimer_3.Run();
                skillCoolDownTimer_3.RemainTime = 0.1f;
                animator.SetFloat("SkillSpeed_3", skillPrefabs[index].GetComponent<BaseMartialSkill>().animatorSpeed);

                Debug.Log(barIndex + skillPrefabs[index].name);


                break;
            case 3:
                activeSkills[barIndex] = skillPrefabs[index];
                activeSkillsCd[3] = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;

                clipOverrides["Skill_4Up"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[0];
                clipOverrides["Skill_4UpRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[1];
                clipOverrides["Skill_4Right"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[2];
                clipOverrides["Skill_4DownRight"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[3];
                clipOverrides["Skill_4Down"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[4];
                clipOverrides["Skill_4DownLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[5];
                clipOverrides["Skill_4Left"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[6];
                clipOverrides["Skill_4UpLeft"] = skillPrefabs[index].GetComponent<BaseMartialSkill>().dirPrefabs[7];
                skillCoolDownTimer_4.Duration = skillPrefabs[index].GetComponent<BaseMartialSkill>().cooldown;
                skillCoolDownTimer_4.Run();
                skillCoolDownTimer_4.RemainTime = 0.1f;
                animator.SetFloat("SkillSpeed_4", skillPrefabs[index].GetComponent<BaseMartialSkill>().animatorSpeed);
                Debug.Log(barIndex + skillPrefabs[index].name);

                break;
        }
        animatorOverrideController.ApplyOverrides(clipOverrides);


    }


   


    public void AbleToSetMousePos(bool b)
    {
        canChangeMouseDir = b;
    }

    /// <summary>
    /// 选择攻击位置
    /// </summary>
    /// <param name="attackAngle"></param>
    /// <returns></returns>
    //public GameObject SwitchAttackPosition(float attackAngle)
    //{
    //   // Debug.Log(attackAngle);
    //    GameObject g = attackPositions[0];
    //    if (337.5 >= attackAngle || attackAngle < 22.5)
    //    {
    //        g = attackPositions[0];
    //    }
    //    if (292.5 <= attackAngle && attackAngle < 337.5)
    //    {
    //        g = attackPositions[1];
    //    }
    //    if (247.5 <= attackAngle && attackAngle < 292.5)
    //    {
    //        g = attackPositions[2];
    //    }
    //    if (202.5 <= attackAngle && attackAngle < 247.5)
    //    {
    //        g = attackPositions[3];
    //    }
    //    if (157.5 <= attackAngle && attackAngle < 202.5)
    //    {
    //        g = attackPositions[4];
    //    }
    //    if (112.5 <= attackAngle && attackAngle < 157.5)
    //    {
    //        g = attackPositions[5];
    //    }
    //    if (67.5 <= attackAngle && attackAngle < 112.5)
    //    {
    //        g = attackPositions[6];
    //    }
    //    if (22.5 <= attackAngle && attackAngle < 67.5)
    //    {
    //        g = attackPositions[7];
    //    }
    //    return g;
    //}


    public void TakeDamage(int damage)
    {
        if (hitableTag)
        {
            //health reduce 
            currentHp -= damage;
            Instantiate(hittedEffectPrefab, transform.position, transform.rotation);
            if (currentHp <= 0)
            {
                isAlive = false;
            }
            else
            {
                //DoStiffness();//造成硬直
                Inhitable();
            }
        }
    }

    public void TakeDamage(Vector3 pos, float backFactor, int damage)
    {
        if (hitableTag)
        {
            //health reduce 
            currentHp -= damage;
            Instantiate(hittedEffectPrefab, transform.position, transform.rotation);

            if (currentHp <= 0)
            {
                isAlive = false;
            }
            else
            {
                KnockBack(pos - transform.position, backFactor);
                Inhitable();
            }

        }
    }
    public override void DoStiffness(bool shock = false)
    {
        actionCastTri = false;
        animator.SetBool("Hitted", true);
        stiffnessTimer.Run();
        // animator.speed = 0;
        canMove = false;
        //isStiffness = true;
    }

    public override void Inhitable()
    {
        hitableTag = false;
        inhitableTimer.Run();
    }
    public override void UnInhitable()
    {
        hitableTag = true;
    }

    /// <summary>
    /// 解除硬直
    /// </summary>
    public override void UndoStiffness()
    {
        //stiffnessTime.Run();
        //animator.speed = 1;
        canMove = true;
        //isStiffness = false;
        animator.SetBool("Hitted", false);
    }
    private void UIUpdate()
    {
        UIBinding();
        
        HealthBar.fillAmount = (float)currentHp / (float)maxHp_f;

        uiiSkill_1_CoolDown.fillAmount = 1 - (activeSkillsCd[0] - skillCoolDownTimer_1.RemainTime) / activeSkillsCd[0];
        uiiSkill_2_CoolDown.fillAmount = 1 - (activeSkillsCd[1] - skillCoolDownTimer_2.RemainTime) / activeSkillsCd[1];
        uiiSkill_3_CoolDown.fillAmount = 1 - (activeSkillsCd[2] - skillCoolDownTimer_3.RemainTime) / activeSkillsCd[2];
        uiiSkill_4_CoolDown.fillAmount = 1 - (activeSkillsCd[3] - skillCoolDownTimer_4.RemainTime) / activeSkillsCd[3];
        uitSkill_1_CoolDown.text = ((int)skillCoolDownTimer_1.RemainTime).ToString();
        uitSkill_2_CoolDown.text = ((int)skillCoolDownTimer_2.RemainTime).ToString();
        uitSkill_3_CoolDown.text = ((int)skillCoolDownTimer_3.RemainTime).ToString();
        uitSkill_4_CoolDown.text = ((int)skillCoolDownTimer_4.RemainTime).ToString();
    }

    public void DetermineSkillType(GameObject tSkill)
    {
        BaseMartialSkill skillScript = tSkill.GetComponent<BaseMartialSkill>();
        
        switch (skillScript.skillType)
        {
            case MartialSkillType.FIXCASTING:
                castingTimer.Duration = skillScript.castingDuration;
                castingTimer.Run();
                animator.SetBool("IsCasting", true);
                skillScript.Release();
                StatusSwitch(PlayerCurrentState.Skill);
                break;
            case MartialSkillType.LINEDRIVECASTING:
                castingTimer.Duration = skillScript.castingDuration;
                castingTimer.Run();
                animator.SetBool("IsCasting", true);
                skillScript.Release();
                StatusSwitch(PlayerCurrentState.Skill);
                break;
            case MartialSkillType.MOVECASTING:
                castingTimer.Duration = skillScript.castingDuration;
                castingTimer.Run();
                animator.SetBool("IsCasting", true);
                skillScript.Release();
                buffers.Active(skillScript.castingDuration);

                StatusSwitch(PlayerCurrentState.MoveSkill);

                break;
            case MartialSkillType.NORMALCASTING:
                castingTimer.Duration = skillScript.castingDuration;
                castingTimer.Run();
                animator.SetBool("IsCasting", true);
                skillScript.Release();
                StatusSwitch(PlayerCurrentState.Skill);
                break;
            case MartialSkillType.TRAJECTORY:
                castingTimer.Duration = skillScript.castingDuration;
                castingTimer.Run();
                animator.SetBool("IsCasting", true);
                skillScript.Release();
                StatusSwitch(PlayerCurrentState.Skill);
                tSkill.GetComponent<BaseMartialSkill>().direction = mouseAngle;
                break;
        }
    }
    public void UIBinding()
    {
        HealthBar = GameObject.Find("HealthBar").GetComponent<Image>() ;
        uiiSkill_1_CoolDown = GameObject.Find("Skill_1_CoolDownImage").GetComponent<Image>();
        uiiSkill_2_CoolDown = GameObject.Find("Skill_2_CoolDownImage").GetComponent<Image>();
        uiiSkill_3_CoolDown = GameObject.Find("Skill_3_CoolDownImage").GetComponent<Image>();
        uiiSkill_4_CoolDown = GameObject.Find("Skill_4_CoolDownImage").GetComponent<Image>();
        uitSkill_1_CoolDown = GameObject.Find("Skill_1_CoolDownText").GetComponent<Text>();
        uitSkill_2_CoolDown = GameObject.Find("Skill_2_CoolDownText").GetComponent<Text>();
        uitSkill_3_CoolDown = GameObject.Find("Skill_3_CoolDownText").GetComponent<Text>();
        uitSkill_4_CoolDown = GameObject.Find("Skill_4_CoolDownText").GetComponent<Text>();
    }



    public void StatusSwitch(PlayerCurrentState currentState)
    {
        switch (currentState)
        {
            case PlayerCurrentState.Normal:
                canMove = true;
                isCastingSkill = false;
                isBaseAttack = false;
                curStatus = PlayerCurrentState.Normal;
                canChangeMouseDir = true;
                canBaseAttack = true;
                //animator.SetBool("Move", false);
                animator.SetBool("IsCasting", false);
                canSkill = true;
                onFangun = false;
                hitableTag = true;
                animator.SetBool("Fangun", false);
                //Debug.Log("Normals");

                break;
            case PlayerCurrentState.Move:
                isCastingSkill = false;
                canMove = true;
                curStatus = PlayerCurrentState.Move;
                canChangeMouseDir = true;
                canBaseAttack = true;
                //animator.SetBool("Move", true);
                canSkill = true;
                onFangun = false;
                //Debug.Log("Moves");

                break;
            case PlayerCurrentState.MoveSkill:
                isCastingSkill = true;
                canMove = true;
                curStatus = PlayerCurrentState.MoveSkill;
                canChangeMouseDir = true;
                canBaseAttack = false;
                animator.SetBool("IsCasting", true);
                canSkill = false;
                onFangun = false;

                break;

            case PlayerCurrentState.Skill:
                isCastingSkill = true;
                canMove = false;
                curStatus = PlayerCurrentState.Skill;
                canChangeMouseDir = false;
                canBaseAttack = false;
                animator.SetBool("IsCasting", true);
                canSkill = false;
                onFangun = false;

                break;
            
            case PlayerCurrentState.Hitteed:
                canMove = false;
                curStatus = PlayerCurrentState.Hitteed;
                canChangeMouseDir = false;
                canBaseAttack = false;
                canSkill = false;
                onFangun = false;

                break;

            case PlayerCurrentState.BaseAttack:
                canMove = false;
                isBaseAttack = true;
                curStatus = PlayerCurrentState.BaseAttack;
                canSkill = false;
                canChangeMouseDir = false;
                onFangun = false;
                //Debug.Log("Baseattacks");

                break;
            case PlayerCurrentState.Fangun:
                animator.SetBool("IsCasting", false);
                animator.SetInteger("AttackCMD", 0);
                animator.SetBool("Fangun", true);
                canMove = true;
                onFangun = true;
                isBaseAttack = false;
                canBaseAttack = false;
                curStatus = PlayerCurrentState.Fangun;
                canSkill = false;
                canChangeMouseDir = true;
                isCastingSkill = false;
                clickCount = 0;
                //Debug.Log("Fangun");

                comboMark = false;
                hitableTag = false;
                break;
            default:
                break;
        }
    }
    

    /// <summary>
    /// 攻击方向箭头指引
    /// </summary>
    private void AttackGuide()
    {
        //float angle = ToolsHub.GetAngleBetweenVectors(new Vector2(0, 1), ((Vector3)playerCharacterPos - transform.position).normalized);
        //aimLine.transform.RotateAround(gameObject.transform.position, Vector3.forward, angle - tangle);
        //
        attackGuidePrefab.transform.Rotate(Vector3.forward, mouseAngle - tmouseAngle);
        tmouseAngle = mouseAngle;
    }

 

    private void OnTriggerEnter2D(Collider2D collision)
    {
    
    }
    private void OnApplicationQuit()
    {
        
    }

    public int ComboStatus()
    {
        return animator.GetInteger("AttackCMD");
    }

}
