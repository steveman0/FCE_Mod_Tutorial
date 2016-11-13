using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using FortressCraft.Community.Utilities;    //The community tools Pack! Highly recommend for useful functions: https://github.com/steveman0/FCECommunityTools/tree/UIUtil

public class MyModMachine : MachineEntity // Add Interfaces such as PowerConsumerInterface or the Storage interfaces
{
    //Field definitions go here
    public int data;
    public GameObject HoloCubePreview;
    public bool mbLinkedToGo;

    //Constructor - called on machine creation - should use the parameters as called below to retain compatibility if mod stuff changes
    //You can add in your own parameters if you need them though
    public MyModMachine(ModCreateSegmentEntityParameters parameters)
    : base(parameters)
    {
        //Handle initializing the machine here
        this.mbNeedsLowFrequencyUpdate = true; //Only gets LowFrequencyUpdate called if true
        this.mbNeedsUnityUpdate = true; //Only gets UnityUpdate called if true
    }

    //This is called when spawning the model for the machine
    //You can use the function to override what model loads in
    //This is useful for if you have multiple value entries representing different machines and you want to use different game objects for each
    public override void SpawnGameObject()
    {
        base.SpawnGameObject();
    }

    public override void LowFrequencyUpdate()
    {
        //Probably the main function machines use.  This is run 5 times a second to handle machine updates.
        
        //Time in seconds since last update, generally close to 0.2 but using this guarantees accurate timing when time stepping occurs
        float seconds = LowFrequencyThread.mrPreviousUpdateTimeStep;

        this.MarkDirtyDelayed(); //Call this to indicate that the machine requies saving to disk
    }

    public override void UnityUpdate()
    {
        //The second main function.  This is called every frame and is intended for all Unity related functions.
        //It's called on the main thread so anything involving GameObjects, audio, or other Unity functions can be dealt with here
        //Because it is called every frame be mindful of how much you put here!! LowFrequencyUpdate is better for most gameplay calculations.

        //Only run this once to avoid creating duplicate objects
        if (!mbLinkedToGo)
        {
            //Instantiate a new gameobject, attach it to the parent object, and position it relative to the original - in this case a holo preview of a cube
            //Keep in mind that if you create extra objects like this you should keep a reference of them and remove them later if the machine is deleted
            GameObject lObj = SpawnableObjectManagerScript.instance.maSpawnableObjects[(int)SpawnableObjectEnum.MassStorageOutputPort].transform.Search("HoloCube").gameObject;
            HoloCubePreview = (GameObject)GameObject.Instantiate(lObj, this.mWrapper.mGameObjectList[0].gameObject.transform.position + new Vector3(0.0f, 0.75f, 0.0f), Quaternion.identity);
            this.mbLinkedToGo = true;
        }
    }

    public override string GetPopupText()
    {
        string MyPopupTextBox = "My text here";

        //This function is called to get the text that goes in the machine panel in the bottom left corner
        //It is called every frame while you are looking at the machine
        //Consider putting the machine name, indications of what Q, E, T or other buttons do when you press them while looking at it

        //Because it is called when looking at the machine it's the ideal place to put functions that are called for player interaction
        if (Input.GetButtonDown("Interact")) ; //When pressing 'E'
        if (Input.GetButtonDown("Store")) ; //When pressing 'T'
        if (Input.GetButtonDown("Extract")) ; //When pressing 'Q'
        if (Input.GetKeyDown(KeyCode.LeftShift)) ; //You can add modifiers too!

        return MyPopupTextBox;
    }

    //This is called when a player deletes the machine - good for disconnecting machine elements are triggering end of life effects
    public override void OnDelete()
    {
        base.OnDelete();
    }

    //This is called when the machine is rotated
    public override void OnUpdateRotation(byte newFlags)
    {
        //These 4 lines of code are useful if you need to restrict it so the machine doesn't rotate
        //They override the rotation and returning it to the original value
        int x = (int)(this.mnX - mSegment.baseX);
        int y = (int)(this.mnY - mSegment.baseY);
        int z = (int)(this.mnZ - mSegment.baseZ);
        mSegment.maCubeData[(y << 8) + (z << 4) + x].meFlags = this.mFlags;
    }

    //This is called when the unity object is disabled due to being out of range
    //If you create any extra GameObjects you should remove them here so they don't take up resources while the player is away
    public override void UnitySuspended()
    {
        GameObject.Destroy(this.HoloCubePreview);
        this.mbLinkedToGo = false;
        base.UnitySuspended();
    }

    //This method is for defining a holobase display cube for your machine
    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        return base.CreateHolobaseEntity(holobase);
    }

    //You can also update the holobase for position/size if you have a machine that moves (like lifts for example)
    public override void HolobaseUpdate(Holobase holobase, HoloMachineEntity holoMachineEntity)
    {
        base.HolobaseUpdate(holobase, holoMachineEntity);
    }

    //You have to override this as true if you want this machine to write and read network updates
    public override bool ShouldNetworkUpdate()
    {
        return true;
    }

    //This function is called in place of Write for writing data that will be sent as a network update
    public override void WriteNetworkUpdate(BinaryWriter writer)
    {
        //Consider adding machine state information in here to be sure that the machine in in sync with the client at all times
        base.WriteNetworkUpdate(writer);
    }

    //Handling for reading network updates
    public override void ReadNetworkUpdate(BinaryReader reader)
    {
        base.ReadNetworkUpdate(reader);
    }

    //This version number is passed to the save/load routine to make sure machines are always compatible even if the code changes between version
    //The game has a history of maintaining compatibility between versions - this is how it does it.  You should try to do the same :)
    public override int GetVersion()
    {
        return 3; 
    }

    //You have override this as true if you want to write entity data to disk
    public override bool ShouldSave()
    {
        return true;
    }

    //Called to write machine data to disk
    public override void Write(BinaryWriter writer)
    {
        writer.Write(data);
    }

    //Called when loading in the machine from disk - entityVersion is the number from GetVersion above when the machine was saved
    //If the version of a machine changes the older version number can be used to be sure that it is still loaded properly
    //If you change what data is written in the Write function you need to increment the version otherwise players with machines from the older versions will fail to load the old machine
    public override void Read(BinaryReader reader, int entityVersion)
    {
        switch (entityVersion)
        {
            case 1:
                {
                    //Read in entity version 1 data
                    data = reader.ReadInt32() + 25;
                    break;
                }
            case 2:
                {
                    //Read in entity version 2 data
                    data = reader.ReadInt32() * 2;
                    break;
                }
            case 3:
                {
                    //you get the idea
                    data = reader.ReadInt32();
                    break;
                }
        }

        //Alternately you can use if statements if data was only added at the end
        if (entityVersion > 2)
        {
            //Read in data that was added at version 3
        }
    }
}
