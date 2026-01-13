using System.Collections.Generic;
using System.IO;
using AssetRipper.Tpk.Shared;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

public class DumpedTypeTree
{
	public DumpedTypeTreeRTTI RTTI { get; }
	public TransferInstructionFlags TransferFlags { get; }
	public List<DumpedTypeTreeNode> Nodes { get; }

	public bool IsReleaseTree => TransferFlags.HasFlag(TransferInstructionFlags.SerializeGameRelease);

	public DumpedTypeTree(BinaryReader reader)
	{
		RTTI = new DumpedTypeTreeRTTI(reader);
		TransferFlags = (TransferInstructionFlags)reader.ReadUInt64();

		var count = reader.ReadUInt32();
		Nodes = new List<DumpedTypeTreeNode>(checked((int)count));
		for (int i = 0; i < count; i++)
		{
			Nodes.Add(new DumpedTypeTreeNode(reader));
		}
	}
}