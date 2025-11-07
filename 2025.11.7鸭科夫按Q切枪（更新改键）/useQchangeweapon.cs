using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using System;
using System.IO;
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
        // 绑定窗口相关（按键绑定 QuickChangeKey）
        private bool showBindWindow = false;
        private Rect bindWindowRect = new Rect(60, 60, 240, 100);
        private bool waitingForBind = false;
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

    // 本地用于触发 QuickChange 的 InputAction（由模组动态创建/重绑定）
    private InputAction? quickChangeAction;
    // 配置文件名（保存在 mod DLL 所在目录）
    private string configFileName = "useQchangeweapon_config.json";

        
        void Start()
        {
            
        }
        void Update()
        {


            // 未完成初始化时，尝试初始化。
            if (!changeinputactionSuccess)
            {
                TryInitIfReady();
            }
            // 如果初始化成功，检测打开绑定窗口按键
            if (changeinputactionSuccess)
            {
                //怕和其他模组按键冲突，游戏内同时按住 Z+X 再按 C 呼出绑定窗口 （Z+X+C）
                if (Input.GetKey(KeyCode.Z) && Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.C))
                {
                    showBindWindow = !showBindWindow;
                }
            }
            // 退出等待绑定状态时不要继续绑定
            if (!showBindWindow) waitingForBind = false;
            
            // 如果没有通过 InputAction 绑定到快速切换，则保留对 QuickChangeKey 的回退检测
            if (quickChangeAction == null)
            {
                if (Input.GetKeyDown(QuickChangeKey))
                {
                    // 判断玩家手部自由
                    if (CharacterMainControl.Main.CanUseHand())
                    {
                        OnQuickSwitch();
                    }
                }
            }

            // 按键绑定操作由 IMGUI 窗口通过 Event.current 捕获（减少轮询）
        }
        
        private void OnDestroy()
        {
            // 清理 quickChangeAction
            if (quickChangeAction != null)
            {
                try
                {
                    quickChangeAction.performed -= OnQuickActionPerformed;
                    quickChangeAction.Disable();
                    quickChangeAction.Dispose();
                }
                catch { }
            }
        }

        // 仅当 CharacterInputControl.Instance 已经存在时尝试初始化
        private void TryInitIfReady()
        {
            var instance = CharacterInputControl.Instance;
            if (instance != null)
            {
                ChangeInputAction();
                LoadQuickSwitchBinding();
            }
        }
        private void LoadQuickSwitchBinding()
        {
            string saved = LoadBindingPersistent();
            if (!string.IsNullOrEmpty(saved))
            {
                CreateOrReplaceQuickAction(saved);
                // 尝试解析为 KeyCode 以用于显示
                KeyCode kc = BindingPathToKeyCode(saved);
                QuickChangeKey = kc;
                Debug.Log($"useQchangeweapon: 加载持久化绑定，QuickChangeKey = {QuickChangeKey}");
            }
        }

        // json文件路径
        private string ConfigFilePath()
        {
            try
            {
                // 直接使用 mod DLL 所在目录
                string asmLocation = typeof(ModBehaviour).Assembly.Location;
                string asmDir = Path.GetDirectoryName(asmLocation);
                Debug.Log($"useQchangeweapon: 配置文件路径 {asmDir}");
                return Path.Combine(asmDir, configFileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"useQchangeweapon: 获取配置文件路径失败: {ex.Message}");
                return configFileName;
            }
        }

    [Serializable]
    private class BindingData { public string quickBinding = string.Empty; }

        // 从配置文件加载按键绑定
        private string LoadBindingPersistent()
        {
            try
            {
                string path = ConfigFilePath();
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(txt))
                    {
                        BindingData d = JsonUtility.FromJson<BindingData>(txt);
                        if (d != null && !string.IsNullOrEmpty(d.quickBinding))
                        {
                            Debug.Log($"useQchangeweapon: 已从 {path} 加载配置");
                            return d.quickBinding;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"useQchangeweapon: 读取配置文件失败: {ex.Message}");
            }
            return string.Empty;
        }

        // 保存绑定到配置文件
        private void SaveBindingPersistent(string bindingPath)
        {
            try
            {
                string path = ConfigFilePath();
                BindingData d = new BindingData() { quickBinding = bindingPath };
                File.WriteAllText(path, JsonUtility.ToJson(d));
                Debug.Log($"useQchangeweapon: 配置已保存到 {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"useQchangeweapon: 写入配置文件失败: {ex.Message}");
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
                        

                    // 检测武器切换键位获取成功
                    if (weapon1Action != null )
                    {
                        Debug.Log("useQchangeweapon:已订阅武器切换输入动作");
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
        // 道具栏有立即使用和手持两种，需要判断道具类型。手持类似乎都是技能，有例外再说
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

        // QuickChange 的 InputAction 回调
        private void OnQuickActionPerformed(InputAction.CallbackContext ctx)
        {
            if (CharacterMainControl.Main.CanUseHand())
            {
                OnQuickSwitch();
            }
        }

        // 将 KeyCode 转换为 Input System 的 control binding path
        private string KeyCodeToBindingPath(KeyCode kc)
        {
            // 字母
            if (kc >= KeyCode.A && kc <= KeyCode.Z)
                return $"<Keyboard>/{kc.ToString().ToLower()}";
            // 数字键
            if (kc >= KeyCode.Alpha0 && kc <= KeyCode.Alpha9)
                return $"<Keyboard>/{kc.ToString().Replace("Alpha", "").ToLower()}";
            switch (kc)
            {
                case KeyCode.Space: return "<Keyboard>/space";
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return "<Keyboard>/enter";
                case KeyCode.Escape: return "<Keyboard>/escape";
                case KeyCode.Backspace: return "<Keyboard>/backspace";
                case KeyCode.Tab: return "<Keyboard>/tab";
                case KeyCode.UpArrow: return "<Keyboard>/upArrow";
                case KeyCode.DownArrow: return "<Keyboard>/downArrow";
                case KeyCode.LeftArrow: return "<Keyboard>/leftArrow";
                case KeyCode.RightArrow: return "<Keyboard>/rightArrow";
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return "<Keyboard>/shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return "<Keyboard>/ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return "<Keyboard>/alt";
                //不包含鼠标
                default:
                    // 其他按键尽量使用小写首字母风格（例如 UpArrow -> upArrow）
                    string name = kc.ToString();
                    if (string.IsNullOrEmpty(name)) return "";
                    return $"<Keyboard>/{char.ToLower(name[0]) + name.Substring(1)}";
            }
        }

        // 从绑定路径尝试解析为 KeyCode（用于显示），不保证对所有控制器都正确
        private KeyCode BindingPathToKeyCode(string path)
        {
            if (string.IsNullOrEmpty(path)) return QuickChangeKey;
            if (path.StartsWith("<Keyboard>/"))
            {
                string key = path.Substring("<Keyboard>/".Length);
                // 处理数字
                if (int.TryParse(key, out int _))
                {
                    return (KeyCode)System.Enum.Parse(typeof(KeyCode), "Alpha" + key);
                }
                switch (key)
                {
                    case "space": return KeyCode.Space;
                    case "enter": return KeyCode.Return;
                    case "escape": return KeyCode.Escape;
                    case "backspace": return KeyCode.Backspace;
                    case "tab": return KeyCode.Tab;
                    case "upArrow": return KeyCode.UpArrow;
                    case "downArrow": return KeyCode.DownArrow;
                    case "leftArrow": return KeyCode.LeftArrow;
                    case "rightArrow": return KeyCode.RightArrow;
                    case "shift": return KeyCode.LeftShift;
                    case "ctrl": return KeyCode.LeftControl;
                    case "alt": return KeyCode.LeftAlt;
                    default:
                        // 恢复为 KeyCode 枚举形式（尝试首字母大写）
                        string enumName = char.ToUpper(key[0]) + key.Substring(1);
                        try { return (KeyCode)System.Enum.Parse(typeof(KeyCode), enumName); } catch { return QuickChangeKey; }
                }
            }
            return QuickChangeKey;
        }

        // 注册Q键InputAction事件 quickChangeAction（绑定路径为 Input System control path，如 "<Keyboard>/q"）
        private void CreateOrReplaceQuickAction(string bindingPath)
        {
            // 清理旧的
            if (quickChangeAction != null)
            {
                try
                {
                    quickChangeAction.performed -= OnQuickActionPerformed;
                    quickChangeAction.Disable();
                    quickChangeAction.Dispose();
                }
                catch { }
                quickChangeAction = null;
            }

            if (string.IsNullOrEmpty(bindingPath)) return;

            quickChangeAction = new InputAction("QuickChange", InputActionType.Button);
            quickChangeAction.AddBinding(bindingPath);
            quickChangeAction.performed += OnQuickActionPerformed;
            quickChangeAction.Enable();
        }

        // IMGUI 绑定窗口（位于类末尾）这里使用ModId:3597205849，int只能保留前八位数字所以只写8位
        void OnGUI()
        {
            if (!showBindWindow) return;

            bindWindowRect = GUI.Window(35972058, bindWindowRect, DrawBindWindow, "绑定 QuickChangeKey");
        }
        // 绘制绑定窗口内容（此处由AI生成，可能导致模组运行不稳定）
        void DrawBindWindow(int id)
        {
            GUI.Label(new Rect(10, 22, 220, 20), $"当前 快捷切换按钮: {QuickChangeKey}");

            if (!waitingForBind)
            {
                if (GUI.Button(new Rect(10, 46, 100, 24), "绑定按键"))
                {
                    waitingForBind = true;
                }
            }
            else
            {
                GUI.Label(new Rect(120, 46, 110, 24), "按下任意键绑定...");
                if (GUI.Button(new Rect(10, 46, 100, 24), "取消"))
                {
                    waitingForBind = false;
                }
            }

            // 关闭窗口按钮
            if (GUI.Button(new Rect(10, 72, 220, 20), "关闭"))
            {
                waitingForBind = false;
                showBindWindow = false;
            }

            // 当处于等待绑定状态时，通过 IMGUI 事件捕获按键或鼠标按钮（避免在 Update 中轮询所有 KeyCode）
            if (waitingForBind)
            {
                Event e = Event.current;
                if (e != null)
                {
                    // 键盘按下
                    if (e.isKey && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
                    {
                        QuickChangeKey = e.keyCode;
                        Debug.Log($"useQchangeweapon: QuickChangeKey 绑定为 {e.keyCode}");
                        waitingForBind = false;
                        showBindWindow = false;
                        e.Use();
                        CreateOrReplaceQuickAction(KeyCodeToBindingPath(QuickChangeKey));
                        SaveBindingPersistent(KeyCodeToBindingPath(QuickChangeKey));
                    }
                }
            }
        }
    }
}