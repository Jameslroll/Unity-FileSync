using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class FileSync
{
	#region Private Structs

	private struct SyncPath
	{
		#region Public Fields

		public string input;
		public string output;

		#endregion
	}

	#endregion

	#region Private Fields

	private const bool autoSyncEnabled = true; // Enable auto-sync?

	private static readonly SyncPath[] syncs = new SyncPath[]
	{
		// Input: absolute path, relative to Application.dataPath when starting with a forward-slash.
		// Output: always relative to Application.dataPath.

		new SyncPath()
		{
			input = "/.Pictures/",
			output = "/Textures/"
		},
		
		// Examples.
		/*
		new SyncPath()
		{
			input = "C:/Users/<User>/Pictures/",
			output = "/Textures/",
		},

		new SyncPath()
		{
			input = "C:/Users/<User>/Documents/Blender/",
			output = "/Models/"
		},

		new SyncPath()
		{
			input = "/.Models/",
			output = "/Models/"
		},
		*/
	};

	// Excluded files to import using regular expressions.
	private static readonly string[] exclusions = new string[]
	{
		@".\.blend.*$",
		@".\.max$",
		@".\.psd$",
		@".\.pdd$",
		@".\.raw$",
		@".\.tmp$",
		@".\.xcf$",
		@".\.afphoto$",
		@".\/\.",
	};

	private static int dataPathLength;
	private static double lastAutoSync;
	private static string[] cachedHashes;

	#endregion

	#region Private Methods

	[MenuItem("Sync/Force Sync")]
	private static int Sync()
	{
		int updatedFiles = 0;

		foreach (SyncPath sync in syncs)
		{
			string input = GetPath(sync.input);
			
			if (string.IsNullOrEmpty(input))
			{
				continue;
			}

			List<string> files = Directory.GetFiles(input, "*.*", SearchOption.AllDirectories).Where(predicate => IsPathValid(predicate)).OrderBy(predicate => predicate).ToList();

			for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
			{
				string filePath = files[fileIndex].Replace('\\', '/');
				string file = filePath.Substring(input.Length);
				string output = Application.dataPath + sync.output + file;
				string outputDirectory = output.Substring(0, Math.Max(output.LastIndexOf('/'), output.LastIndexOf('\\')));

				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}

				File.Copy(filePath, output, true);
				updatedFiles++;
			}
		}

		AssetDatabase.Refresh();

		return updatedFiles;
	}

	[InitializeOnLoadMethod]
	private static void Initialize()
	{
		if (autoSyncEnabled)
		{
			dataPathLength = Application.dataPath.Length;
			cachedHashes = new string[syncs.Length];

			EditorApplication.update += AutoSync;
		}
	}

	private static void AutoSync()
	{
		if (Application.isPlaying) { return; }
		if (EditorApplication.timeSinceStartup - lastAutoSync <= 5f) { return; }

		lastAutoSync = EditorApplication.timeSinceStartup;

		bool shouldSync = false;

		for (int pathIndex = 0; pathIndex < cachedHashes.Length; pathIndex++)
		{
			string hash = GetHash(syncs[pathIndex].input);

			if (cachedHashes[pathIndex] != hash)
			{
				shouldSync = true;
			}

			cachedHashes[pathIndex] = hash;
		}

		if (shouldSync)
		{
			Debug.Log($"Synchronized {Sync()} files!");
		}
	}

	private static string GetHash(string path)
	{
		path = GetPath(path);

		if (string.IsNullOrEmpty(path))
		{
			return "";
		}

		List<string> files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(predicate => IsPathValid(predicate)).OrderBy(predicate => predicate).ToList();

		if (files.Count == 0)
		{
			return "";
		}

		MD5 md5 = MD5.Create();

		for (int i = 0; i < files.Count; i++)
		{
			string file = files[i];

			string relativePath = file.Substring(path.Length + 1);
			byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
			byte[] contentBytes = File.ReadAllBytes(file);

			md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

			if (i == files.Count - 1)
			{
				md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
			}
			else
			{
				md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
			}
		}

		return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
	}

	private static string GetPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}

		if (path[0] == '/')
		{
			return Application.dataPath + path;
		}

		return path;
	}

	private static bool IsPathValid(string path)
	{
		path = path.Replace('\\', '/');
		string relativePath = path.Substring(dataPathLength);

		if (Array.Exists(exclusions, exclusion => Regex.IsMatch(relativePath, exclusion)))
		{
			return false;
		}

		char[] relativeChars = relativePath.ToCharArray();
		char lastChar = '/';

		foreach (char @char in relativeChars)
		{
			if ((lastChar.Equals('/') || lastChar.Equals('\\')) && @char.Equals('_'))
			{
				return false;
			}

			lastChar = @char;
		}

		return true;
	}

	#endregion
}