using System.IO;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

public class DumpedTypeTreeRTTI(BinaryReader reader)
{
	public string ClassName { get; } = reader.ReadLengthPrefixedString();
	public string ClassNamespace { get; } = reader.ReadLengthPrefixedString();
	public string Module { get; } = reader.ReadLengthPrefixedString();
	public int PersistentTypeId { get; } = reader.ReadInt32();
	public int Size { get; } = reader.ReadInt32();

	public DumpedTypeTreeRTTIFlags Flags { get; } = (DumpedTypeTreeRTTIFlags)reader.ReadUInt32();

	public int BasePersistentTypeId { get; } = reader.ReadInt32();

	public uint DerivedFromTypeIndex { get; } = reader.ReadUInt32();
	public uint DerivedFromDescendantCount { get; } = reader.ReadUInt32();
}