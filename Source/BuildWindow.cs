using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExBuildWindow : MonoBehaviour
	{
		public class Styles {
			public static GUIStyle normal;
			public static GUIStyle red;
			public static GUIStyle yellow;
			public static GUIStyle green;
			public static GUIStyle white;
			public static GUIStyle label;
			public static GUIStyle slider;
			public static GUIStyle sliderText;

			public static GUIStyle listItem;
			public static GUIStyle listBox;

			public static ProgressBar bar;

			private static bool initialized;

			public static void Init ()
			{
				if (initialized)
					return;
				initialized = true;

				normal = new GUIStyle (GUI.skin.button);
				normal.normal.textColor = normal.focused.textColor = Color.white;
				normal.hover.textColor = normal.active.textColor = Color.yellow;
				normal.onNormal.textColor = normal.onFocused.textColor = normal.onHover.textColor = normal.onActive.textColor = Color.green;
				normal.padding = new RectOffset (8, 8, 8, 8);

				red = new GUIStyle (GUI.skin.box);
				red.padding = new RectOffset (8, 8, 8, 8);
				red.normal.textColor = red.focused.textColor = Color.red;

				yellow = new GUIStyle (GUI.skin.box);
				yellow.padding = new RectOffset (8, 8, 8, 8);
				yellow.normal.textColor = yellow.focused.textColor = Color.yellow;

				green = new GUIStyle (GUI.skin.box);
				green.padding = new RectOffset (8, 8, 8, 8);
				green.normal.textColor = green.focused.textColor = Color.green;

				white = new GUIStyle (GUI.skin.box);
				white.padding = new RectOffset (8, 8, 8, 8);
				white.normal.textColor = white.focused.textColor = Color.white;

				label = new GUIStyle (GUI.skin.label);
				label.normal.textColor = label.focused.textColor = Color.white;
				label.alignment = TextAnchor.MiddleCenter;

				slider = new GUIStyle (GUI.skin.horizontalSlider);
				slider.margin = new RectOffset (0, 0, 0, 0);

				sliderText = new GUIStyle (GUI.skin.label);
				sliderText.alignment = TextAnchor.MiddleCenter;
				sliderText.margin = new RectOffset (0, 0, 0, 0);

				listItem = new GUIStyle ();
				listItem.normal.textColor = Color.white;
				Texture2D texInit = new Texture2D(1, 1);
				texInit.SetPixel(0, 0, Color.white);
				texInit.Apply();
				listItem.hover.background = texInit;
				listItem.onHover.background = texInit;
				listItem.hover.textColor = Color.black;
				listItem.onHover.textColor = Color.black;
				listItem.padding = new RectOffset(4, 4, 4, 4);

				listBox = new GUIStyle(GUI.skin.box);

				bar = new ProgressBar (XKCDColors.Azure,
									   XKCDColors.ElectricBlue,
									   new Color(255, 255, 255, 0.8f));
			}
		}

		static ExBuildWindow instance;
		static bool hide_ui = false;
		static bool gui_enabled = true;
		static Rect windowpos;
		static bool highlight_pad = true;
		static bool link_lfo_sliders = true;

		static CraftBrowser craftlist = null;
		static Vector2 resscroll;

		List<ExLaunchPad> launchpads;
		DropDownList pad_list;
		ExLaunchPad pad;

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void ShowGUI ()
		{
			gui_enabled = true;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				windowpos.x = pos.x;
				windowpos.y = pos.y;
				windowpos.width = pos.z;
				windowpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out gui_enabled);
			}
			val = node.GetValue ("link_lfo_sliders");
			if (val != null) {
				bool.TryParse (val, out link_lfo_sliders);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			Quaternion pos;
			pos.x = windowpos.x;
			pos.y = windowpos.y;
			pos.z = windowpos.width;
			pos.w = windowpos.height;
			node.AddValue ("rect", KSPUtil.WriteQuaternion (pos));
			node.AddValue ("visible", gui_enabled);
			node.AddValue ("link_lfo_sliders", link_lfo_sliders);
		}

		void BuildPadList (Vessel v)
		{
			if (pad != null) {
				pad.part.SetHighlightDefault ();
			}
			launchpads = null;
			pad_list = null;
			pad = null;	//FIXME would be nice to not lose the active pad
			var pads = new List<ExLaunchPad> ();

			foreach (var p in v.Parts) {
				pads.AddRange (p.Modules.OfType<ExLaunchPad> ());
			}
			if (pads.Count > 0) {
				launchpads = pads;
				pad = launchpads[0];
				var pad_names = new List<string> ();
				int ind = 0;
				foreach (var p in launchpads) {
					if (p.PadName != "") {
						pad_names.Add (p.PadName);
					} else {
						pad_names.Add ("pad-" + ind);
					}
					ind++;
				}
				pad_list = new DropDownList (pad_names);
			}
		}

		void onVesselChange (Vessel v)
		{
			BuildPadList (v);
			UpdateGUIState ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (FlightGlobals.ActiveVessel == v) {
				BuildPadList (v);
			}
		}

		void UpdateGUIState ()
		{
			enabled = !hide_ui && launchpads != null && gui_enabled;
			if (pad != null) {
				if (enabled && highlight_pad) {
					pad.part.SetHighlightColor (XKCDColors.LightSeaGreen);
					pad.part.SetHighlight (true);
				} else {
					pad.part.SetHighlightDefault ();
				}
			}
			if (launchpads != null) {
				foreach (var p in launchpads) {
					p.UpdateMenus (enabled && p == pad);
				}
			}
		}

		void onHideUI ()
		{
			hide_ui = true;
			UpdateGUIState ();
		}

		void onShowUI ()
		{
			hide_ui = false;
			UpdateGUIState ();
		}

		void Awake ()
		{
			instance = this;
			GameEvents.onVesselChange.Add (onVesselChange);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);
			enabled = false;
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onVesselChange.Remove (onVesselChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
		}

		float ResourceLine (string label, string resourceName, float fraction,
							double minAmount, double maxAmount,
							double available)
		{
			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			// Fill amount
			// limit slider to 0.5% increments
			GUILayout.BeginVertical ();
			if (minAmount == maxAmount) {
				GUILayout.Box ("Must be 100%", GUILayout.Width (300),
							   GUILayout.Height (20));
				fraction = 1.0F;
			} else {
				fraction = GUILayout.HorizontalSlider (fraction, 0.0F, 1.0F,
													   Styles.slider,
													   GUI.skin.horizontalSliderThumb,
													   GUILayout.Width (300),
													   GUILayout.Height (20));
				fraction = (float)Math.Round (fraction, 3);
				fraction = (Mathf.Floor (fraction * 200)) / 200;
				GUILayout.Box ((fraction * 100).ToString () + "%",
							   Styles.sliderText, GUILayout.Width (300),
							   GUILayout.Height (20));
			}
			GUILayout.EndVertical ();

			double required = minAmount + (maxAmount - minAmount) * fraction;

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (available >= 0 && available < required) {
				if (ExLaunchPad.timed_builds) {
					requiredStyle = Styles.yellow;
				} else {
					requiredStyle = Styles.red;
				}
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			if (available >= 0) {
				GUILayout.Box ((Math.Round (available, 2)).ToString (),
							   Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			} else {
				GUILayout.Box ("N/A", Styles.white, GUILayout.Width (75),
							   GUILayout.Height (40));
			}

			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();

			return fraction;
		}

		void ResourceProgress (string label, BuildCost.BuildResource br,
							   BuildCost.BuildResource req)
		{
			double fraction = (req.amount - br.amount) / req.amount;
			double required = br.amount;
			double available = pad.padResources.ResourceAmount (br.name);

			GUILayout.BeginHorizontal ();

			// Resource name
			GUILayout.Box (label, Styles.white, GUILayout.Width (120),
						   GUILayout.Height (40));

			GUILayout.BeginVertical ();
			var percent = (fraction * 100).ToString("G4") + "%";
			Styles.bar.Draw ((float) fraction, percent, 300);
			GUILayout.EndVertical ();

			// Calculate if we have enough resources to build
			GUIStyle requiredStyle = Styles.green;
			if (required > available) {
				requiredStyle = Styles.yellow;
			}
			// Required and Available
			GUILayout.Box ((Math.Round (required, 2)).ToString (),
						   requiredStyle, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.Box ((Math.Round (available, 2)).ToString (),
						   Styles.white, GUILayout.Width (75),
						   GUILayout.Height (40));
			GUILayout.FlexibleSpace ();

			GUILayout.EndHorizontal ();
		}

		void SelectPad_start ()
		{
			pad_list.styleListItem = Styles.listItem;
			pad_list.styleListBox = Styles.listBox;
			pad_list.DrawBlockingSelector ();
		}

		public static void SelectPad (ExLaunchPad selected_pad)
		{
			instance.Select_Pad (selected_pad);
		}

		void Select_Pad (ExLaunchPad selected_pad)
		{
			if (pad) {
				pad.part.SetHighlightDefault ();
			}
			pad = selected_pad;
			pad_list.SelectItem (launchpads.IndexOf (pad));
			UpdateGUIState ();
		}

		void SelectPad ()
		{
			GUILayout.BeginHorizontal ();
			pad_list.DrawButton ();
			highlight_pad = GUILayout.Toggle (highlight_pad, "Highlight Pad");
			Select_Pad (launchpads[pad_list.SelectedIndex]);
			GUILayout.EndHorizontal ();
		}

		void SelectPad_end ()
		{
			if (pad_list != null) {
				pad_list.DrawDropDown();
				pad_list.CloseOnOutsideClick();
			}
		}

		void SelectCraft ()
		{
			GUILayout.BeginHorizontal ("box");
			GUILayout.FlexibleSpace ();
			// VAB / SPH selection
			for (var t = ExLaunchPad.CraftType.VAB;
				 t <= ExLaunchPad.CraftType.SubAss;
				 t++) {
				if (GUILayout.Toggle (pad.craftType == t, t.ToString (),
									  GUILayout.Width (80))) {
					pad.craftType = t;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			string strpath = HighLogic.SaveFolder;

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Select Craft", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				string []dir = new string[] {"VAB", "SPH", "../Subassemblies"};
				var diff = HighLogic.CurrentGame.Parameters.Difficulty;
				bool stock = diff.AllowStockVessels;
				if (pad.craftType == ExLaunchPad.CraftType.SubAss) {
					diff.AllowStockVessels = false;
				}
				//GUILayout.Button is "true" when clicked
				var clrect = new Rect (Screen.width / 2, 100, 350, 500);
				craftlist = new CraftBrowser (clrect, dir[(int)pad.craftType],
											  strpath, "Select a ship to load",
											  craftSelectComplete,
											  craftSelectCancel,
											  HighLogic.Skin,
											  EditorLogic.ShipFileImage, true);
				diff.AllowStockVessels = stock;
			}
			GUI.enabled = pad.craftConfig != null;
			if (GUILayout.Button ("Clear", Styles.normal,
								  GUILayout.ExpandWidth (false))) {
				pad.UnloadCraft ();
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal ();
		}

		void SelectedCraft ()
		{
			var ship_name = pad.craftConfig.GetValue ("ship");
			GUILayout.Box ("Selected Craft:	" + ship_name, Styles.white);
		}

		void ResourceHeader ()
		{
			var width120 = GUILayout.Width (120);
			var width300 = GUILayout.Width (300);
			var width75 = GUILayout.Width (75);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Resource", Styles.label, width120);
			GUILayout.Label ("Fill Percentage", Styles.label, width300);
			GUILayout.Label ("Required", Styles.label, width75);
			GUILayout.Label ("Available", Styles.label, width75);
			GUILayout.EndHorizontal ();
		}

		void ResourceScroll_begin ()
		{
			resscroll = GUILayout.BeginScrollView (resscroll,
												   GUILayout.Width (625),
												   GUILayout.Height (300));
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();
		}

		void ResourceScroll_end ()
		{
			GUILayout.EndVertical ();
			GUILayout.Label ("", Styles.label, GUILayout.Width (15));
			GUILayout.EndHorizontal ();
			GUILayout.EndScrollView ();
		}

		bool RequiredResources ()
		{
			bool can_build = true;
			GUILayout.Label ("Resources required to build:", Styles.label,
							 GUILayout.ExpandWidth (true));
			foreach (var br in pad.buildCost.required) {
				double a = br.amount;
				double available = -1;

				available = pad.padResources.ResourceAmount (br.name);
				ResourceLine (br.name, br.name, 1.0f, a, a, available);
				if (br.amount > available) {
					can_build = false;
				}
			}
			return can_build;
		}

		void BuildButton ()
		{
			if (GUILayout.Button ("Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				pad.BuildCraft ();
			}
		}

		void FinalizeButton ()
		{
			if (GUILayout.Button ("Finalize Build", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				pad.BuildAndLaunchCraft ();
			}
		}

		internal static BuildCost.BuildResource FindResource (List<BuildCost.BuildResource> reslist, string name)
		{
			return reslist.Where(r => r.name == name).FirstOrDefault ();
		}

		void BuildProgress ()
		{
			foreach (var br in pad.builtStuff.required) {
				var req = FindResource (pad.buildCost.required, br.name);
				ResourceProgress (br.name, br, req);
			}
		}

		bool OptionalResources ()
		{
			bool can_build = true;

			link_lfo_sliders = GUILayout.Toggle (link_lfo_sliders,
												 "Link LiquidFuel and "
												 + "Oxidizer sliders");
			foreach (var br in pad.buildCost.optional) {
				double available = pad.padResources.ResourceAmount (br.name);
				double maximum = pad.craftResources.ResourceCapacity(br.name);
				float frac = (float) (br.amount / maximum);
				frac = ResourceLine (br.name, br.name, frac, 0,
									 maximum, available);
				if (link_lfo_sliders
					&& (br.name == "LiquidFuel" || br.name == "Oxidizer")) {
					string other;
					if (br.name == "LiquidFuel") {
						other = "Oxidizer";
					} else {
						other = "LiquidFuel";
					}
					var or = FindResource (pad.buildCost.optional, other);
					if (or != null) {
						double om = pad.craftResources.ResourceCapacity (other);
						or.amount = om * frac;
					}
				}
				br.amount = maximum * frac;
				if (br.amount > available) {
					can_build = false;
				}
			}
			return can_build;
		}

		void PauseButton ()
		{
			if (pad.paused) {
				if (GUILayout.Button ("Resume Build", Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					pad.TransferResources ();
					pad.ResumeBuild ();
				}
			} else {
				if (GUILayout.Button ("Pause Build", Styles.normal,
									  GUILayout.ExpandWidth (true))) {
					pad.TransferResources ();
					pad.PauseBuild ();
				}
			}
		}

		void ReleaseButton ()
		{
			if (GUILayout.Button ("Release", Styles.normal,
								  GUILayout.ExpandWidth (true))) {
				pad.TransferResources ();
				pad.ReleaseVessel ();
			}
		}

		void CloseButton ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				HideGUI ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void WindowGUI (int windowID)
		{
			Styles.Init ();

			SelectPad_start ();

			GUILayout.BeginVertical ();
			SelectPad ();

			if (ExLaunchPad.timed_builds) {
				switch (pad.state) {
				case ExLaunchPad.State.Idle:
					SelectCraft ();
					break;
				case ExLaunchPad.State.Planning:
					SelectCraft ();
					SelectedCraft ();
					ResourceScroll_begin ();
					RequiredResources ();
					ResourceScroll_end ();
					BuildButton ();
					break;
				case ExLaunchPad.State.Building:
					SelectedCraft ();
					ResourceScroll_begin ();
					BuildProgress ();
					ResourceScroll_end ();
					PauseButton ();
					break;
				case ExLaunchPad.State.Complete:
					FinalizeButton ();
					break;
				case ExLaunchPad.State.Transfer:
					SelectedCraft ();
					ResourceScroll_begin ();
					OptionalResources ();
					ResourceScroll_end ();
					ReleaseButton ();
					break;
				}
			} else {
				switch (pad.state) {
				case ExLaunchPad.State.Idle:
					SelectCraft ();
					break;
				case ExLaunchPad.State.Planning:
					SelectCraft ();
					SelectedCraft ();
					ResourceScroll_begin ();
					bool have_required = RequiredResources ();
					bool have_optional = OptionalResources ();
					ResourceScroll_end ();
					if (!ExLaunchPad.useResources
						|| (have_required && have_optional)) {
						BuildButton ();
					}
					break;
				case ExLaunchPad.State.Building:
					// shouldn't happen
					break;
				case ExLaunchPad.State.Complete:
					SelectedCraft ();
					ReleaseButton ();
					break;
				}
			}

			GUILayout.EndVertical ();

			CloseButton ();

			SelectPad_end ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		private void craftSelectComplete (string filename, string flagname)
		{
			craftlist = null;
			pad.LoadCraft (filename, flagname);
		}

		private void craftSelectCancel ()
		{
			craftlist = null;
		}

		void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;
			string sit = pad.vessel.situation.ToString ();
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  "Extraplanetary Launchpad: " + sit,
										  GUILayout.Width (640));
			if (craftlist != null) {
				craftlist.OnGUI ();
			}
		}
	}
}
