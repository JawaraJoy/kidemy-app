#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EduGame.Editor
{
    [CustomEditor(typeof(AnimationStateBehaviour))]
    public class AnimationStateBehaviourEditor : UnityEditor.Editor
    {
        private const string c_ClipConfigProperty = "m_ClipConfig";
        private const string c_StateHashProperty = "m_StateHash";
        private const string c_ClipLengthProperty = "m_ClipLength";
        private const string c_RefreshButton = "Refresh";
        private const string c_StateNotDetected = "Animator State could not be detected.";
        private const string c_StateLabel = "State";
        private const string c_StatePathLabel = "State Path";
        private const string c_StateHashLabel = "State Hash";
        private const string c_ClipLengthLabel = "Clip Length";

        private SerializedProperty m_ClipConfig;
        private SerializedProperty m_StateHash;
        private SerializedProperty m_ClipLength;

        private AnimationStateBehaviour m_Behaviour;

        private AnimatorController m_Controller;
        private AnimatorState m_State;

        private string m_StatePath;

        private void OnEnable()
        {
            m_Behaviour = target as AnimationStateBehaviour;

            if (m_Behaviour == null)
            {
                return;
            }

            m_ClipConfig =
                serializedObject.FindProperty(c_ClipConfigProperty);

            m_StateHash =
                serializedObject.FindProperty(c_StateHashProperty);

            m_ClipLength =
                serializedObject.FindProperty(c_ClipLengthProperty);

            RefreshStateInfo();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                m_ClipConfig
            );

            EditorGUILayout.Space();

            DrawStateInfo();

            EditorGUILayout.Space();

            if (GUILayout.Button(c_RefreshButton))
            {
                RefreshStateInfo();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStateInfo()
        {
            if (m_State == null)
            {
                EditorGUILayout.HelpBox(
                    c_StateNotDetected,
                    MessageType.Warning
                );

                return;
            }

            EditorGUILayout.LabelField(
                c_StateLabel,
                m_State.name
            );

            EditorGUILayout.LabelField(
                c_StatePathLabel,
                m_StatePath
            );

            EditorGUILayout.LabelField(
                c_StateHashLabel,
                m_StateHash.intValue.ToString()
            );

            EditorGUILayout.LabelField(
                c_ClipLengthLabel,
                $"{m_ClipLength.floatValue:F3} seconds"
            );
        }

        private void RefreshStateInfo()
        {
            if (m_Behaviour == null)
            {
                return;
            }

            m_Controller = FindController();

            if (m_Controller == null)
            {
                m_State = null;
                m_StatePath = null;
                return;
            }

            m_State = null;
            m_StatePath = null;

            foreach (AnimatorControllerLayer layer
                     in m_Controller.layers)
            {
                string path = FindStatePath(
                    layer.stateMachine,
                    m_Behaviour,
                    layer.name
                );

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                m_StatePath = path;

                m_State = FindState(
                    layer.stateMachine,
                    m_Behaviour
                );

                break;
            }

            if (m_State == null)
            {
                return;
            }

            int stateHash =
                Animator.StringToHash(m_StatePath);

            float clipLength =
                GetClipLength(m_State);

            serializedObject.Update();

            m_StateHash.intValue = stateHash;
            m_ClipLength.floatValue = clipLength;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(m_Behaviour);
        }

        private AnimatorController FindController()
        {
            string assetPath =
                AssetDatabase.GetAssetPath(m_Behaviour);

            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<
                    AnimatorController>(assetPath);

            return controller;
        }

        private AnimatorState FindState(
            AnimatorStateMachine stateMachine,
            AnimationStateBehaviour behaviour)
        {
            foreach (ChildAnimatorState childState
                     in stateMachine.states)
            {
                AnimatorState state =
                    childState.state;

                foreach (StateMachineBehaviour stateBehaviour
                         in state.behaviours)
                {
                    if (stateBehaviour == behaviour)
                    {
                        return state;
                    }
                }
            }

            foreach (ChildAnimatorStateMachine childStateMachine
                     in stateMachine.stateMachines)
            {
                AnimatorState result =
                    FindState(
                        childStateMachine.stateMachine,
                        behaviour
                    );

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private string FindStatePath(
            AnimatorStateMachine stateMachine,
            AnimationStateBehaviour behaviour,
            string currentPath)
        {
            foreach (ChildAnimatorState childState
                     in stateMachine.states)
            {
                AnimatorState state =
                    childState.state;

                foreach (StateMachineBehaviour stateBehaviour
                         in state.behaviours)
                {
                    if (stateBehaviour != behaviour)
                    {
                        continue;
                    }

                    return $"{currentPath}.{state.name}";
                }
            }

            foreach (ChildAnimatorStateMachine childStateMachine
                     in stateMachine.stateMachines)
            {
                string childPath =
                    $"{currentPath}." +
                    $"{childStateMachine.stateMachine.name}";

                string result =
                    FindStatePath(
                        childStateMachine.stateMachine,
                        behaviour,
                        childPath
                    );

                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return null;
        }

        private float GetClipLength(
            AnimatorState state)
        {
            if (state.motion is AnimationClip clip)
            {
                return clip.length;
            }

            return 0f;
        }
    }
}
#endif