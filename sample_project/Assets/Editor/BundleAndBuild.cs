using UnityEngine;
using UnityEditor;

public class BundleAndBuild
{
    [MenuItem("Trash Dash Debug/Build with Bundle/Build")]
    static void Build()
    {
		string[] scenes = new string[] { 
			"Assets/Scenes/Start.unity", 
			"Assets/Scenes/Main.unity", 
			"Assets/Scenes/Shop.unity"  
		};

		PlayerSettings.Android.useCustomKeystore = true;

		PlayerSettings.Android.keystoreName = "endless_runner_android.keystore";
		PlayerSettings.Android.keystorePass = "password";

		PlayerSettings.Android.keyaliasName = "googleplay";
		PlayerSettings.Android.keyaliasPass = "password";

		BuildOptions buildOptions = BuildOptions.None;
		buildOptions |= BuildOptions.CleanBuildCache;
		buildOptions |= BuildOptions.DetailedBuildReport;
		//buildOptions |= BuildOptions.StrictMode;
		
		BuildPipeline.BuildPlayer(scenes, "Build/Android/TrashDash.apk", BuildTarget.Android, buildOptions);
	}

	[MenuItem("Trash Dash Debug/Build with Bundle/Build and Run")]
	static void BuildAndRun()
	{
		
	}
}
