using AssetRipper.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace AssetRipper.Tpk.ConsoleApp;

internal static class FileSorter
{
	/// <summary>
	/// Get the paths to the jsons in order
	/// </summary>
	/// <param name="directoryPath">The directory containing the struct dumps</param>
	/// <returns>An array of absolute file paths ordered by Unity version</returns>
	/// <exception cref="ArgumentException"></exception>
	public static IEnumerable<string> GetOrderedJsonFilePaths(string directoryPath)
	{
		DirectoryInfo directory = new DirectoryInfo(directoryPath);
		if (!directory.Exists)
		{
			throw new ArgumentException(null, nameof(directoryPath));
		}

		Dictionary<UnityVersion, string> files = new Dictionary<UnityVersion, string>();
		foreach (FileInfo file in directory.GetFiles())
		{
			if (file.Extension == JsonExtension)
			{
				string fileNameWithoutExtension = file.Name.Substring(0, file.Name.Length - JsonExtension.Length);
				UnityVersion version = UnityVersion.Parse(fileNameWithoutExtension);
				files.Add(version, file.FullName);
			}
		}
		List<UnityVersion> orderedVersions = files.Select(pair => pair.Key).ToList();
		orderedVersions.Sort();
		return orderedVersions.Select(version => files[version]);
	}

	/// <summary>
	/// Get the paths to the type tree binaries in order
	/// </summary>
	/// <param name="directoryPath">The directory containing the struct dump directories</param>
	/// <returns>An array of absolute file paths ordered by Unity version</returns>
	/// <exception cref="ArgumentException"></exception>
	public static IEnumerable<string> GetOrderedBinaryFilePaths(string directoryPath)
	{
		var info = new DirectoryInfo(directoryPath);
		if (!info.Exists)
		{
			throw new ArgumentException(null, nameof(directoryPath));
		}

		var files = new Dictionary<UnityVersion, List<string>>();
		foreach (var directory in info.GetDirectories())
		{
			UnityVersion version = default;

			foreach (var file in directory.GetFiles())
			{
				if (file.Extension == BinaryExtension)
				{
					if (version == default)
					{
						version = UnityVersion.Parse(directory.Name);
					}

					if (files.TryGetValue(version, out var fileList))
					{
						fileList.Add(file.FullName);
					}
					else
					{
						files[version] = [file.FullName];
					}
				}
			}
		}

		var orderedVersions = files.Select(pair => pair.Key).ToList();
		orderedVersions.Sort();

		return orderedVersions.SelectMany(version => files[version]);
	}

	private const string JsonExtension = ".json";
	private const string BinaryExtension = ".ttbin";
}