using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Collections.Generic;
using System;

//Inheriting BaseMachineWindow is required to support the GenericMachinePanelScript for machine UI
public class MyModMachineWindow : BaseMachineWindow 
{
    //Interface definition for network commands
    //First an interface name must be defined - this is for the UIManager to map it to the right machine - using your author ID is recommended to avoid conflicts
    public const string InterfaceName = "MyAuthorID.MyModMachineInterface";

    //Each function must also have a string defined to associate it with a given command
    public const string InterfaceMyFunctionString = "MyFunctionString";
    public const string InterfaceMyFunctionItem = "MyFunctionItem";

    //UI panels should have a bool for marking when they need updated
    //Only mark it for updating when it needs it otherwise it'll be wasting time every frame running the code when it isn't required
    public static bool dirty;

    //If other players can do things to the machine that require you to update the UI this will tell it when it is needed
    public static bool networkredraw;


    ///////////////////////////////////////////////////////
    //UI Functions
    ///////////////////////////////////////////////////////

    //This is called when the GenericMachinePanelScript tries to show your machine UI window if you decide to implement a UI window
    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        MyModMachine machine = targetEntity as MyModMachine;

        if (machine == null)
            return;

        //Definition of window contents
        this.manager.SetTitle("My Machine Window Title");

        this.manager.AddIcon("itemicon", "empty", Color.white, 0, 0);
        this.manager.AddBigLabel("labelidentifier", "Label text", Color.white, 0, 60);
        this.manager.AddButton("buttonidentifier", "Button Text", 0, 120);
        this.manager.AddPowerBar("powerbaridentifier", 0, 180);

        //Mark it dirty if you have contents that need updating immediately
        dirty = true;
    }

    //Called every frame to update the window contents 
    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        MyModMachine machine = targetEntity as MyModMachine;

        //If the machine reference is lost we need to exit the window and remove the UI rules that lock the screen position allow cursor movement
        if (machine == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }

        //Redraw if network update requires a change in the window contents
        if (networkredraw)
            this.manager.RedrawWindow();

        //Some example update functions below
        this.manager.UpdatePowerBar("powerbaridentifier", 0, 100f);

        //This function hooks up the scroll wheel to the panel scroll bar
        GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue -= Input.GetAxis("Mouse ScrollWheel");

        //Only update content below if dirty - save time looking up icon information if they don't need to change
        if (!dirty)
            return;

        this.manager.UpdateIcon("itemicon", ItemManager.GetItemIcon(100), Color.white);
        this.manager.UpdateLabel("labelidentifier", "New Label Text", Color.white);

        //Set dirty to false once updated
        dirty = false;
    }

    //Called when a UI panel button is clicked
    public override bool ButtonClicked(string name, SegmentEntity targetEntity)
    {
        if (name == "buttonidentifier")
        {
            //Perform actions in response and set dirty if required
            MyModMachineWindow.MyFunctionString(WorldScript.mLocalPlayer, targetEntity as MyModMachine, 5);
            dirty = true;
            return true;
        }
        else if (name == "itemicon")
        {
            //You can trigger on clicking icons too (you can pass an item reference associated with the slow instead)
            MyModMachineWindow.MyFunctionItem(WorldScript.mLocalPlayer, targetEntity as MyModMachine, ItemManager.SpawnItem(100));
            return true;
        }
        return false;
    }

    //You can also get a call when the player's cursor hovers over an icon/button so that you can display info about it
    //Example code below for displaying item info in the hotbar
    public override void ButtonEnter(string name, SegmentEntity targetEntity)
    {
        MyModMachine machine = targetEntity as MyModMachine;

        if (name == "itemicon")
        {
            ItemBase itemForSlot = ItemManager.SpawnItem(100);
            int count = itemForSlot.GetAmount();
            if (itemForSlot == null)
                return;
            if (HotBarManager.mbInited)
            {
                HotBarManager.SetCurrentBlockLabel(ItemManager.GetItemName(itemForSlot));
            }
            else
            {
                if (!SurvivalHotBarManager.mbInited)
                    return;
                string name1 = !WorldScript.mLocalPlayer.mResearch.IsKnown(itemForSlot) ? "Unknown Material" : ItemManager.GetItemName(itemForSlot);
                if (count > 1)
                    SurvivalHotBarManager.SetCurrentBlockLabel(string.Format("{0} {1}", count, name1));
                else
                    SurvivalHotBarManager.SetCurrentBlockLabel(name1);
            }
        }
    }

    //For handling when a player drags an item to an object (icon slot for example) in the UI
    public override void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
    {
        base.HandleItemDrag(name, draggedItem, dragDelegate, targetEntity);
    }

    //Called when the UI is closed - good for resetting window states or associated variables
    public override void OnClose(SegmentEntity targetEntity)
    {
        base.OnClose(targetEntity);
    }

    ///////////////////////////////////////////////////////////////
    //Network command functions
    ///////////////////////////////////////////////////////////////

    public static bool MyFunctionString(Player player, MyModMachine machine, int data)
    {
        //handle data machine data update here 
        machine.data = data;
        machine.MarkDirtyDelayed();
        
        //This can be used to force a redraw of the window in the event that the data update requires the window to change for other players
        networkredraw = true;

        //Send the command to the server
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceMyFunctionString, data.ToString(), null, machine, 0.0f);
        return true;
    }

    public static bool MyFunctionItem(Player player, MyModMachine machine, ItemBase item)
    {
        //Simplistic item transfer function - player data is available too!
        if (WorldScript.mbIsServer)
            player.mInventory.AddItem(item);
        machine.MarkDirtyDelayed();
        dirty = true;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceMyFunctionItem, null, item, machine, 0.0f);
        return true;
    }

    //Handle network command function - Must be registered with the UIManager in order to be called properly
    //Registering it with the UIManager is best done in the FortressCraftMod class' Register method
    //The static function calls are designed to work equivalently on the server and client so that the two remain in sync
    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        MyModMachine machine = nic.target as MyModMachine;
        string key = nic.command;
        if (key != null)
        {
            if (key == InterfaceMyFunctionItem)
            {
                MyModMachineWindow.MyFunctionItem(player, machine, nic.itemContext);
            }
            else if (key == InterfaceMyFunctionString)
            {
                int data;
                //Parse the string data (safely) and send to the appropriate function
                if (int.TryParse(nic.payload ?? "0", out data))
                    MyModMachineWindow.MyFunctionString(player, machine, data);
            }
        }
        return new NetworkInterfaceResponse()
        {
            entity = machine,
            inventory = player.mInventory
        };
    }
}
