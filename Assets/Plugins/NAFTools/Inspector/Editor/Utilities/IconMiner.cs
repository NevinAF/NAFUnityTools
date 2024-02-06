using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace NAF.Inspector.Editor
{
	public static class IconMiner
	{
		[MenuItem("Tools/NAF/Generate EditorIcons.cs")]
		private static void GenerateClass()
		{
			EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
			try {
				var editorAssetBundle = GetEditorAssetBundle();
				var iconsPath = GetIconsPath();
				var classContents = new StringBuilder();
				Dictionary<string, string> icons = new Dictionary<string, string>();


				var assetNames = EnumerateIcons(editorAssetBundle, iconsPath).ToArray();
				for (var i = 0; i < assetNames.Length; i++)
				{
					var assetName = assetNames[i];
					var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
					if (icon == null)
						continue;

					StringBuilder memberName = new StringBuilder(icon.name, icon.name.Length + 1);
					if (char.IsDigit(memberName[0]))
						memberName.Insert(0, '_');
					for (int index = 0; index < memberName.Length; index++)
					{
						if (!char.IsLetterOrDigit(memberName[index]))
							memberName[index] = '_';
					}

					icons[memberName.ToString()] = icon.name;
				}

				using (FileStream fs = new FileStream("EditorIcons.cs", FileMode.Create))
				using (StreamWriter writer = new StreamWriter(fs))
				{
					writer.WriteLine("namespace NAF");
					writer.WriteLine("{");
					writer.WriteLine("\tpublic static class EditorIcons");
					writer.WriteLine("\t{");
					writer.WriteLine("\t\tpublic const string");
					foreach (var icon in icons)
						writer.WriteLine($"\t\t\t{icon.Key} = \"{icon.Value}\",");
					writer.BaseStream.Position -= 3; // Move back 3 characters
					writer.Write(';'); // Write the semicolon
					writer.WriteLine("\t}");
					writer.WriteLine("}");
				}
				Debug.Log($"'EditorIcons.cs' has been generated.");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static IEnumerable<string> EnumerateIcons(AssetBundle editorAssetBundle, string iconsPath)
		{
			foreach (var assetName in editorAssetBundle.GetAllAssetNames())
			{
				if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
					continue;
				if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false &&
					assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
					continue;

				yield return assetName;
			}
		}

		private static AssetBundle GetEditorAssetBundle()
		{
			var editorGUIUtility = typeof(EditorGUIUtility);
			var getEditorAssetBundle = editorGUIUtility.GetMethod(
				"GetEditorAssetBundle",
				BindingFlags.NonPublic | BindingFlags.Static)!;

			return (AssetBundle)getEditorAssetBundle.Invoke(null, new object[] { })!;
		}

		private static string GetIconsPath()
		{
#if UNITY_2018_3_OR_NEWER
			return UnityEditor.Experimental.EditorResources.iconsPath;
#else
			var assembly = typeof(EditorGUIUtility).Assembly;
			var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility")!;

			var iconsPathProperty = editorResourcesUtility.GetProperty(
				"iconsPath",
				BindingFlags.Static | BindingFlags.Public)!;

			return (string)iconsPathProperty.GetValue(null, new object[] { })!;
#endif
		}
	}
}