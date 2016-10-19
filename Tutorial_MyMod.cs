using UnityEngine;

public class MyMod : FortressCraftMod
{

    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterEntityHandler("MyAuthorID.MyModMachine");

        //It's nice to be able to confirm in the log that the mod registered properly and to know what version it is
        //Remember to increment it though if you change versions
        //This is strictly a nicety that I do with all my mods
        //It's helpful when trying to debug user's problems when you can see what mods in use from the log
        Debug.Log("My Mod V1 Registered!");

        //Register Network Commands for the machine
        UIManager.NetworkCommandFunctions.Add("MyAuthorID.MyModMachineInterface", new UIManager.HandleNetworkCommand(MyModMachineWindow.HandleNetworkCommand));

        return modRegistrationData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

        //Assumes that all value entries are handled by the same machine!  
        if (parameters.Cube == ModManager.mModMappings.CubesByKey["MyAuthorID.MyModMachine"].CubeType)
        {
            //You can set what object model will be spawned in for the machine
            parameters.ObjectType = SpawnableObjectEnum.Conveyor;
            result.Entity = new MyModMachine(parameters);
        }
        return result;
    }
}