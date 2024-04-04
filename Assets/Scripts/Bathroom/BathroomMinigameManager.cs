using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BathroomMinigameManager : MonoBehaviour
{
    public static BathroomMinigameManager Instance;

    [Header("Database")]
    [SerializeField] private SelfCareItemDatabase itemDatabase;

    [Header("References")]
    [SerializeField] private GameObject miniGameUI;
    [SerializeField] private Collider2D gameTrigger;

    [SerializeField] private Transform[] goodSlots;
    [SerializeField] private Transform[] badSlots;
    [SerializeField] private TextMeshProUGUI currentValue;
    [SerializeField] private TextMeshProUGUI targetValue;

    [Header("Info Items")]
    [SerializeField] private Transform itemDisplayParent;

    [SerializeField] private GameObject itemHolderPrefab;

    [Header("Clickable Items")]
    [SerializeField] private Transform clickableItemsParent;

    [SerializeField] private GameObject clickableItemPrefab;

    private PlayerController _playerController;

    private bool _isMiniGameOn;
    private bool _targetAlreadyCalculated;

    private List<SelfCareItem> selectedItems = new List<SelfCareItem>();

    private int _targetScore;
    private int _currentScore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else { Destroy(this.gameObject); }
    }

    private void Start()
    {
        GameObject.FindGameObjectWithTag("Player").TryGetComponent(out _playerController);
        
        // Reset Text
        CalculateTargetValue();

        currentValue.text = 0.ToString();
    }

    private void Update()
    {
        if (_isMiniGameOn)
        {
            _playerController.StopPlayerMovement(); // can make movement speed 0 instead of updating every frame but is similar anyways?
        }
    }

    public void StartMinigame()
    {
        _isMiniGameOn = true;

        // Set up the game
        ToggleUI();

        gameTrigger.enabled = false;

        PopulateItems();
    }

    private void ToggleUI()
    {
        miniGameUI.SetActive(_isMiniGameOn);
    }

    private void PopulateItems()
    {
        foreach (SelfCareItem item in itemDatabase.Items)
        {
            // Item Information Display
            Instantiate(itemHolderPrefab, itemDisplayParent).TryGetComponent(out UISelfCareItemHolder holder);
            holder.careItem = item;
            holder.Initialize();

            // Selectable Items Display
            Instantiate(clickableItemPrefab, clickableItemsParent.transform).TryGetComponent(out UISelfCareItemHolder clickableHolder);
            clickableHolder.careItem = item;
            clickableHolder.Initialize();
        }
    }

    public void ItemSelectedFromDisplay(SelfCareItem selfCareItem) // For Buttons
    {
        // May change
        if (selfCareItem.ItemValue == 0) return; // Should be a default value, so no item should have 0

        if (selfCareItem.ItemValue > 0) // Positive Good Negative Bad
        {
            FindEmptySlot(goodSlots, selfCareItem);
        }
        else
        {
            FindEmptySlot(badSlots, selfCareItem);
        }

        UpdateTargetValues();
    }

    private void FindEmptySlot(Array slots, SelfCareItem careItem)
    {
        if (selectedItems.Contains(careItem)) // List includes item already
        {
            return;
        }

        foreach (Transform slot in slots) // Go through slots
        {
            slot.TryGetComponent(out GoodAndBadSlot slotInfo); // Get slot info

            if (slotInfo.GetItem() == null) // Check if already occupied
            {
                slotInfo.Initialize(careItem);
                selectedItems.Add(careItem);
                return;
            }
        }
    }

    private void UpdateTargetValues()
    {
        // Calculate current value
        int sum = 0;

        foreach (var item in selectedItems)
        {
            sum += item.ItemValue;
        }

        _currentScore = sum;
        currentValue.text = sum.ToString();
        
        if (_currentScore == _targetScore && selectedItems.Count == 5)
        {
            GameCompleted();
        }
    }

    private void GameCompleted()
    {
        _isMiniGameOn = false;

        ToggleUI();
    }

    private void CalculateTargetValue()
    {
        if (!_targetAlreadyCalculated)
        {
            int positiveTargets = 0, negativeTargets = 0;

            List<SelfCareItem> targets = new List<SelfCareItem>();

            // A randomised target list using item database
            System.Random randomGen = new System.Random();

            bool targetNeeded = true;

            while (targetNeeded)
            {
                int index = randomGen.Next(0, itemDatabase.Items.Length);

                SelfCareItem item = itemDatabase.Items[index];

                if (!targets.Contains(item))
                {
                    if (positiveTargets < 3 && item.ItemValue > 0) // positive value needed
                    {
                        positiveTargets++;
                        targets.Add(itemDatabase.Items[index]);
                    }
                    else if (negativeTargets < 2 && item.ItemValue < 0) // negative value needed
                    {
                        negativeTargets++;
                        targets.Add(itemDatabase.Items[index]);
                    }
                }

                if (targets.Count == 5)
                {
                    targetNeeded = false;
                }
            }

            // Calculate target value
            _targetScore = 0;

            foreach (var item in targets)
            {
                _targetScore += item.ItemValue;
            }

            targetValue.text = _targetScore.ToString();

            _targetAlreadyCalculated = true;
        }
    }

    public bool RemoveItemFromList(SelfCareItem selfCareItem)
    {
        bool returnValue = selectedItems.Remove(selfCareItem);
        UpdateTargetValues();
        return returnValue;
    }
}