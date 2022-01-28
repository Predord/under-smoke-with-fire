//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.1.0
//     from Assets/Scripts/Entity Input Controls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @EntityInputControls : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @EntityInputControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Entity Input Controls"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""374d0b81-10ff-425b-9d67-cbaf84a61245"",
            ""actions"": [
                {
                    ""name"": ""AlternativeAction"",
                    ""type"": ""Button"",
                    ""id"": ""9a3630a1-a117-439a-b551-6cd1520ac96a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""Move"",
                    ""type"": ""Button"",
                    ""id"": ""a54df137-68cb-459b-a4c3-d36b19d7b52b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""TouchCell"",
                    ""type"": ""Button"",
                    ""id"": ""4b2a53c0-654b-474a-a7ce-6b15348e8a85"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""QuickSlot"",
                    ""type"": ""Button"",
                    ""id"": ""fe40d0dc-2b6d-4b6b-b8fe-8d8275d7c39a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""ChangeAbilityCurveMiddlePoint"",
                    ""type"": ""Value"",
                    ""id"": ""6da95d52-ada0-47e5-9531-9d9f55a56e70"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""StopCurrentAction"",
                    ""type"": ""Button"",
                    ""id"": ""d773d347-4ab6-4790-94e6-1e126edce85c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c61f3333-11de-4b95-926d-0332da4c65cf"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AlternativeAction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""deafacf3-e564-4a28-b1bc-582daa7aea5a"",
                    ""path"": ""<Keyboard>/enter"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f109edd3-b552-43ab-b3dc-0eef1dd83d79"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""TouchCell"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""97d67637-3738-4922-a232-b2ac56ee3ea6"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""446c1eb3-208d-43e9-8d71-d62d8730fc7a"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=2)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""429a9380-4768-49cf-9e38-b11abb36fa40"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=3)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a42e5dab-da7c-4139-a16b-4f54f4e879ff"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=4)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ae4fdc89-134b-4efd-a914-20e87bf5c4cf"",
                    ""path"": ""<Keyboard>/5"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=5)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""be3731a2-5ec7-478a-8284-c571d250724f"",
                    ""path"": ""<Keyboard>/6"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=6)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b2b3bca1-fa1e-48ac-8ff7-25c4ffcfdd4b"",
                    ""path"": ""<Keyboard>/7"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=7)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cc9995ce-7296-400f-8208-2f86b2edac88"",
                    ""path"": ""<Keyboard>/8"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=8)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""501f1889-7de8-4631-a567-ab05e1b638d3"",
                    ""path"": ""<Keyboard>/9"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=9)"",
                    ""groups"": """",
                    ""action"": ""QuickSlot"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""53b49d75-38d6-482d-80f3-0467a9457ba1"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ChangeAbilityCurveMiddlePoint"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a6022163-96c7-47a8-8e2b-9a09a6fc77ed"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""StopCurrentAction"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Camera"",
            ""id"": ""f01c5efb-919e-4136-94d6-b90f8a738a61"",
            ""actions"": [
                {
                    ""name"": ""Camera Movement"",
                    ""type"": ""PassThrough"",
                    ""id"": ""86c9610c-9883-4723-a190-1de801034bc5"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Zoom"",
                    ""type"": ""Value"",
                    ""id"": ""203fa7c4-4c75-477f-9c8d-22923740d0d0"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Camera Rotate"",
                    ""type"": ""Button"",
                    ""id"": ""3f901370-aee2-40fd-92e7-12e1723068c2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""TopDownView"",
                    ""type"": ""Button"",
                    ""id"": ""0151b6f6-a6be-466c-b2d9-d71973ddeded"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""9d54cb4d-0d20-4604-bc44-fe60d76a13f4"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""8290548f-3347-4d78-a7cd-ece915433d42"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""96b1c750-34c0-40d1-a00f-76bf037607a5"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""31829cf5-049d-4499-a8f6-b1f1d257f189"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""2f8e8c46-faf4-4812-83ef-cd31507f035f"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""9ed711a6-06c8-4870-a36f-f03304a9ffdf"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Zoom"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4ea8e0b2-b025-42a2-8509-bc70bfae5898"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": ""Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Camera Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f727021b-132b-4021-b164-19ff4f8065de"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""TopDownView"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""UI"",
            ""id"": ""cf79ec91-ac0d-48fb-83b5-c0896e02f706"",
            ""actions"": [
                {
                    ""name"": ""OpenStatsPanel"",
                    ""type"": ""Button"",
                    ""id"": ""d5f2ae36-0a96-438c-ad03-fa83ad539c36"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""OpenActionList"",
                    ""type"": ""Button"",
                    ""id"": ""d885ffd0-66de-467d-aab9-e52c11a8b0cd"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""MenuCallCancel"",
                    ""type"": ""Button"",
                    ""id"": ""d43f2ab7-e7f4-4156-a176-7bda1a4c8667"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""4db20c4e-8a22-4373-ae75-6618b867d1a8"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MenuCallCancel"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cf78cf8e-ef23-4ae7-99fe-3540d54c91e4"",
                    ""path"": ""<Keyboard>/i"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""OpenActionList"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6a0abac1-8516-4845-a917-bc254ddc8967"",
                    ""path"": ""<Keyboard>/k"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""OpenStatsPanel"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_AlternativeAction = m_Player.FindAction("AlternativeAction", throwIfNotFound: true);
        m_Player_Move = m_Player.FindAction("Move", throwIfNotFound: true);
        m_Player_TouchCell = m_Player.FindAction("TouchCell", throwIfNotFound: true);
        m_Player_QuickSlot = m_Player.FindAction("QuickSlot", throwIfNotFound: true);
        m_Player_ChangeAbilityCurveMiddlePoint = m_Player.FindAction("ChangeAbilityCurveMiddlePoint", throwIfNotFound: true);
        m_Player_StopCurrentAction = m_Player.FindAction("StopCurrentAction", throwIfNotFound: true);
        // Camera
        m_Camera = asset.FindActionMap("Camera", throwIfNotFound: true);
        m_Camera_CameraMovement = m_Camera.FindAction("Camera Movement", throwIfNotFound: true);
        m_Camera_CameraZoom = m_Camera.FindAction("Camera Zoom", throwIfNotFound: true);
        m_Camera_CameraRotate = m_Camera.FindAction("Camera Rotate", throwIfNotFound: true);
        m_Camera_TopDownView = m_Camera.FindAction("TopDownView", throwIfNotFound: true);
        // UI
        m_UI = asset.FindActionMap("UI", throwIfNotFound: true);
        m_UI_OpenStatsPanel = m_UI.FindAction("OpenStatsPanel", throwIfNotFound: true);
        m_UI_OpenActionList = m_UI.FindAction("OpenActionList", throwIfNotFound: true);
        m_UI_MenuCallCancel = m_UI.FindAction("MenuCallCancel", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Player
    private readonly InputActionMap m_Player;
    private IPlayerActions m_PlayerActionsCallbackInterface;
    private readonly InputAction m_Player_AlternativeAction;
    private readonly InputAction m_Player_Move;
    private readonly InputAction m_Player_TouchCell;
    private readonly InputAction m_Player_QuickSlot;
    private readonly InputAction m_Player_ChangeAbilityCurveMiddlePoint;
    private readonly InputAction m_Player_StopCurrentAction;
    public struct PlayerActions
    {
        private @EntityInputControls m_Wrapper;
        public PlayerActions(@EntityInputControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @AlternativeAction => m_Wrapper.m_Player_AlternativeAction;
        public InputAction @Move => m_Wrapper.m_Player_Move;
        public InputAction @TouchCell => m_Wrapper.m_Player_TouchCell;
        public InputAction @QuickSlot => m_Wrapper.m_Player_QuickSlot;
        public InputAction @ChangeAbilityCurveMiddlePoint => m_Wrapper.m_Player_ChangeAbilityCurveMiddlePoint;
        public InputAction @StopCurrentAction => m_Wrapper.m_Player_StopCurrentAction;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
            {
                @AlternativeAction.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAlternativeAction;
                @AlternativeAction.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAlternativeAction;
                @AlternativeAction.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAlternativeAction;
                @Move.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMove;
                @TouchCell.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnTouchCell;
                @TouchCell.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnTouchCell;
                @TouchCell.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnTouchCell;
                @QuickSlot.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnQuickSlot;
                @QuickSlot.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnQuickSlot;
                @QuickSlot.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnQuickSlot;
                @ChangeAbilityCurveMiddlePoint.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnChangeAbilityCurveMiddlePoint;
                @ChangeAbilityCurveMiddlePoint.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnChangeAbilityCurveMiddlePoint;
                @ChangeAbilityCurveMiddlePoint.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnChangeAbilityCurveMiddlePoint;
                @StopCurrentAction.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnStopCurrentAction;
                @StopCurrentAction.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnStopCurrentAction;
                @StopCurrentAction.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnStopCurrentAction;
            }
            m_Wrapper.m_PlayerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @AlternativeAction.started += instance.OnAlternativeAction;
                @AlternativeAction.performed += instance.OnAlternativeAction;
                @AlternativeAction.canceled += instance.OnAlternativeAction;
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @TouchCell.started += instance.OnTouchCell;
                @TouchCell.performed += instance.OnTouchCell;
                @TouchCell.canceled += instance.OnTouchCell;
                @QuickSlot.started += instance.OnQuickSlot;
                @QuickSlot.performed += instance.OnQuickSlot;
                @QuickSlot.canceled += instance.OnQuickSlot;
                @ChangeAbilityCurveMiddlePoint.started += instance.OnChangeAbilityCurveMiddlePoint;
                @ChangeAbilityCurveMiddlePoint.performed += instance.OnChangeAbilityCurveMiddlePoint;
                @ChangeAbilityCurveMiddlePoint.canceled += instance.OnChangeAbilityCurveMiddlePoint;
                @StopCurrentAction.started += instance.OnStopCurrentAction;
                @StopCurrentAction.performed += instance.OnStopCurrentAction;
                @StopCurrentAction.canceled += instance.OnStopCurrentAction;
            }
        }
    }
    public PlayerActions @Player => new PlayerActions(this);

    // Camera
    private readonly InputActionMap m_Camera;
    private ICameraActions m_CameraActionsCallbackInterface;
    private readonly InputAction m_Camera_CameraMovement;
    private readonly InputAction m_Camera_CameraZoom;
    private readonly InputAction m_Camera_CameraRotate;
    private readonly InputAction m_Camera_TopDownView;
    public struct CameraActions
    {
        private @EntityInputControls m_Wrapper;
        public CameraActions(@EntityInputControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @CameraMovement => m_Wrapper.m_Camera_CameraMovement;
        public InputAction @CameraZoom => m_Wrapper.m_Camera_CameraZoom;
        public InputAction @CameraRotate => m_Wrapper.m_Camera_CameraRotate;
        public InputAction @TopDownView => m_Wrapper.m_Camera_TopDownView;
        public InputActionMap Get() { return m_Wrapper.m_Camera; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CameraActions set) { return set.Get(); }
        public void SetCallbacks(ICameraActions instance)
        {
            if (m_Wrapper.m_CameraActionsCallbackInterface != null)
            {
                @CameraMovement.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraMovement;
                @CameraMovement.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraMovement;
                @CameraMovement.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraMovement;
                @CameraZoom.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraZoom;
                @CameraZoom.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraZoom;
                @CameraZoom.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraZoom;
                @CameraRotate.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraRotate;
                @CameraRotate.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraRotate;
                @CameraRotate.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnCameraRotate;
                @TopDownView.started -= m_Wrapper.m_CameraActionsCallbackInterface.OnTopDownView;
                @TopDownView.performed -= m_Wrapper.m_CameraActionsCallbackInterface.OnTopDownView;
                @TopDownView.canceled -= m_Wrapper.m_CameraActionsCallbackInterface.OnTopDownView;
            }
            m_Wrapper.m_CameraActionsCallbackInterface = instance;
            if (instance != null)
            {
                @CameraMovement.started += instance.OnCameraMovement;
                @CameraMovement.performed += instance.OnCameraMovement;
                @CameraMovement.canceled += instance.OnCameraMovement;
                @CameraZoom.started += instance.OnCameraZoom;
                @CameraZoom.performed += instance.OnCameraZoom;
                @CameraZoom.canceled += instance.OnCameraZoom;
                @CameraRotate.started += instance.OnCameraRotate;
                @CameraRotate.performed += instance.OnCameraRotate;
                @CameraRotate.canceled += instance.OnCameraRotate;
                @TopDownView.started += instance.OnTopDownView;
                @TopDownView.performed += instance.OnTopDownView;
                @TopDownView.canceled += instance.OnTopDownView;
            }
        }
    }
    public CameraActions @Camera => new CameraActions(this);

    // UI
    private readonly InputActionMap m_UI;
    private IUIActions m_UIActionsCallbackInterface;
    private readonly InputAction m_UI_OpenStatsPanel;
    private readonly InputAction m_UI_OpenActionList;
    private readonly InputAction m_UI_MenuCallCancel;
    public struct UIActions
    {
        private @EntityInputControls m_Wrapper;
        public UIActions(@EntityInputControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @OpenStatsPanel => m_Wrapper.m_UI_OpenStatsPanel;
        public InputAction @OpenActionList => m_Wrapper.m_UI_OpenActionList;
        public InputAction @MenuCallCancel => m_Wrapper.m_UI_MenuCallCancel;
        public InputActionMap Get() { return m_Wrapper.m_UI; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(UIActions set) { return set.Get(); }
        public void SetCallbacks(IUIActions instance)
        {
            if (m_Wrapper.m_UIActionsCallbackInterface != null)
            {
                @OpenStatsPanel.started -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenStatsPanel;
                @OpenStatsPanel.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenStatsPanel;
                @OpenStatsPanel.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenStatsPanel;
                @OpenActionList.started -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenActionList;
                @OpenActionList.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenActionList;
                @OpenActionList.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnOpenActionList;
                @MenuCallCancel.started -= m_Wrapper.m_UIActionsCallbackInterface.OnMenuCallCancel;
                @MenuCallCancel.performed -= m_Wrapper.m_UIActionsCallbackInterface.OnMenuCallCancel;
                @MenuCallCancel.canceled -= m_Wrapper.m_UIActionsCallbackInterface.OnMenuCallCancel;
            }
            m_Wrapper.m_UIActionsCallbackInterface = instance;
            if (instance != null)
            {
                @OpenStatsPanel.started += instance.OnOpenStatsPanel;
                @OpenStatsPanel.performed += instance.OnOpenStatsPanel;
                @OpenStatsPanel.canceled += instance.OnOpenStatsPanel;
                @OpenActionList.started += instance.OnOpenActionList;
                @OpenActionList.performed += instance.OnOpenActionList;
                @OpenActionList.canceled += instance.OnOpenActionList;
                @MenuCallCancel.started += instance.OnMenuCallCancel;
                @MenuCallCancel.performed += instance.OnMenuCallCancel;
                @MenuCallCancel.canceled += instance.OnMenuCallCancel;
            }
        }
    }
    public UIActions @UI => new UIActions(this);
    public interface IPlayerActions
    {
        void OnAlternativeAction(InputAction.CallbackContext context);
        void OnMove(InputAction.CallbackContext context);
        void OnTouchCell(InputAction.CallbackContext context);
        void OnQuickSlot(InputAction.CallbackContext context);
        void OnChangeAbilityCurveMiddlePoint(InputAction.CallbackContext context);
        void OnStopCurrentAction(InputAction.CallbackContext context);
    }
    public interface ICameraActions
    {
        void OnCameraMovement(InputAction.CallbackContext context);
        void OnCameraZoom(InputAction.CallbackContext context);
        void OnCameraRotate(InputAction.CallbackContext context);
        void OnTopDownView(InputAction.CallbackContext context);
    }
    public interface IUIActions
    {
        void OnOpenStatsPanel(InputAction.CallbackContext context);
        void OnOpenActionList(InputAction.CallbackContext context);
        void OnMenuCallCancel(InputAction.CallbackContext context);
    }
}
