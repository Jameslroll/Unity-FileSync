using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class FileSync
{
	/* Constants */
	private const bool autoSyncEnabled = true;

	private static readonly SyncPath[] syncs = new SyncPath[]
		{
			new SyncPath()
			{
				input = "C:/Users/<User>/Pictures",
				output = "/Textures/",
			},

			new SyncPath()
			{
				input = "C:/Users/<User>/Documents/Blender",
				output = "/Models/",
			},
		};

	private static readonly string[] exclusions = new string[]
		{
			"blend",
			"max",
			"psd",
			"pdd",
			"raw",
			"tmp",
		};

	/* Structures: Private */
	private struct SyncPath
	{
		public string input;
		public string output;
	}

	/* Fields: Private */
	private static double lastAutoSync;
	private static string[] cachedHashes;
	
	/* Methods: Private */
	[MenuItem("Sync/Force Sync")]
	private static void Sync()
	{
		foreach (SyncPath sync in syncs)
		{
			if (string.IsNullOrEmpty(sync.input))
			{
				continue;
			}

			List<string> files = Directory.GetFiles(sync.input, "*.*", SearchOption.AllDirectories).Where(predicate => IsPathValid(predicate, sync.input.Length + 1)).OrderBy(predicate => predicate).ToList();
			
			foreach (string file in files)
			{
				string relativePath = file.Substring(sync.input.Length + 1);
				string output = Application.dataPath + sync.output + relativePath;
				string outputDirectory = output.Substring(0, Math.Max(output.LastIndexOf('/'), output.LastIndexOf('\\')));
				
				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}
				
				File.Copy(file, output, true);
			}
		}

		AssetDatabase.Refresh();
	}
	
	[InitializeOnLoadMethod]
	private static void Initialize()
	{
		if (autoSyncEnabled)
		{
			cachedHashes = new string[syncs.Length];

			EditorApplication.update += AutoSync;
		}
	}

	private static void AutoSync()
	{
		if (Application.isPlaying) { return; }
		if (EditorApplication.timeSinceStartup - lastAutoSync <= 1f) { return; }
		
		lastAutoSync = EditorApplication.timeSinceStartup;
		
		bool shouldSync = false;

		for (int pathIndex = 0; pathIndex < cachedHashes.Length; pathIndex++)
		{
			string hash = GetHash(syncs[pathIndex].input);

			if (cachedHashes[pathIndex] !=  hash)
			{
				shouldSync = true;
			}

			cachedHashes[pathIndex] = hash;
		}

		if (shouldSync)
		{
			Sync();

			Debug.Log("Auto-Synced");
		}
	}

	private static string GetHash(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return "";
		}

		List<string> files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(predicate => IsPathValid(predicate, path.Length + 1)).OrderBy(predicate => predicate).ToList();

		MD5 md5 = MD5.Create();

		for(int i = 0; i < files.Count; i++)
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

	private static bool IsPathValid(string path, int directoryLength = 0)
	{
		string extension = path.Substring(path.LastIndexOf('.') + 1);

		if (exclusions.Contains(extension))
		{
			return false;
		}

		string relativePath = path.Substring(directoryLength);
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
}
