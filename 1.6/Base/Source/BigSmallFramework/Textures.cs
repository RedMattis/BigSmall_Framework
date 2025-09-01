using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
	[StaticConstructorOnStartup]
	public static class Textures
	{
		public static Texture2D ColorPawn_Icon { get; } = ContentFinder<Texture2D>.Get("BS_UI/ColorPawn");
		public static Texture2D Mechanical_Icon { get; } = ContentFinder<Texture2D>.Get("BS_Traits/BS_Mechanical");
		public static Texture2D AlienIcon_Icon { get; } = ContentFinder<Texture2D>.Get("BS_Traits/Alien");
		public static Texture2D BrightnessTexture { get; } = ContentFinder<Texture2D>.Get("BS_UI/BrightnessGradient", true);
		public static Texture2D SliderHandle { get; } = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle");
	}
}
