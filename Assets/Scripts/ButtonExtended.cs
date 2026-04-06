using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Enhanced button with object enable/disable transition support.
    /// </summary>
    [AddComponentMenu("UI/Button Extended", 30)]
    public class ButtonExtended : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [Serializable]
        /// <summary>
        /// Function definition for a button click event.
        /// </summary>
        public class ButtonClickedEvent : UnityEvent {}

        // Event delegates triggered on click.
        [FormerlySerializedAs("onClick")]
        [SerializeField]
        private ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

        [Header("Object Enable Transition")]
        [Tooltip("GameObject to enable when button is pressed")]
        [SerializeField]
        private GameObject m_OnPressObject;

        [Tooltip("GameObject to enable when button is not interactable")]
        [SerializeField]
        private GameObject m_OnDisabledObject;

        [Tooltip("Enable object transition system")]
        [SerializeField]
        private bool m_UseObjectTransition = false;

        private SelectionState m_CurrentState = SelectionState.Normal;

        protected ButtonExtended()
        {}

        /// <summary>
        /// UnityEvent that is triggered when the button is pressed.
        /// Note: Triggered on MouseUp after MouseDown on the same object.
        /// </summary>
        public ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
            set { m_OnClick = value; }
        }

        /// <summary>
        /// GameObject that will be enabled when button is pressed
        /// </summary>
        public GameObject onPressObject
        {
            get { return m_OnPressObject; }
            set { m_OnPressObject = value; }
        }

        /// <summary>
        /// GameObject that will be enabled when button is disabled/not interactable
        /// </summary>
        public GameObject onDisabledObject
        {
            get { return m_OnDisabledObject; }
            set { m_OnDisabledObject = value; }
        }

        /// <summary>
        /// Enable or disable the object transition system
        /// </summary>
        public bool useObjectTransition
        {
            get { return m_UseObjectTransition; }
            set 
            { 
                m_UseObjectTransition = value;
                UpdateObjectStates(currentSelectionState);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateObjectStates(currentSelectionState);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Clean up object states when disabled
            if (m_UseObjectTransition)
            {
                SetObjectActive(m_OnPressObject, false);
                SetObjectActive(m_OnDisabledObject, false);
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            
            if (m_UseObjectTransition)
            {
                UpdateObjectStates(state);
            }
            
            m_CurrentState = state;
        }

        private void UpdateObjectStates(SelectionState state)
        {
            if (!m_UseObjectTransition)
                return;

            // Handle disabled state
            if (!IsInteractable())
            {
                SetObjectActive(m_OnPressObject, false);
                SetObjectActive(m_OnDisabledObject, true);
                return;
            }

            // Handle other states
            switch (state)
            {
                case SelectionState.Normal:
                case SelectionState.Highlighted:
                case SelectionState.Selected:
                    SetObjectActive(m_OnPressObject, false);
                    SetObjectActive(m_OnDisabledObject, false);
                    break;

                case SelectionState.Pressed:
                    SetObjectActive(m_OnPressObject, true);
                    SetObjectActive(m_OnDisabledObject, false);
                    break;

                case SelectionState.Disabled:
                    SetObjectActive(m_OnPressObject, false);
                    SetObjectActive(m_OnDisabledObject, true);
                    break;
            }
        }

        private void SetObjectActive(GameObject obj, bool active)
        {
            if (obj != null && obj.activeSelf != active)
            {
                obj.SetActive(active);
            }
        }

        private void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            m_OnClick.Invoke();
        }

        /// <summary>
        /// Call all registered IPointerClickHandlers.
        /// Register button presses using the IPointerClickHandler.
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        // Override pointer down to ensure pressed state is applied
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            
            if (m_UseObjectTransition && IsInteractable())
            {
                SetObjectActive(m_OnPressObject, true);
                SetObjectActive(m_OnDisabledObject, false);
            }
        }

        // Override pointer up to ensure normal state is restored
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            
            if (m_UseObjectTransition && IsInteractable())
            {
                SetObjectActive(m_OnPressObject, false);
            }
        }

        // Override pointer exit to handle mouse leaving while pressed
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            
            if (m_UseObjectTransition && IsInteractable())
            {
                SetObjectActive(m_OnPressObject, false);
            }
        }

        /// <summary>
        /// Call all registered ISubmitHandler.
        /// </summary>
        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            // if we get set disabled during the press
            // don't run the coroutine.
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Update object states in editor when properties change
            if (Application.isPlaying && m_UseObjectTransition)
            {
                UpdateObjectStates(currentSelectionState);
            }
        }
#endif
    }
}
