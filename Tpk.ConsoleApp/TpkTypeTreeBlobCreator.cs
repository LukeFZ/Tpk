using AssetRipper.Primitives;
using AssetRipper.Tpk.TypeTrees;
using AssetRipper.Tpk.TypeTrees.Json;
using System.Collections.Generic;
using System.Linq;
using AssetRipper.Tpk.TypeTrees.TypeTreeBinary;
using VersionClassPair = System.Collections.Generic.KeyValuePair<
	AssetRipper.Primitives.UnityVersion,
	AssetRipper.Tpk.TypeTrees.TpkUnityClass?>;

namespace AssetRipper.Tpk.ConsoleApp
{
	internal static class TpkTypeTreeBlobCreator
	{
		public static TpkTypeTreeBlob CreateFromPath(string path, bool isZipFile, bool isBinary)
			=> isZipFile 
				? CreateFromZipFile(path, isBinary) 
				: CreateFromDirectory(path, isBinary);

		public static TpkTypeTreeBlob CreateFromDirectory(string directoryPath, bool isBinary)
			=> Create(isBinary
				? FileSorter.GetOrderedBinaryFilePaths(directoryPath)
				: FileSorter.GetOrderedJsonFilePaths(directoryPath), isBinary);

		public static TpkTypeTreeBlob CreateFromZipFile(string zipFilePath, bool isBinary) =>
			isBinary 
				? Create(ZipFileReader.ReadTypeTreeBinaryFromZipFile(zipFilePath)) 
				: Create(ZipFileReader.ReadUnityInfoFromZipFile(zipFilePath));

		private static TpkTypeTreeBlob Create(IEnumerable<string> pathsOrderedByUnityVersion, bool isBinary)
		{
			if (isBinary)
			{
				return Create(pathsOrderedByUnityVersion.Select(x =>
					(UnityVersion.Parse(Directory.GetParent(x)!.Name), TypeTreeBinary.FromFile(x))));
			}

			return Create(pathsOrderedByUnityVersion.Select(UnityInfo.ReadFromJsonFile));
		}

		private static TpkTypeTreeBlob Create(IEnumerable<UnityInfo> infosOrderedByUnityVersion)
		{
			TpkTypeTreeBlob blob = new TpkTypeTreeBlob();
			blob.CommonString.Add(UnityVersion.MinVersion, 0);

			byte latestCommonStringCount = 0;
			List<string> commonStrings = new List<string>();
			Dictionary<int, string> latestUnityClassesDumped = new Dictionary<int, string>();
			Dictionary<int, TpkClassInformation> classDictionary = new Dictionary<int, TpkClassInformation>();

			foreach (UnityInfo info in infosOrderedByUnityVersion)
			{
				Console.WriteLine(info.Version);
				UnityVersion version = UnityVersion.Parse(info.Version);
				blob.Versions.Add(version);

				if (info.Strings.Count != latestCommonStringCount)
				{
					latestCommonStringCount = (byte)info.Strings.Count;
					blob.CommonString.Add(version, latestCommonStringCount);
				}

				for (int i = 0; i < info.Strings.Count; i++)
				{
					if (i < commonStrings.Count)
					{
						if (info.Strings[i].String != commonStrings[i])
						{
							throw new Exception($"String inequality at index {i} for version {version}");
						}
					}
					else
					{
						commonStrings.Add(info.Strings[i].String);
					}
				}

				foreach (UnityClass unityClass in info.Classes)
				{
					string dump = unityClass.ToJsonString();
					if (!latestUnityClassesDumped.TryGetValue(unityClass.TypeID, out string? cachedDump) || cachedDump != dump)
					{
						latestUnityClassesDumped[unityClass.TypeID] = dump;
						if (!classDictionary.TryGetValue(unityClass.TypeID, out TpkClassInformation? tpkClassInformation))
						{
							tpkClassInformation = new TpkClassInformation(unityClass.TypeID);
							classDictionary.Add(unityClass.TypeID, tpkClassInformation);
						}
						TpkUnityClass tpkUnityClass = ClassConversion.Convert(unityClass, blob.StringBuffer, blob.NodeBuffer);
						tpkClassInformation.Classes.Add(new VersionClassPair(version, tpkUnityClass));
					}
				}

				List<int> typeIds = info.Classes.Select(c => c.TypeID).ToList();
				foreach (int unusedId in classDictionary.Keys.Where(id => !typeIds.Contains(id)))
				{
					if (!string.IsNullOrEmpty(latestUnityClassesDumped[unusedId]))
					{
						latestUnityClassesDumped[unusedId] = "";
						classDictionary[unusedId].Classes.Add(new VersionClassPair(version, null));
					}
				}
			}

			blob.CommonString.SetIndices(blob.StringBuffer, commonStrings);
			PostProcessCreatedBlob(blob, classDictionary);

			return blob;
		}

		private static TpkTypeTreeBlob Create(IEnumerable<(UnityVersion, TypeTreeBinary)> typeTreeBinariesOrderedByUnityVersion)
		{
			var blob = new TpkTypeTreeBlob();
			blob.CommonString.Add(UnityVersion.MinVersion, 0);

			var latestUnityClassesDumped = new Dictionary<int, int>();
			var classDictionary = new Dictionary<int, TpkClassInformation>();

			var versionClasses = new Dictionary<UnityVersion, Dictionary<int, TpkUnityClass>>();

			foreach (var (fullVersion, typeTreeBinary) in typeTreeBinariesOrderedByUnityVersion)
			{
				if (!versionClasses.TryGetValue(fullVersion, out var classesByTypeId))
				{
					versionClasses[fullVersion] = classesByTypeId = new Dictionary<int, TpkUnityClass>();
				}

				Console.WriteLine(fullVersion);
				blob.Versions.Add(fullVersion);

				var conversionContext = new TypeTreeBinaryConversionContext(typeTreeBinary);
				foreach (var typeTree in typeTreeBinary.TypeTrees)
				{
					var currentTypeTreeValueHash = typeTree.GetValueHash();
					if (!latestUnityClassesDumped.TryGetValue(typeTree.RTTI.PersistentTypeId, out var cachedHashCode) 
					    || cachedHashCode != currentTypeTreeValueHash)
					{
						latestUnityClassesDumped[typeTree.RTTI.PersistentTypeId] = currentTypeTreeValueHash;
						if (!classDictionary.TryGetValue(typeTree.RTTI.PersistentTypeId, out var tpkClassInformation))
						{
							tpkClassInformation = new TpkClassInformation(typeTree.RTTI.PersistentTypeId);
							classDictionary.Add(typeTree.RTTI.PersistentTypeId, tpkClassInformation);
						}

						if (classesByTypeId.TryGetValue(typeTree.RTTI.PersistentTypeId, out var tpkUnityClass))
						{
							TypeTreeBinaryConversionContext.Merge(tpkUnityClass, typeTree, blob.StringBuffer, blob.NodeBuffer);
						}
						else
						{
							tpkUnityClass = conversionContext.Convert(typeTree, blob.StringBuffer, blob.NodeBuffer);
							classesByTypeId[typeTree.RTTI.PersistentTypeId] = tpkUnityClass;
						}
						
						tpkClassInformation.Classes.Add(new VersionClassPair(fullVersion, tpkUnityClass));
					}
				}

				var typeIds = typeTreeBinary.TypeTrees.Select(c => c.RTTI.PersistentTypeId).ToList();
				foreach (var unusedId in classDictionary.Keys.Where(id => !typeIds.Contains(id)))
				{
					if (latestUnityClassesDumped.Remove(unusedId))
					{
						classDictionary[unusedId].Classes.Add(new VersionClassPair(fullVersion, null));
					}
				}
			}

			PostProcessCreatedBlob(blob, classDictionary);

			return blob;
		}

		private static void PostProcessCreatedBlob(TpkTypeTreeBlob blob,
			Dictionary<int, TpkClassInformation> classDictionary)
		{
			foreach (TpkClassInformation tpkClassInfo in classDictionary.Values)
			{
				VersionClassPair[] pairs = tpkClassInfo.Classes.ToArray();
				TpkUnityClass? previousClass = pairs[0].Value;
				for (int i = 1; i < pairs.Length; i++)
				{
					VersionClassPair pair = pairs[i];
					if (pair.Value == previousClass)
					{
						tpkClassInfo.Classes.Remove(pair);
					}
					else
					{
						previousClass = pair.Value;
					}
				}
			}

			blob.ClassInformation.AddRange(classDictionary.Values);

			//About 21k / 65k
			Console.WriteLine($"Node buffer has {blob.NodeBuffer.Count} entries, which is {GetUShortPercent(blob.NodeBuffer.Count)}% of its maximum {ushort.MaxValue} entries");
			//About 7k / 65k
			Console.WriteLine($"String buffer has {blob.StringBuffer.Count} entries, which is {GetUShortPercent(blob.StringBuffer.Count)}% of its maximum {ushort.MaxValue} entries");

			blob.CreationTime = DateTime.Now.ToUniversalTime();
		}

		private static int GetUShortPercent(int value) => value * 100 / ushort.MaxValue;
	}
}
