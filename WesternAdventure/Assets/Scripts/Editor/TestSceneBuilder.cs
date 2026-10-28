using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WesternAdventure.NPC;
using WesternAdventure.UI;

namespace WesternAdventure.EditorTools
{
    /// <summary>
    /// LLM/Ollama NPC diyaloğunu Play Mode'da hızlıca deneyebilmek için minimal bir
    /// test sahnesi üretir. Gerçek oyun UI'ı/sahnesi tasarım ekibi tarafından
    /// yapılana kadar geliştirme sırasında kullanılır; menüden tekrar çalıştırılabilir.
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/DialogueTest.unity";
        private const string PersonaPath = "Assets/Data/NpcPersonas/DoorGuardPersona.asset";

        [MenuItem("WesternAdventure/Diyalog Test Sahnesi Oluştur")]
        public static void BuildDialogueTestScene()
        {
            NpcPersona persona = GetOrCreatePersona();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("TestDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            Text replyText = CreateLabel(canvasGO.transform, "ReplyText", new Vector2(0, 150), new Vector2(700, 120), "NPC burada konuşacak.");
            Text thinkingText = CreateLabel(canvasGO.transform, "ThinkingIndicator", new Vector2(0, 70), new Vector2(300, 40), "Düşünüyor...");
            thinkingText.gameObject.SetActive(false);
            Text errorText = CreateLabel(canvasGO.transform, "ErrorText", new Vector2(0, 30), new Vector2(700, 40), string.Empty);
            errorText.color = Color.red;

            var resources = new DefaultControls.Resources();

            GameObject inputGO = DefaultControls.CreateInputField(resources);
            inputGO.transform.SetParent(canvasGO.transform, false);
            inputGO.name = "PlayerInputField";
            SetRect(inputGO.GetComponent<RectTransform>(), new Vector2(-110, -150), new Vector2(420, 40));

            GameObject buttonGO = DefaultControls.CreateButton(resources);
            buttonGO.transform.SetParent(canvasGO.transform, false);
            buttonGO.name = "SendButton";
            SetRect(buttonGO.GetComponent<RectTransform>(), new Vector2(260, -150), new Vector2(140, 40));
            buttonGO.GetComponentInChildren<Text>().text = "Gönder";

            var npcGO = new GameObject("TestNpc", typeof(NpcDialogueController), typeof(DialogueUI), typeof(DialogueTestHarness));
            var dialogueController = npcGO.GetComponent<NpcDialogueController>();

            SetPrivateField(dialogueController, "persona", persona);

            var dialogueUI = npcGO.GetComponent<DialogueUI>();
            SetPrivateField(dialogueUI, "dialogueController", dialogueController);
            SetPrivateField(dialogueUI, "replyText", replyText);
            SetPrivateField(dialogueUI, "thinkingIndicator", thinkingText.gameObject);
            SetPrivateField(dialogueUI, "errorText", errorText);

            var harness = npcGO.GetComponent<DialogueTestHarness>();
            SetPrivateField(harness, "dialogueController", dialogueController);
            SetPrivateField(harness, "playerInput", inputGO.GetComponent<InputField>());
            SetPrivateField(harness, "sendButton", buttonGO.GetComponent<Button>());

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"Diyalog test sahnesi oluşturuldu: {ScenePath}. Play'e basıp NPC ile konuşabilirsin.");
        }

        private static NpcPersona GetOrCreatePersona()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NpcPersona>(PersonaPath);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/Data/NpcPersonas");
            var persona = ScriptableObject.CreateInstance<NpcPersona>();
            AssetDatabase.CreateAsset(persona, PersonaPath);
            AssetDatabase.SaveAssets();
            return persona;
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string text)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetRect(go.GetComponent<RectTransform>(), anchoredPos, size);
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
