using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DustlineArena.Editor
{
    public static class PlayerDodgeInstaller
    {
        private const string PlayerControllerPath = "Assets/_Project/Animation/Controllers/AC_Player.controller";
        private const string DodgeClipPath = "Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Movement Advanced/Dodge_Forward.anim";
        private const string DodgeParameter = "Dodge";
        private const string DodgeStateName = "Dodge Forward";

        [MenuItem("Dustline Arena/Install Player Dodge Roll")]
        public static void Install()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            AnimationClip dodgeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DodgeClipPath);
            if (controller == null || dodgeClip == null || controller.layers.Length == 0)
            {
                Debug.LogError("Dustline Arena: cannot install player dodge roll. Missing player controller or dodge clip.");
                return;
            }

            EnsureTrigger(controller, DodgeParameter);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state != null && state.name == "Idle");
            AnimatorState dodge = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state != null && state.name == DodgeStateName);

            if (dodge == null)
            {
                dodge = machine.AddState(DodgeStateName, new Vector3(520f, -240f, 0f));
            }

            dodge.motion = dodgeClip;
            dodge.speed = 1.12f;
            dodge.writeDefaultValues = true;

            EnsureAnyStateDodgeTransition(machine, dodge);
            EnsureDodgeExitTransition(dodge, idle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: player dodge roll animation installed.");
        }

        public static void InstallFromCommandLine()
        {
            Install();
        }

        private static void EnsureTrigger(AnimatorController controller, string parameterName)
        {
            if (controller.parameters.Any(parameter => parameter.name == parameterName))
            {
                return;
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
        }

        private static void EnsureAnyStateDodgeTransition(AnimatorStateMachine machine, AnimatorState dodge)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState == dodge
                    && transition.conditions.Any(condition => condition.parameter == DodgeParameter))
                {
                    return;
                }
            }

            AnimatorStateTransition dodgeTransition = machine.AddAnyStateTransition(dodge);
            dodgeTransition.hasExitTime = false;
            dodgeTransition.duration = 0.03f;
            dodgeTransition.canTransitionToSelf = false;
            dodgeTransition.AddCondition(AnimatorConditionMode.If, 0f, DodgeParameter);
        }

        private static void EnsureDodgeExitTransition(AnimatorState dodge, AnimatorState idle)
        {
            if (idle == null)
            {
                return;
            }

            foreach (AnimatorStateTransition transition in dodge.transitions)
            {
                dodge.RemoveTransition(transition);
            }

            AnimatorStateTransition exitTransition = dodge.AddTransition(idle);
            exitTransition.hasExitTime = true;
            exitTransition.exitTime = 0.82f;
            exitTransition.duration = 0.08f;
            exitTransition.hasFixedDuration = true;
        }
    }
}
