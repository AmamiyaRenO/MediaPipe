using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

#if UNITY_EDITOR
namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// 姿势跟随系统快速设置工具
    /// </summary>
    public class PoseFollowSetup : EditorWindow
    {
        private bool createDemoCharacter = true;
        private GameObject targetCharacter;
        private bool addUIControls = true;
        private Vector3 characterStartPosition = Vector3.zero;
        private Vector3 characterScale = Vector3.one;
        private UnityEngine.Color characterColor = UnityEngine.Color.cyan;
        
        [MenuItem("Tools/MediaPipe/姿势跟随系统设置")]
        public static void ShowWindow()
        {
            GetWindow<PoseFollowSetup>("姿势跟随设置");
        }
        
        void OnGUI()
        {
            GUILayout.Label("姿势跟随系统设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "这个工具将帮助您快速在当前场景中设置姿势跟随系统。\n" +
                "确保场景中已经有PoseLandmarkerRunner组件正常运行。", 
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 角色设置选项
            GUILayout.Label("角色设置", EditorStyles.boldLabel);
            createDemoCharacter = EditorGUILayout.Toggle("创建演示角色", createDemoCharacter);
            
            if (createDemoCharacter)
            {
                EditorGUI.indentLevel++;
                characterStartPosition = EditorGUILayout.Vector3Field("起始位置", characterStartPosition);
                characterScale = EditorGUILayout.Vector3Field("缩放", characterScale);
                characterColor = EditorGUILayout.ColorField("颜色", characterColor);
                EditorGUI.indentLevel--;
            }
            else
            {
                targetCharacter = EditorGUILayout.ObjectField("目标角色", targetCharacter, typeof(GameObject), true) as GameObject;
            }
            
            EditorGUILayout.Space();
            
            // UI控制选项
            GUILayout.Label("UI设置", EditorStyles.boldLabel);
            addUIControls = EditorGUILayout.Toggle("添加UI控制面板", addUIControls);
            
            EditorGUILayout.Space();
            
            // 设置按钮
            GUI.backgroundColor = UnityEngine.Color.green;
            if (GUILayout.Button("设置姿势跟随系统", GUILayout.Height(40)))
            {
                SetupPoseFollowSystem();
            }
            GUI.backgroundColor = UnityEngine.Color.white;
            
            EditorGUILayout.Space();
            
            // 帮助信息
            if (GUILayout.Button("查看使用说明"))
            {
                Application.OpenURL("https://github.com/homuler/MediaPipeUnityPlugin");
            }
        }
        
        void SetupPoseFollowSystem()
        {
            // 1. 检查是否有PoseLandmarkerRunner
            PoseLandmarkerRunner runner = FindObjectOfType<PoseLandmarkerRunner>();
            if (runner == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中未找到PoseLandmarkerRunner组件！\n请确保MediaPipe姿势检测系统已正确设置。", "确定");
                return;
            }
            
            GameObject character = null;
            
            // 2. 创建或设置角色
            if (createDemoCharacter)
            {
                character = CreateDemoCharacter();
            }
            else if (targetCharacter != null)
            {
                character = targetCharacter;
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请选择目标角色或启用'创建演示角色'选项。", "确定");
                return;
            }
            
            // 3. 添加CharacterFollower组件
            CharacterFollower follower = character.GetComponent<CharacterFollower>();
            if (follower == null)
            {
                follower = character.AddComponent<CharacterFollower>();
            }
            
            // 4. 创建演示控制器
            GameObject demoController = new GameObject("PoseFollowDemoController");
            PoseFollowDemo demo = demoController.AddComponent<PoseFollowDemo>();
            
            // 设置演示控制器的引用
            SerializedObject demoObj = new SerializedObject(demo);
            demoObj.FindProperty("demoCharacter").objectReferenceValue = character;
            demoObj.FindProperty("createDemoCharacter").boolValue = false;
            demoObj.ApplyModifiedProperties();
            
            // 5. 创建UI控制面板（如果需要）
            if (addUIControls)
            {
                CreateUIControlPanel(demo);
            }
            
            // 6. 选中创建的对象
            Selection.activeGameObject = character;
            
            EditorUtility.DisplayDialog("成功", "姿势跟随系统设置完成！\n\n请运行场景并在摄像头前移动来测试效果。", "确定");
        }
        
        GameObject CreateDemoCharacter()
        {
            GameObject character = GameObject.CreatePrimitive(PrimitiveType.Cube);
            character.name = "PoseFollowCharacter";
            character.transform.position = characterStartPosition;
            character.transform.localScale = characterScale;
            
            // 设置材质
            Renderer renderer = character.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = characterColor;
                material.SetFloat("_Metallic", 0.2f);
                material.SetFloat("_Smoothness", 0.8f);
                renderer.material = material;
            }
            
            return character;
        }
        
        void CreateUIControlPanel(PoseFollowDemo demo)
        {
            // 查找或创建Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                // 创建EventSystem（如果没有）
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
            
            // 创建控制面板
            GameObject panel = new GameObject("PoseFollowControlPanel");
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.35f, 0.98f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new UnityEngine.Color(0, 0, 0, 0.7f);
            
            // 创建标题文本
            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(panel.transform, false);
            
            RectTransform titleRect = titleText.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
            
            Text titleTextComp = titleText.AddComponent<Text>();
            titleTextComp.text = "姿势跟随控制";
            titleTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleTextComp.fontSize = 16;
            titleTextComp.color = UnityEngine.Color.white;
            titleTextComp.alignment = TextAnchor.MiddleCenter;
            
            // 创建信息显示文本
            GameObject infoText = new GameObject("InfoText");
            infoText.transform.SetParent(panel.transform, false);
            
            RectTransform infoRect = infoText.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0.1f);
            infoRect.anchorMax = new Vector2(1, 0.85f);
            infoRect.offsetMin = new Vector2(10, 0);
            infoRect.offsetMax = new Vector2(-10, 0);
            
            Text infoTextComp = infoText.AddComponent<Text>();
            infoTextComp.text = "等待姿势检测...";
            infoTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            infoTextComp.fontSize = 12;
            infoTextComp.color = UnityEngine.Color.white;
            infoTextComp.alignment = TextAnchor.UpperLeft;
            
            // 创建重置按钮
            GameObject resetButton = new GameObject("ResetButton");
            resetButton.transform.SetParent(panel.transform, false);
            
            RectTransform buttonRect = resetButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.02f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.08f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Image buttonImage = resetButton.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new UnityEngine.Color(0.2f, 0.6f, 1f, 0.8f);
            
            Button buttonComp = resetButton.AddComponent<Button>();
            
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(resetButton.transform, false);
            
            RectTransform buttonTextRect = buttonText.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            Text buttonTextComp = buttonText.AddComponent<Text>();
            buttonTextComp.text = "重置位置";
            buttonTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonTextComp.fontSize = 14;
            buttonTextComp.color = UnityEngine.Color.white;
            buttonTextComp.alignment = TextAnchor.MiddleCenter;
            
            // 连接UI到演示脚本
            SerializedObject demoObj = new SerializedObject(demo);
            demoObj.FindProperty("resetButton").objectReferenceValue = buttonComp;
            demoObj.FindProperty("infoText").objectReferenceValue = infoTextComp;
            demoObj.ApplyModifiedProperties();
        }
    }
}
#endif 