// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using Lean.Touch;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing user inputs.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(InputManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class InputManager : ManagerBase
    {
        [BeginFoldout("WebGL")]
        [SerializeField, Tooltip("This property determines whether keyboard inputs are captured by WebGL. If this is enabled (default), all inputs will be received by the WebGL canvas regardless of focus, and other elements in the webpage will not receive keyboard inputs. You need to disable this property if you need inputs to be received by other html input elements."), EndFoldout]
        private bool _captureAllKeyboardInput;

        private bool _isClick;

        private EventSystem _eventSystem;

        private LeanTouch _leanTouch;

        private MeshRendererVisual _mouseOver;
        private bool _isMouseDown;
        private Tween _mouseClickTween;

        /// <summary>
        /// Dispatched every update. This only works when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseMoveEvent;
        /// <summary>
        /// Dispatched once when a mouse enters a <see cref="DepictionEngine.MeshRendererVisual"/>. This only works  when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseEnterEvent;
        /// <summary>
        /// Dispatched once when a mouse exits a <see cref="DepictionEngine.MeshRendererVisual"/>. This only works  when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseExitEvent;
        /// <summary>
        /// Dispatched once when the left mouse button is pressed or at least one touch input is detected. This only works when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseDownEvent;
        /// <summary>
        /// Dispatched once when the left mouse button is released or no touch inputs are detected. This only works when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseUpEvent;
        /// <summary>
        /// Dispatched once if an <see cref="DepictionEngine.InputManager.OnMouseUpEvent"/> is detected within less then 0.15 second after a <see cref="DepictionEngine.InputManager.OnMouseDownEvent"/> has been dispatched. This only works when in play mode and a main <see cref="DepictionEngine.Camera"/> is present in the scene. 
        /// </summary>
        public static Action<RaycastHitDouble> OnMouseClickedEvent;

        private static InputManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InputManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<InputManager>(createIfMissing);
            return _instance;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            UpdateCaptureAllKeyboardInput();

            if (_eventSystem == null)
            {
                if (!gameObject.TryGetComponent(out _eventSystem))
                    _eventSystem = AddComponent<EventSystem>(initializingContext);
            }

            if (_leanTouch == null)
            {
                if (!gameObject.TryGetComponent(out _leanTouch))
                {
                    _leanTouch = AddComponent<LeanTouch>(initializingContext);
                    _leanTouch.UseHover = false;
                }
            }
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => captureAllKeyboardInput = value, true, initializingContext);
        }

        private void MouseMove(RaycastHitDouble hit)
        {
            SetMouseOver(hit);

            if (Input.GetMouseButton(0) || Input.touchCount > 0)
                SetMouseDown(hit);
            else
                SetMouseUp(hit);

            if (mouseClickTween == Disposable.NULL)
                _isClick = false;

            OnMouseMoveEvent?.Invoke(hit);
            if (hit != null && hit.meshRendererVisual != Disposable.NULL)
                hit.meshRendererVisual.OnMouseMoveHit(hit);
        }

        private void MouseEnter(MeshRendererVisual meshRendererVisual, RaycastHitDouble hit)
        {
            OnMouseEnterEvent?.Invoke(hit);
            if (meshRendererVisual != Disposable.NULL)
                meshRendererVisual.OnMouseEnterHit(hit);
        }

        private void MouseExit(MeshRendererVisual meshRendererVisual, RaycastHitDouble hit)
        {
            OnMouseExitEvent?.Invoke(hit);
            if (meshRendererVisual != Disposable.NULL)
                meshRendererVisual.OnMouseExitHit(hit);
        }

        private void MouseDown(MeshRendererVisual meshRendererVisual, RaycastHitDouble hit)
        {
            OnMouseDownEvent?.Invoke(hit);
            if (meshRendererVisual != Disposable.NULL)
                meshRendererVisual.OnMouseDownHit(hit);
        }

        private void MouseUp(MeshRendererVisual meshRendererVisual, RaycastHitDouble hit)
        {
            OnMouseUpEvent?.Invoke(hit);
            if (meshRendererVisual != Disposable.NULL)
                meshRendererVisual.OnMouseUpHit(hit);
        }

        private void MouseClicked(MeshRendererVisual meshRendererVisual, RaycastHitDouble hit)
        {
            OnMouseClickedEvent?.Invoke(hit);
            if (meshRendererVisual != Disposable.NULL)
                meshRendererVisual.OnMouseClickedHit(hit);
        }

        /// <summary>
        /// This property determines whether keyboard inputs are captured by WebGL. If this is enabled (default), all inputs will be received by the WebGL canvas regardless of focus, and other elements in the webpage will not receive keyboard inputs. You need to disable this property if you need inputs to be received by other html input elements.
        /// </summary>
        [Json]
        public bool captureAllKeyboardInput
        {
            get { return _captureAllKeyboardInput; }
            set 
            { 
                SetValue(nameof(captureAllKeyboardInput), value, ref _captureAllKeyboardInput, (newValue, oldValue) => 
                {
                    UpdateCaptureAllKeyboardInput();
                }); 
            }
        }

        private void UpdateCaptureAllKeyboardInput()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            WebGLInput.captureAllKeyboardInput = captureAllKeyboardInput;
#endif
        }

        public MeshRendererVisual mouseOver
        {
            get { return _mouseOver; }
        }

        private bool SetMouseOver(RaycastHitDouble value)
        {
            MeshRendererVisual meshRendererVisual = value?.meshRendererVisual;
            
            if (Object.ReferenceEquals(_mouseOver, meshRendererVisual))
                return false;

            if (_mouseOver != Disposable.NULL)
                MouseExit(_mouseOver, value);

            _mouseOver = meshRendererVisual;

            if (_mouseOver != Disposable.NULL)
                MouseEnter(_mouseOver, value);

            mouseClickTween = null;

            return true;
        }

        private void SetMouseDown(RaycastHitDouble value)
        {
            if (_isMouseDown)
                return;
            
            _isMouseDown = true;

            MeshRendererVisual meshRendererVisual = value?.meshRendererVisual;

            MouseDown(meshRendererVisual, value);

            mouseClickTween = tweenManager.DelayedCall(0.15f, null, null, () => { mouseClickTween = null; });
            _isClick = true;
        }

        private void SetMouseUp(RaycastHitDouble value)
        {
            if (!_isMouseDown)
                return;
            
            _isMouseDown = false;

            MeshRendererVisual meshRendererVisual = value?.meshRendererVisual;

            MouseUp(meshRendererVisual, value);

            if (_isClick)
            {
                mouseClickTween = null;

                MouseClicked(meshRendererVisual, value);
            }
        }

        private Tween mouseClickTween
        {
            get { return _mouseClickTween; }
            set
            {
                if (Object.ReferenceEquals(value, _mouseClickTween))
                    return;

                DisposeManager.Dispose(_mouseClickTween);

                _mouseClickTween = value;
            }
        }

        public bool IsCtrlDown()
        {
            return GetKey(KeyCode.LeftControl) || GetKeyDown(KeyCode.LeftControl);
        }

        public bool GetMouseIsInScreen()
        {
            return Input.mousePosition.x > 0 && Input.mousePosition.x < Screen.width && Input.mousePosition.y > 0 && Input.mousePosition.y < Screen.height;
        }

        public float GetScrollDelta()
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }

        public bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public bool GetMouseButton(int button)
        {
            return Input.GetMouseButton(button);
        }

        public float GetDistanceDelta()
        {
            float distanceDelta = 0.0f;

            float pinchDelta = GetPinchDelta() / 500.0f;
            if (pinchDelta != 0.0f)
                distanceDelta = -pinchDelta;
            else
            {
                if (GetKeyDown(KeyCode.Equals) || GetKeyDown(KeyCode.Minus))
                    distanceDelta = GetKey(KeyCode.Equals) ? -0.1f : 0.1f;
                else
                {
                    float scrollDelta = inputManager.GetScrollDelta();
                    bool mouseButtonDown = GetMouseButton(0) || GetMouseButton(1) || GetMouseButton(2);
                    if (scrollDelta != 0.0f && GetMouseIsInScreen() && !mouseButtonDown)
                        distanceDelta = -scrollDelta;
                    else if (GetTapClickCount() == 2)
                        distanceDelta = -0.2f;
                }
            }

            return distanceDelta;
        }

        public int GetTapClickCount()
        {
            return LeanTouch.Fingers.Count > 0 ? LeanTouch.Fingers[0].TapCount : 0;
        }

        public int GetFingerMouseCount()
        {
            return LeanTouch.Fingers.Count;
        }

        public int GetFingerMouseDownCount()
        {
            int downCount = 0;
            foreach (LeanFinger finger in LeanTouch.Fingers)
                downCount += finger.Down ? 1 : 0;
            return downCount;
        }

        public float GetPinchDelta()
        {
            return LeanGesture.GetScreenDistance() - LeanGesture.GetLastScreenDistance();
        }

        public Vector3 GetScreenCenter()
        {            
            return LeanTouch.Fingers.Count > 0 ? LeanGesture.GetScreenCenter() : Input.mousePosition;
        }

        public Vector3 GetForwardVectorDelta()
        {
            Vector3 forwardVectorDelta = Vector3.zero;

            int fingerMouseCount = GetFingerMouseCount();
            if ((IsCtrlDown() && fingerMouseCount == 1 && Input.GetMouseButton(0)) || fingerMouseCount > 1)
            {
                Vector2 positionDelta = LeanGesture.GetScreenDelta();
                positionDelta = new Vector3(-positionDelta.y / 10.0f, positionDelta.x / 8.0f);
                if (fingerMouseCount > 1)
                {
                    forwardVectorDelta.x = positionDelta.x;
                    forwardVectorDelta.y = LeanGesture.GetTwistDegrees();
                }
                else
                    forwardVectorDelta = positionDelta;
            }
            return forwardVectorDelta;
        }

        public void UpdateCameraMouse(Camera camera)
        {
            if (Application.isPlaying && camera == Camera.main)
            {
                RaycastHitDouble closestHit = null;
           
                Camera mouseOverWindowCamera = cameraManager.GetMouseOverWindowCamera();
                if (mouseOverWindowCamera == camera)
                {
                    //Problem: Sometimes mouse events are not registered on MeshRendererVisuals that are far away from the origin
                    //Fix: We use Raycasting and call the mouseDown/mouseUp if we identify a mouse event
                    //TODO: Consider perhaps origin shifting will fix this naturally?
                    RaycastHitDouble[]  hits = PhysicsDouble.TerrainFiltered(PhysicsDouble.CameraRaycastAll(camera, GetScreenCenter(), camera.GetFarDistanceClipPlane()), camera);
       
                    if (hits.Length != 0)
                    {
                        closestHit = PhysicsDouble.GetClosestHit(camera, hits);
                        if (closestHit != null)
                        {
                            Vector3Double viewportPoint = camera.WorldToViewportPoint(closestHit.point);
                            if (viewportPoint.x > 1.0d || viewportPoint.x < 0.0 || viewportPoint.y > 1.0d || viewportPoint.y < 0.0)
                                closestHit = null;
                        }
                    }
                }

                MouseMove(closestHit);
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                mouseClickTween = null;

                return true;
            }
            return false;
        }
    }
}
