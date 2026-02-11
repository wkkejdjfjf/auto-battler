using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HoldClickableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hold Settings")]
    [SerializeField] private float _holdDuration = 0.5f;
    [SerializeField] private float _initialClickRate = 5f; // Clicks per second
    [SerializeField] private bool _accelerateClicks = false;
    [SerializeField] private float _accelerationRate = 0.5f; // How quickly click rate increases
    [SerializeField] private float _maxClickRate = 20f; // Maximum clicks per second
    [SerializeField] public bool _debugMode = false; // Disable debug logging by default

    // Events
    public event Action OnClicked;
    public event Action OnHoldClicked;

    // Private variables
    private bool _isHoldingButton = false;
    private float _elapsedTime = 0f;
    private float _currentClickRate = 0f;
    private float _clickCooldown = 0f;
    private Button _button; // Reference to the Button component

    // Static registry of active hold buttons by item ID
    private static Dictionary<string, HoldClickableButtonState> _activeHoldButtons = new Dictionary<string, HoldClickableButtonState>();

    // Item ID to track this specific button instance
    [HideInInspector] public string itemId;

    private void Awake()
    {
        // Get reference to Button component
        _button = GetComponent<Button>();

        if (_debugMode)
            Debug.Log($"[{gameObject.name}] HoldClickableButton initialized");
    }

    private void OnEnable()
    {
        // Only log if debug mode is on
        if (_debugMode)
            Debug.Log($"[{gameObject.name}] HoldClickableButton enabled for itemId: {itemId}");

        // Check if we need to restore a holding state from our registry
        if (!string.IsNullOrEmpty(itemId) && _activeHoldButtons.TryGetValue(itemId, out HoldClickableButtonState state))
        {
            // Restore the state
            if (state.IsHolding && Time.time - state.LastUpdateTime < 0.1f)
            {
                _isHoldingButton = true;
                _elapsedTime = state.ElapsedTime;
                _currentClickRate = state.CurrentClickRate;
                _clickCooldown = state.ClickCooldown;

                if (_debugMode)
                    Debug.Log($"[{gameObject.name}] Restored holding state for {itemId}");
            }
        }
    }

    private void OnDisable()
    {
        // Save our state if we're holding and have a valid itemId
        if (_isHoldingButton && !string.IsNullOrEmpty(itemId))
        {
            _activeHoldButtons[itemId] = new HoldClickableButtonState
            {
                IsHolding = _isHoldingButton,
                ElapsedTime = _elapsedTime,
                CurrentClickRate = _currentClickRate,
                ClickCooldown = _clickCooldown,
                LastUpdateTime = Time.time
            };

            if (_debugMode)
                Debug.Log($"[{gameObject.name}] Saved holding state for {itemId}");
        }
        else if (_activeHoldButtons.ContainsKey(itemId))
        {
            // Clear the state if we're not holding
            _activeHoldButtons.Remove(itemId);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only process if button is interactable
        if (_button == null || !_button.interactable)
        {
            if (_debugMode) Debug.Log($"[{gameObject.name}] Button not interactable, ignoring press");
            return;
        }

        ToggleHoldingButton(true);
        if (_debugMode) Debug.Log($"[{gameObject.name}] OnPointerDown for {itemId}");

        // Save our state to the registry
        if (!string.IsNullOrEmpty(itemId))
        {
            _activeHoldButtons[itemId] = new HoldClickableButtonState
            {
                IsHolding = _isHoldingButton,
                ElapsedTime = _elapsedTime,
                CurrentClickRate = _currentClickRate,
                ClickCooldown = _clickCooldown,
                LastUpdateTime = Time.time
            };
        }
    }

    private void ToggleHoldingButton(bool isPointerDown)
    {
        _isHoldingButton = isPointerDown;
        if (isPointerDown)
        {
            _elapsedTime = 0;
            _currentClickRate = _initialClickRate;
            _clickCooldown = 0;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Check if we were holding the button first
        if (!_isHoldingButton)
            return;

        if (_debugMode) Debug.Log($"[{gameObject.name}] OnPointerUp, elapsed: {_elapsedTime:F2}s for {itemId}");

        // Only trigger a click if we haven't started continuous clicking
        if (_elapsedTime <= _holdDuration)
        {
            if (_button != null && _button.interactable)
            {
                if (_debugMode) Debug.Log($"[{gameObject.name}] Quick click detected for {itemId}");
                Click();
            }
        }

        // Reset state regardless of whether we clicked
        ToggleHoldingButton(false);

        // Remove from registry
        if (!string.IsNullOrEmpty(itemId) && _activeHoldButtons.ContainsKey(itemId))
        {
            _activeHoldButtons.Remove(itemId);
            if (_debugMode) Debug.Log($"[{gameObject.name}] Removed holding state for {itemId}");
        }
    }

    private void ManageButtonInteraction()
    {
        // Safeguard against destroyed objects and UI issues
        if (!this || !gameObject || !gameObject.activeInHierarchy || !enabled)
            return;

        // Skip if button is not being held
        if (!_isHoldingButton)
            return;

        // Make sure button reference is still valid
        if (_button == null)
            _button = GetComponent<Button>();

        // Check interactability on every frame
        if (_button == null || !_button.interactable)
        {
            // Button became non-interactable or destroyed while being held
            ToggleHoldingButton(false);

            // Remove from registry
            if (!string.IsNullOrEmpty(itemId) && _activeHoldButtons.ContainsKey(itemId))
            {
                _activeHoldButtons.Remove(itemId);
            }
            return;
        }

        _elapsedTime += Time.deltaTime;

        // Update the registry with our current state
        if (!string.IsNullOrEmpty(itemId))
        {
            _activeHoldButtons[itemId] = new HoldClickableButtonState
            {
                IsHolding = _isHoldingButton,
                ElapsedTime = _elapsedTime,
                CurrentClickRate = _currentClickRate,
                ClickCooldown = _clickCooldown,
                LastUpdateTime = Time.time
            };
        }

        // Check if we've reached the hold duration
        if (_elapsedTime >= _holdDuration)
        {
            // If we just reached the hold duration, log it
            if (_elapsedTime - Time.deltaTime < _holdDuration && _debugMode)
            {
                Debug.Log($"[{gameObject.name}] Hold threshold reached for {itemId}");
            }

            // Handle continuous clicking
            _clickCooldown -= Time.deltaTime;
            if (_clickCooldown <= 0)
            {
                // Try-catch to prevent errors if something happens to the button during a click
                try
                {
                    // Trigger the hold click event
                    HoldClick();

                    // Reset cooldown based on current click rate
                    _clickCooldown = 1f / _currentClickRate;

                    // Accelerate click rate if enabled
                    if (_accelerateClicks)
                    {
                        _currentClickRate += _accelerationRate * Time.deltaTime;
                        _currentClickRate = Mathf.Min(_currentClickRate, _maxClickRate);
                        if (_debugMode && _currentClickRate >= _maxClickRate)
                            Debug.Log($"[{gameObject.name}] Reached max click rate: {_maxClickRate}/sec for {itemId}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error during hold click: {e.Message}");
                    ToggleHoldingButton(false);

                    // Remove from registry on error
                    if (!string.IsNullOrEmpty(itemId) && _activeHoldButtons.ContainsKey(itemId))
                    {
                        _activeHoldButtons.Remove(itemId);
                    }
                }
            }
        }
    }

    private void Click()
    {
        if (_debugMode) Debug.Log($"[{gameObject.name}] Click event fired for {itemId}");
        OnClicked?.Invoke();
    }

    private void HoldClick()
    {
        if (_debugMode) Debug.Log($"[{gameObject.name}] HoldClick event fired for {itemId}");
        OnHoldClicked?.Invoke();
    }

    private void Update() => ManageButtonInteraction();

    // Static method to clear the registry (useful for scene changes)
    public static void ClearRegistry()
    {
        _activeHoldButtons.Clear();
    }
}

// A class to store the state of a hold button
[System.Serializable]
public class HoldClickableButtonState
{
    public bool IsHolding;
    public float ElapsedTime;
    public float CurrentClickRate;
    public float ClickCooldown;
    public float LastUpdateTime;
}