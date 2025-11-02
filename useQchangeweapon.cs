using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

namespace useQchangeweapon
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // 脚本逻辑运算用
        private int NewWeapen_key = 0;
        private int LastWeapen_key = 0;
        private int Weaponkeytemp = 0;
        // 判断初始化是否正常
        private bool changeinputactionSuccess = false;
        // 本模组的Q键
        private KeyCode QuickChangeKey = KeyCode.Q;
        // 3种武器切换按键的输入事件
        private InputAction? weapon1Action;
        private InputAction? weapon2Action;
        private InputAction? weapon3Action;

        void Start()
        {
            // 需要进入场景后才能获取 CharacterInputControl 实例
        }
        void Update()
        {
            // 未完成初始化时，尝试初始化。已完成初始化后检测Q键
            if (!changeinputactionSuccess)
            {
                TryInitIfReady();
            }
            else if (Input.GetKeyDown(QuickChangeKey))
            {
                //判断玩家手部自由
                if(CharacterMainControl.Main.CanUseHand())
                {
                    OnQuickSwitch();
                }
            }
        }

        // 仅当 CharacterInputControl.Instance 已经存在时尝试初始化
        private void TryInitIfReady()
        {
            var instance = CharacterInputControl.Instance;
            if (instance != null)
            {
                ChangeInputAction();
            }
        }
        // 通过反射获取输入动作并订阅回调
        private void ChangeInputAction()
        {
            var inputControl = CharacterInputControl.Instance;
            if (inputControl == null) return;

            // 通过反射获取 CharacterInputControl.cs 的 inputActions
            var inputActionsField = typeof(CharacterInputControl).GetField("inputActions", BindingFlags.NonPublic | BindingFlags.Instance);

            if (inputActionsField != null)
            {
                var inputActions = inputActionsField.GetValue(inputControl);
                if (inputActions != null)
                {
                    // 获取各个 InputAction 引用
                    var type = inputActions.GetType();
                    weapon1Action = type.GetField("ItemShortcut1")?.GetValue(inputActions) as InputAction;
                    weapon2Action = type.GetField("ItemShortcut2")?.GetValue(inputActions) as InputAction;
                    // ItemShortcut_Melee 是切近战的键位
                    weapon3Action = type.GetField("ItemShortcut_Melee")?.GetValue(inputActions) as InputAction;

                    // 订阅动作回调（如果存在）类似将方法绑定给按键，不用进update轮询
                    if (weapon1Action != null)
                        weapon1Action.performed += ctx => OnWeapon1Selected();
                    if (weapon2Action != null)
                        weapon2Action.performed += ctx => OnWeapon2Selected();
                    if (weapon3Action != null)
                        weapon3Action.performed += ctx => OnWeapon3Selected();

                    // 检测三种武器切换键位获取成功
                    if (weapon1Action != null && weapon2Action != null && weapon3Action != null)
                    {
                        Debug.Log("useQchangeweapon:成功订阅武器切换输入动作");
                        changeinputactionSuccess = true;
                    }
                }
            }
        }
        // InputSystem 回调处理方法
        private void OnWeapon1Selected()
        {
            if (NewWeapen_key != 0)
            {
                LastWeapen_key = NewWeapen_key;
                NewWeapen_key = 0;
            }
        }

        private void OnWeapon2Selected()
        {
            if (NewWeapen_key != 1)
            {
                LastWeapen_key = NewWeapen_key;
                NewWeapen_key = 1;
            }
        }
        private void OnWeapon3Selected()
        {
            if (NewWeapen_key != -1)
            {
                LastWeapen_key = NewWeapen_key;
                NewWeapen_key = -1;
            }
        }
        //本模组核心功能，不停对调前后2个值
        // CharacterMainControl.Main.SwitchToWeapon(int);  0=主武器 1=副武器 -1=近战
        
        private void OnQuickSwitch()
        {
            CharacterMainControl.Main.SwitchToWeapon(LastWeapen_key);
            Weaponkeytemp = NewWeapen_key;
            NewWeapen_key = LastWeapen_key;
            LastWeapen_key = Weaponkeytemp;
        }
        
    }
}