using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using ItemStatsSystem;
using Duckov;
using Unity.VisualScripting;

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
        // 其他物品快捷键输入事件备用
        private InputAction? itemshortcut3Action;
        private InputAction? itemshortcut4Action;
        private InputAction? itemshortcut5Action;
        private InputAction? itemshortcut6Action;
        private InputAction? itemshortcut7Action;
        private InputAction? itemshortcut8Action;

        
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
                    itemshortcut3Action = type.GetField("ItemShortcut3")?.GetValue(inputActions) as InputAction;
                    itemshortcut4Action = type.GetField("ItemShortcut4")?.GetValue(inputActions) as InputAction;
                    itemshortcut5Action = type.GetField("ItemShortcut5")?.GetValue(inputActions) as InputAction;
                    itemshortcut6Action = type.GetField("ItemShortcut6")?.GetValue(inputActions) as InputAction;
                    itemshortcut7Action = type.GetField("ItemShortcut7")?.GetValue(inputActions) as InputAction;
                    itemshortcut8Action = type.GetField("ItemShortcut8")?.GetValue(inputActions) as InputAction;

                    // 订阅动作回调（如果存在）类似将方法绑定给按键，不用进update轮询
                    if (weapon1Action != null)
                        weapon1Action.performed += ctx => OnWeapon1Selected();
                    if (weapon2Action != null)
                        weapon2Action.performed += ctx => OnWeapon2Selected();
                    if (weapon3Action != null)
                        weapon3Action.performed += ctx => OnWeapon3Selected();
                    if (itemshortcut3Action != null)
                        itemshortcut3Action.performed += ctx => OnShortCutInput3Performed();
                    if (itemshortcut4Action != null)
                        itemshortcut4Action.performed += ctx => OnShortCutInput4Performed();
                    if (itemshortcut5Action != null)
                        itemshortcut5Action.performed += ctx => OnShortCutInput5Performed();
                    if (itemshortcut6Action != null)
                        itemshortcut6Action.performed += ctx => OnShortCutInput6Performed();
                    if (itemshortcut7Action != null)
                        itemshortcut7Action.performed += ctx => OnShortCutInput7Performed();
                    if (itemshortcut8Action != null)
                        itemshortcut8Action.performed += ctx => OnShortCutInput8Performed();
                        

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
            if (LastWeapen_key == 0 || LastWeapen_key == 1 || LastWeapen_key == -1)//-1、0、1 武器切换
            {
                CharacterMainControl.Main.SwitchToWeapon(LastWeapen_key);
            }
            if (LastWeapen_key >= 3 && LastWeapen_key <= 8) //3-8快捷栏物品切换
            {
                Item item = ItemShortcut.Get(LastWeapen_key - 3);
                if (item != null && (item.GetBool("IsSkill") || item.HasHandHeldAgent))
                {
                    CharacterMainControl.Main.ChangeHoldItem(item);
                }
                else return;
            }
            Weaponkeytemp = NewWeapen_key;
            NewWeapen_key = LastWeapen_key;
            LastWeapen_key = Weaponkeytemp;
        }
        // 模仿 CharacterInputControl.cs 的 ShotrCutInput 处理其他物品快捷键
        private void OnItemshortcutSelected(int itemIndex)
        {
            Item item = ItemShortcut.Get(itemIndex - 3);
            if (item != null && (item.GetBool("IsSkill") || item.HasHandHeldAgent))
            {
                if(NewWeapen_key != itemIndex)
                {
                    LastWeapen_key = NewWeapen_key;
                    NewWeapen_key = itemIndex;//3-8
                }
            }
            else return;
        }
        private void OnShortCutInput3Performed()
        {
            OnItemshortcutSelected(3);
        }
        private void OnShortCutInput4Performed()
        {
            OnItemshortcutSelected(4);
        }
        private void OnShortCutInput5Performed()
        {
            OnItemshortcutSelected(5);
        }
        private void OnShortCutInput6Performed()
        {
            OnItemshortcutSelected(6);
        }
        private void OnShortCutInput7Performed()
        {
            OnItemshortcutSelected(7);
        }
        private void OnShortCutInput8Performed()
        {
            OnItemshortcutSelected(8);
        }
        
    }
}