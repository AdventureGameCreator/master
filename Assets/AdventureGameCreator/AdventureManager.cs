﻿using UnityEngine;
using UnityEngine.UI;
using AdventureGameCreator.Entities;
using AdventureGameCreator.UI;
using System;

namespace AdventureGameCreator
{
    // NOTE:    public fields removed but setters are required within properties in order to deserialize the xml data - I do not like!

    // NOTE:    Consider "actions" in more depth
    //          game actions = save / load / quit
    //          player actions = attack
    //          item actions = examine?  / use 
    //          actions may / may not be relevant in all conditions, for example, death / lose location - no point searching / inventory

    // NOTE:    Consider refactoring player input so that delegates are handled in the same way as
    //          the ObservableList delegates

    // NOTE:    Consider calculating option key based on connection descriptor / item name at run time?
    //          e.g first letter of string, second if first is taken etc
    //          Need to have some reserved characters for actions, such as [E]xamine, [L]ook etc

    // NOTE:    Could add a [RequiredComponent] of type AdventureDataService
    //          this would be an interface, allowing different types of the
    //          data service to be used, XML, database, www etc
    //          Would mean exposing the underyling data service though

    public class AdventureManager : MonoBehaviour
    {
        // UI components for display
        [SerializeField] private Text _locationTitle;
        [SerializeField] private Text _locationDescription;

        [SerializeField] private Text _inventory;

        // configuration
        private const string dataFilePath = "/StreamingAssets/XML/adventure_data.xml";
        private const int startLocation = 0;

        // enums
        private enum ActionState { AtLocation, ViewInventory, InventoryItemSelected };

        // private fields
        private Player _player = null;
        private Adventure _adventure = null;
        private Location _currentLocation = null;

        private InventoryDisplay _inventoryDisplay = null;

        private ActionState _actionState = 0;

        private bool _optionSelected = false;

        // delegate for managing keyboard input
        private delegate void OnKeyPress(string key);
        private OnKeyPress onKeyPress;


        /// <summary>
        /// Called when the object becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            SubscribeDelegates();
        }

        /// <summary>
        /// Called when the object becomes disabled and inactive
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeDelegates();
        }

        /// <summary>
        /// Subscribe our delegates
        /// </summary>
        private void SubscribeDelegates()
        {
            // option selected
            onKeyPress += Option_Selected;
        }

        /// <summary>
        /// Unsubscribe our delegates
        /// </summary>
        private void UnsubscribeDelegates()
        {
            // option selected
            onKeyPress -= Option_Selected;
        }

        /// <summary>
        /// Use this for initialisation
        /// </summary>
        private void Start()
        {
            // create new player
            _player = new Player();

            // load adventure data
            _adventure = Adventure.Load(Application.dataPath + dataFilePath);

            // set start location
            _currentLocation = _adventure.Locations[startLocation];

            // instantiate the inventory display
            _inventoryDisplay = new InventoryDisplay(_inventory, _player.Inventory);

            // hide inventory
            _inventoryDisplay.Disable();

            // wire up inventory and location item delegates
            _player.Inventory.Items.Updated += _inventoryDisplay.InventoryItems_Updated;
            _adventure.Locations.Changed += Location_Changed;       // TODO:    This won't work in the same way as location/items, may need an ObservableEntity
            _currentLocation.Items.Updated += LocationItems_Updated;
            _currentLocation.Items.Changed += LocationItems_Changed;

            Begin();
        }

        /// <summary>
        /// Handles the Changed method for the adventure's location collection
        /// </summary>
        private void Location_Changed(int obj)
        {
            DisplayCurrentLocation();
        }

        /// <summary>
        /// Handles the Updated method for the location's item collection
        /// </summary>
        private void LocationItems_Updated()
        {
            DisplayCurrentLocation();
        }

        /// <summary>
        /// Handles the Changed method for the location's item collection
        /// </summary>
        private void LocationItems_Changed(int index)
        {
            DisplayCurrentLocation();
        }

        /// <summary>
        /// Begins the adventure
        /// </summary>
        private void Begin()
        {
            _actionState = ActionState.AtLocation;
            DisplayCurrentLocation();
        }

        /// <summary>
        /// Displays the current location to the player
        /// </summary>
        private void DisplayCurrentLocation()
        {
            _locationTitle.text = _currentLocation.Title;
            _locationDescription.text = _currentLocation.Description;

            DisplayItems();
            DisplayConnectionOptions();
            DisplayActions();
        }

        /// <summary>
        /// Displays available actions
        /// </summary>
        private void DisplayActions()
        {
            string actionOption = string.Empty;

            switch (_actionState)
            {
                case ActionState.AtLocation:

                    _locationDescription.text += "\n\n";

                    if (_currentLocation.IsSearchable)
                    {
                        if (!_currentLocation.IsSearched)
                        {
                            // actionOption = "[ " + actionOption.key + " ] " + actionOption.descriptor + "   ";
                            actionOption = "[S]earch   ";

                            _locationDescription.text += actionOption;
                        }
                    }

                    // actionOption = "[ " + actionOption.key + " ] " + actionOption.descriptor + "   ";
                    actionOption = "[I]nventory   ";

                    _locationDescription.text += actionOption;

                    break;

                case ActionState.ViewInventory:

                    _locationDescription.text += "\n\n";

                    // actionOption = "[ " + actionOption.key + " ] " + actionOption.descriptor + "   ";
                    actionOption = "[I]nventory   ";

                    _locationDescription.text += actionOption;

                    break;

                case ActionState.InventoryItemSelected:

                    _locationDescription.text += "\n\n";

                    // actionOption = "[ " + actionOption.key + " ] " + actionOption.descriptor + "   ";
                    actionOption = "[D]rop, [E]xamine, [U]se item, [C]ancel";

                    _locationDescription.text += actionOption;

                    break;
            }
        }

        /// <summary>
        /// Displays each connection option to the player
        /// </summary>
        private void DisplayConnectionOptions()
        {
            string connectionOption;

            _locationDescription.text += "\n\n";

            foreach (Connection connection in _currentLocation.Connections)
            {
                connectionOption = "[ " + connection.Key + " ] " + connection.Descriptor + "   ";

                _locationDescription.text += connectionOption;
            }
        }

        /// <summary>
        /// Displays each item option to the player
        /// </summary>
        private void DisplayItems()
        {
            string itemOption;

            _locationDescription.text += "\n\n";

            foreach (Item item in _currentLocation.Items)
            {
                if (item.IsVisible)
                {
                    itemOption = "[ " + item.Key + " ] " + item.Name + " ";

                    _locationDescription.text += itemOption;
                }
            }
        }

        /// <summary>
        /// Updates the player's current location
        /// </summary>
        /// <param name="locationID">The ID of the location</param>
        private void MoveToLocation(int locationID)
        {
            // TODO:    Consider mapping delegates to static rather than instance methods
            //          Follow tutorial again on Unity website

            // not sure if this is needed at this time
            _currentLocation.Items.Updated -= LocationItems_Updated;
            _currentLocation.Items.Changed -= LocationItems_Changed;

            _currentLocation = _adventure.Locations[locationID];

            // TODO:    This is just at test
            //          delegate allocations are going to be per "instance" of the object.
            //          When I was updating the _currentLocation to a new instance, it would
            //          lose the delegate it had.
            //          I may need to tidy this up before assigning more...!
            _currentLocation.Items.Updated += LocationItems_Updated;
            _currentLocation.Items.Changed += LocationItems_Changed;

            DisplayCurrentLocation();
        }

        /// <summary>
        /// Handles adventure option selection
        /// </summary>
        /// <param name="key">The key which was pressed</param>
        private void Option_Selected(string key)
        {
            // connection options
            CheckConnectionOptions(key);

            // item options
            CheckItemOptions(key);

            // inventory item options
            CheckInventoryItemOptions(key);

            // action options
            CheckActionOptions(key);
        }

        /// <summary>
        /// Checks to see if a connection has been selected
        /// </summary>
        /// <param name="key">The key which was pressed</param>
        private void CheckConnectionOptions(string key)
        {
            if (!_optionSelected)
            {
                if (_actionState == ActionState.AtLocation)
                {
                    foreach (Connection connection in _currentLocation.Connections)
                    {
                        if (connection.Key.ToUpper() == key.ToUpper())
                        {
                            _optionSelected = true;

                            MoveToLocation(connection.ID);

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if an item has been selected
        /// </summary>
        /// <param name="key">The key which was pressed</param>
        private void CheckItemOptions(string key)
        {
            if (!_optionSelected)
            {
                if (_actionState == ActionState.AtLocation)
                {
                    foreach (Item item in _currentLocation.Items)
                    {
                        if (item.IsVisible)
                        {
                            if (item.Key.ToUpper() == key.ToUpper())
                            {
                                _optionSelected = true;

                                _player.Take(item);

                                _currentLocation.Items.Remove(item);

                                break;
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Checks to see if an inventory item has been selected
        /// </summary>
        /// <param name="key">The key which was pressed</param>
        private void CheckInventoryItemOptions(string key)
        {
            if (!_optionSelected)
            {
                if (_actionState == ActionState.ViewInventory)
                {
                    foreach (Item item in _player.Inventory.Items)
                    {
                        if (item.Key.ToUpper() == key.ToUpper())
                        {
                            _optionSelected = true;

                            _actionState = ActionState.InventoryItemSelected;

                            item.IsSelected = true;
                            DisplayCurrentLocation();

                            break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks to see if an action has been selected
        /// </summary>
        /// <param name="key">The key which was pressed</param>
        private void CheckActionOptions(string key)
        {
            if (!_optionSelected)
            {

                switch (_actionState)
                {
                    case ActionState.AtLocation:

                        // TODO:    Refactor - this is "listening for"..
                        if (key.ToUpper() == "S")
                        {
                            _optionSelected = true;

                            if (_currentLocation.IsSearchable)
                            {
                                if (!_currentLocation.IsSearched)
                                {
                                    _player.Search(ref _currentLocation);
                                    DisplayCurrentLocation();
                                }
                            }
                        }

                        // TODO:    Refactor - this is "listening for"..
                        if (key.ToUpper() == "I")
                        {
                            _optionSelected = true;

                            _actionState = ActionState.ViewInventory;

                            _inventoryDisplay.Toggle();
                        }
                        break;

                    case ActionState.ViewInventory:

                        // TODO:    Refactor - this is "listening for"..
                        if (key.ToUpper() == "I")
                        {
                            _optionSelected = true;

                            _actionState = ActionState.AtLocation;

                            _inventoryDisplay.Toggle();
                        }
                        break;

                    case ActionState.InventoryItemSelected:

                        // Drop item
                        if (key.ToUpper() == "D")
                        {
                            _optionSelected = true;

                            foreach (Item item in _player.Inventory.Items)
                            {
                                if (item.IsSelected)
                                {
                                    item.IsSelected = false;

                                    _actionState = ActionState.ViewInventory;

                                    _player.Drop(item);

                                    _currentLocation.Items.Add(item);

                                    break;
                                }
                            }
                        }

                        // TODO:    Examine item
                        if (key.ToUpper() == "E")
                        {
                            _optionSelected = true;
                            throw new NotImplementedException("Examining items has not yet been implemented.");
                        }

                        // TODO:    Use item
                        if (key.ToUpper() == "U")
                        {
                            _optionSelected = true;
                            throw new NotImplementedException("Using items has not yet been implemented.");
                        }

                        // Drop item
                        if (key.ToUpper() == "C")
                        {
                            _optionSelected = true;

                            foreach (Item item in _player.Inventory.Items)
                            {
                                if (item.IsSelected)
                                {
                                    item.IsSelected = false;

                                    _actionState = ActionState.ViewInventory;   // change state back to inventory view

                                    DisplayCurrentLocation();

                                    break;
                                }
                            }
                        }

                        break;
                }
            }
        }



        /// <summary>
        /// Update is called once per frame
        /// </summary>
        private void Update()
        {
            CheckForPlayerInput();
        }




        /// <summary>
        /// 
        /// </summary>
        private void OnGUI()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (Input.GetKeyDown(e.keyCode))
                {
                    //  Debug.Log("Down: " + e.keyCode);
                }
            }
            else if (e.type == EventType.keyUp)
            {
                if (Input.GetKeyUp(e.keyCode))
                {
                    _optionSelected = false;       // NOTE:    Resets option selected bool
                }
            }
        }

        /// <summary>
        /// Checks for keyboard interaction
        /// </summary>
        private void CheckForPlayerInput()
        {
            if (Input.anyKeyDown)
            {
                string input = Input.inputString;

                if (input.Length > 0)
                {
                    input = input.Substring(0, 1);

                    onKeyPress(input);
                }
            }
        }
    }
}