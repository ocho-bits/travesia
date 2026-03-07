using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupPlayerAnimator2D
{
    private const string AnimationsRoot = "Assets/Animations";
    private const string PlayerAnimationsRoot = "Assets/Animations/Player";
    private const string ControllerPath = "Assets/Animations/Player/Player.controller";

    [MenuItem("Tools/Setup Player Animator 2D")]
    public static void Setup()
    {
        GameObject playerRoot = FindPlayerRoot();
        if (playerRoot == null)
        {
            Debug.LogError("[SetupPlayerAnimator2D] Could not find Player. Tag a GameObject as 'Player' or select one with PlayerMotor2D.");
            return;
        }

        PlayerMotor2D motor = playerRoot.GetComponent<PlayerMotor2D>();
        if (motor == null)
        {
            Debug.LogError("[SetupPlayerAnimator2D] Selected Player root does not have PlayerMotor2D.", playerRoot);
            return;
        }

        PlayerInput2D input = playerRoot.GetComponent<PlayerInput2D>();
        GroundCheck2D groundCheck = playerRoot.GetComponentInChildren<GroundCheck2D>(true);
        Rigidbody2D rb = playerRoot.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("[SetupPlayerAnimator2D] Player root is missing Rigidbody2D.", playerRoot);
            return;
        }

        Transform graphics = ResolveGraphicsTransform(playerRoot.transform);
        if (graphics == null)
        {
            Debug.LogError("[SetupPlayerAnimator2D] Failed to resolve/create Graphics transform.", playerRoot);
            return;
        }

        SpriteRenderer spriteRenderer = graphics.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            SpriteRenderer existing = playerRoot.GetComponentInChildren<SpriteRenderer>(true);
            if (existing != null)
            {
                spriteRenderer = existing;
                Debug.LogWarning("[SetupPlayerAnimator2D] Graphics child has no SpriteRenderer. Using existing SpriteRenderer found elsewhere.", existing);
            }
            else
            {
                spriteRenderer = graphics.gameObject.AddComponent<SpriteRenderer>();
                Debug.LogWarning("[SetupPlayerAnimator2D] No SpriteRenderer found. Added a new SpriteRenderer to Graphics.", graphics);
            }
        }

        Animator animator = graphics.GetComponent<Animator>();
        if (animator == null)
        {
            animator = graphics.gameObject.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;

        EnsureFolder(AnimationsRoot);
        EnsureFolder(PlayerAnimationsRoot);

        AnimationClip runClip = FindRunClip();
        if (runClip == null)
        {
            Debug.LogError("[SetupPlayerAnimator2D] Could not find clip 'She-Walk' (or She_Walk). Animator setup aborted.");
            return;
        }

        AnimatorController controller = BuildController(runClip);
        animator.runtimeAnimatorController = controller;

        PlayerAnimationDriver2D driver = playerRoot.GetComponent<PlayerAnimationDriver2D>();
        if (driver == null)
        {
            driver = playerRoot.AddComponent<PlayerAnimationDriver2D>();
        }

        WireDriver(driver, animator, spriteRenderer, graphics, motor, input, groundCheck, rb);

        EditorUtility.SetDirty(playerRoot);
        EditorUtility.SetDirty(graphics.gameObject);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(driver);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SetupPlayerAnimator2D] Completed. Controller: {ControllerPath}", playerRoot);
    }

    private static GameObject FindPlayerRoot()
    {
        GameObject byTag = null;

        try
        {
            byTag = GameObject.FindGameObjectWithTag("Player");
        }
        catch
        {
            // Tag may not exist.
        }

        if (byTag != null)
        {
            PlayerMotor2D taggedMotor = byTag.GetComponent<PlayerMotor2D>();
            if (taggedMotor != null)
            {
                return taggedMotor.gameObject;
            }

            taggedMotor = byTag.GetComponentInParent<PlayerMotor2D>();
            if (taggedMotor != null)
            {
                return taggedMotor.gameObject;
            }
        }

        if (Selection.activeGameObject != null)
        {
            PlayerMotor2D selectedMotor = Selection.activeGameObject.GetComponent<PlayerMotor2D>();
            if (selectedMotor != null)
            {
                return selectedMotor.gameObject;
            }

            selectedMotor = Selection.activeGameObject.GetComponentInParent<PlayerMotor2D>();
            if (selectedMotor != null)
            {
                return selectedMotor.gameObject;
            }
        }

        return null;
    }

    private static Transform ResolveGraphicsTransform(Transform playerRoot)
    {
        Transform graphics = playerRoot.Find("Graphics");
        if (graphics != null)
        {
            return graphics;
        }

        SpriteRenderer childSprite = null;
        foreach (SpriteRenderer sr in playerRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.transform != playerRoot)
            {
                childSprite = sr;
                break;
            }
        }

        if (childSprite != null)
        {
            return childSprite.transform;
        }

        GameObject graphicsGo = new GameObject("Graphics");
        graphicsGo.transform.SetParent(playerRoot);
        graphicsGo.transform.localPosition = Vector3.zero;
        graphicsGo.transform.localRotation = Quaternion.identity;
        graphicsGo.transform.localScale = Vector3.one;
        return graphicsGo.transform;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
        string name = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static AnimationClip FindRunClip()
    {
        List<string> guids = new List<string>(AssetDatabase.FindAssets("t:AnimationClip She_Walk"));
        guids.AddRange(AssetDatabase.FindAssets("t:AnimationClip She-Walk"));

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                continue;
            }

            if (string.Equals(clip.name, "She-Walk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(clip.name, "She_Walk", StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        // Fallback by known project path.
        return AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Player/She_Walk.anim");
    }

    private static AnimatorController BuildController(AnimationClip runClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        RebuildParameters(controller);
        RebuildBaseLayer(controller, runClip);
        RebuildOverlayLayer(controller, runClip);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return controller;
    }

    private static void RebuildParameters(AnimatorController controller)
    {
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(controller.parameters[i]);
        }

        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Overlay", AnimatorControllerParameterType.Int);
    }

    private static void RebuildBaseLayer(AnimatorController controller, AnimationClip runClip)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        AnimatorControllerLayer baseLayer = layers.Length > 0 ? layers[0] : new AnimatorControllerLayer();

        baseLayer.name = "Base Layer";
        baseLayer.defaultWeight = 1f;
        baseLayer.blendingMode = AnimatorLayerBlendingMode.Override;

        AnimatorStateMachine sm = baseLayer.stateMachine;
        if (sm == null)
        {
            sm = new AnimatorStateMachine { name = "Base Layer" };
            AssetDatabase.AddObjectToAsset(sm, controller);
        }

        ClearStateMachine(sm);

        AnimatorState run = sm.AddState("Run", new Vector3(250f, 120f, 0f));
        run.motion = runClip;

        AnimatorState jump = sm.AddState("Jump", new Vector3(550f, 120f, 0f));
        jump.motion = runClip; // Placeholder until a real jump clip exists.

        sm.defaultState = run;

        AnimatorStateTransition runToJumpByGrounded = run.AddTransition(jump);
        ConfigureImmediateTransition(runToJumpByGrounded);
        runToJumpByGrounded.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

        AnimatorStateTransition runToJumpByTrigger = run.AddTransition(jump);
        ConfigureImmediateTransition(runToJumpByTrigger);
        runToJumpByTrigger.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

        AnimatorStateTransition jumpToRun = jump.AddTransition(run);
        ConfigureImmediateTransition(jumpToRun);
        jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

        baseLayer.stateMachine = sm;

        if (layers.Length == 0)
        {
            controller.layers = new[] { baseLayer };
        }
        else
        {
            layers[0] = baseLayer;
            controller.layers = layers;
        }
    }

    private static void RebuildOverlayLayer(AnimatorController controller, AnimationClip runClip)
    {
        AnimatorStateMachine overlaySm = new AnimatorStateMachine { name = "Overlay" };
        AssetDatabase.AddObjectToAsset(overlaySm, controller);

        AnimatorState empty = overlaySm.AddState("Empty", new Vector3(250f, 120f, 0f));
        AnimatorState reaction = overlaySm.AddState("ReactionPlaceholder", new Vector3(550f, 120f, 0f));
        reaction.motion = runClip; // Placeholder clip for contextual reactions.
        overlaySm.defaultState = empty;

        AnimatorStateTransition emptyToReaction = empty.AddTransition(reaction);
        ConfigureImmediateTransition(emptyToReaction);
        emptyToReaction.AddCondition(AnimatorConditionMode.Equals, 1f, "Overlay");

        AnimatorStateTransition reactionToEmpty = reaction.AddTransition(empty);
        ConfigureImmediateTransition(reactionToEmpty);
        reactionToEmpty.AddCondition(AnimatorConditionMode.Equals, 0f, "Overlay");

        AnimatorControllerLayer overlayLayer = new AnimatorControllerLayer
        {
            name = "Overlay",
            defaultWeight = 1f,
            blendingMode = AnimatorLayerBlendingMode.Override,
            stateMachine = overlaySm
        };

        AnimatorControllerLayer[] existing = controller.layers;
        if (existing.Length == 0)
        {
            controller.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", defaultWeight = 1f, stateMachine = new AnimatorStateMachine() }, overlayLayer };
            return;
        }

        if (existing.Length == 1)
        {
            controller.layers = new[] { existing[0], overlayLayer };
            return;
        }

        existing[1] = overlayLayer;
        controller.layers = existing;
    }

    private static void ClearStateMachine(AnimatorStateMachine sm)
    {
        for (int i = sm.states.Length - 1; i >= 0; i--)
        {
            sm.RemoveState(sm.states[i].state);
        }

        for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveAnyStateTransition(sm.anyStateTransitions[i]);
        }

        for (int i = sm.entryTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveEntryTransition(sm.entryTransitions[i]);
        }
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.offset = 0f;
    }

    private static void WireDriver(
        PlayerAnimationDriver2D driver,
        Animator animator,
        SpriteRenderer spriteRenderer,
        Transform graphics,
        PlayerMotor2D motor,
        PlayerInput2D input,
        GroundCheck2D groundCheck,
        Rigidbody2D rb)
    {
        SerializedObject so = new SerializedObject(driver);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        so.FindProperty("graphicsTransform").objectReferenceValue = graphics;
        so.FindProperty("motor").objectReferenceValue = motor;
        so.FindProperty("input").objectReferenceValue = input;
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
