using System.IO;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

public class DumpedTypeTreeNode(BinaryReader reader)
{
	public string Type { get; } = reader.ReadLengthPrefixedString();
	public string Name { get; } = reader.ReadLengthPrefixedString();

	public DumpedTypeTreeNodeFlags Flags { get; } = (DumpedTypeTreeNodeFlags)reader.ReadUInt32();
	public int ByteSize { get; } = reader.ReadInt32();

	public short Version { get; } = reader.ReadInt16();
	public byte Level { get; } = reader.ReadByte();

	public DumpedTypeTreeNodeMetaFlags MetaFlags { get; } = (DumpedTypeTreeNodeMetaFlags)reader.ReadUInt32();
	public ulong RefTypeHash { get; } = reader.ReadUInt64();
}