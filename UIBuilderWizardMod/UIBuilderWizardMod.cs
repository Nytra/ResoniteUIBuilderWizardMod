using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System.Reflection;
using Elements.Core;
using System;

namespace UIBuilderWizardMod
{
	public class UIBuilderWizardMod : ResoniteMod
	{
		public override string Name => "UIBuilderWizardMod";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteAccessibleFullBodyCalibrator";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.UIBuilderWizardMod");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(FullBodyCalibratorDialog), "OnStartCalibration")]
		class AccessibleFullBodyCalibratorPatch
		{
			public static bool Prefix(FullBodyCalibratorDialog __instance)
			{
				// true: run the original method
				// false: skip the original method
				return true;
			}
		}
	}
}