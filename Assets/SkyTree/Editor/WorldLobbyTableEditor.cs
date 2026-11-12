using UnityEditor;
using UnityEngine;

namespace SkyTree.Editor
{
    [CustomEditor(typeof(WorldLobbyTable))]
    public sealed class WorldLobbyTableEditor : UnityEditor.Editor
    {
        private readonly bool[] expanded = { true, false, false, false, false, false, false, false, false };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultRegion"), new GUIContent("기본 지역"), true);
            EditorGUILayout.HelpBox("월드 1~9 · 지역 1~9\n지역 이름·배경·글자색을 관리합니다. 배경을 지정하지 않은 지역은 기본 지역을 공유합니다. UI와 주인공 배는 LobbyRoot에서 관리합니다.", MessageType.Info);
            var worlds = serializedObject.FindProperty("worlds");
            for (int world = 0; world < worlds.arraySize; world++)
            {
                expanded[world] = EditorGUILayout.Foldout(expanded[world], $"월드 {world + 1}", true);
                if (!expanded[world]) continue;
                EditorGUI.indentLevel++;
                var regions = worlds.GetArrayElementAtIndex(world).FindPropertyRelative("regions");
                for (int region = 0; region < regions.arraySize; region++)
                    EditorGUILayout.PropertyField(regions.GetArrayElementAtIndex(region), new GUIContent($"지역 {region + 1}"), true);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
