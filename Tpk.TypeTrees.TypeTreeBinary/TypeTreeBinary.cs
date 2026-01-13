using System.Collections.Generic;
using System.IO;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

public class TypeTreeBinary
{
	public const ulong ExpectedMagic = 0x4545525445505954; // 'TYPETREE', little-endian
	public const uint ExpectedVersion = 1;

	public DumpedTypeTreeHeader Header { get; }
	public List<DumpedTypeTree> TypeTrees { get; }

	public TypeTreeBinary(BinaryReader reader)
	{
		Header = new DumpedTypeTreeHeader(reader);

		if (Header.Magic != ExpectedMagic)
		{
			throw new InvalidDataException($"Invalid magic number: {Header.Magic:X16}");
		}

		if (Header.Version != ExpectedVersion)
		{
			throw new InvalidDataException($"Unsupported version: {Header.Version}");
		}

		TypeTrees = new List<DumpedTypeTree>(checked((int)reader.ReadUInt32()));
		for (int i = 0; i < TypeTrees.Count; i++)
		{
			TypeTrees.Add(new DumpedTypeTree(reader));
		}
	}

	public static TypeTreeBinary FromFile(string filePath)
	{
		using var fs = File.OpenRead(filePath);
		using var reader = new BinaryReader(fs);
		return new TypeTreeBinary(reader);
	}
}
