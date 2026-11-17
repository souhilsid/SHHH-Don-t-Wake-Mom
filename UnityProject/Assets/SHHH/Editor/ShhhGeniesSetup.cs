using SHHH.Automation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SHHH.EditorTools
{
    /// <summary>
    /// `genies_setup` (V8 Gate B): builds Resources/Genies/SHHH_GeniesAnimator.controller from the Genies SDK's own humanoid clips
    /// (DefaultIdle / Walk / Run / wave, Packages/com.genies.avatar-sdk.client/.../CommonAnimations) with SHHH's parameters:
    /// Speed (m/s) blend idle→walk→run, Carry, and Cheer/Wave/Hehe triggers. IK Pass on, so tracked/bot hand IK can reach props.
    /// </summary>
    public static class ShhhGeniesSetup
    {
        const string Clips = "Packages/com.genies.avatar-sdk.client/Internal/Avatars/Runtime/Assets/CommonAnimations/";
        const string Out = "Assets/SHHH/Resources/Genies/SHHH_GeniesAnimator.controller";

        [ShhhCommand("genies_setup")]
        public static string Setup(string args)
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "DefaultIdle.anim");
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "Walk_N.anim") ?? AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "Walk.anim");
            var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "Run_N.anim") ?? AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "Run.anim");
            var wave = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + "actions-waveGeneric.anim");
            if (idle == null || walk == null) return $"missing Genies clips (idle {idle != null}, walk {walk != null}, run {run != null}, wave {wave != null})";
            System.IO.Directory.CreateDirectory("Assets/SHHH/Resources/Genies");
            AssetDatabase.DeleteAsset(Out);
            var c = AnimatorController.CreateAnimatorControllerAtPath(Out);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("Carry", AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Cheer", "Wave", "Hehe" }) c.AddParameter(t, AnimatorControllerParameterType.Trigger);
            var layers = c.layers; layers[0].iKPass = true; c.layers = layers;
            var sm = c.layers[0].stateMachine;
            var loco = c.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f); tree.AddChild(walk, 1.1f); if (run != null) tree.AddChild(run, 3.2f);
            sm.defaultState = loco;
            if (wave != null)
            {
                var w = sm.AddState("Wave"); w.motion = wave;
                foreach (var t in new[] { "Cheer", "Wave", "Hehe" })
                {
                    var tr = sm.AddAnyStateTransition(w); tr.AddCondition(AnimatorConditionMode.If, 0, t); tr.duration = 0.15f; tr.canTransitionToSelf = false;
                }
                var back = w.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.25f;
            }
            AssetDatabase.SaveAssets();
            return $"controller ok: idle={idle.name} walk={walk.name} run={(run ? run.name : "-")} wave={(wave ? wave.name : "-")} -> {Out}";
        }
    }
}
