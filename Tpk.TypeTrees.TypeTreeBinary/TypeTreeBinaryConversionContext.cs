using AssetRipper.Tpk.Shared;
using System.Collections.Generic;
using System.Linq;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

public class TypeTreeBinaryConversionContext
{
	private readonly TypeTreeBinary _binary;
	private readonly Dictionary<int, DumpedTypeTree> _typeTreesByTypeId;

	public TypeTreeBinaryConversionContext(TypeTreeBinary binary)
	{
		_binary = binary;
		_typeTreesByTypeId = _binary.TypeTrees.ToDictionary(x => x.RTTI.PersistentTypeId, x => x);
	}

	public TpkUnityClass Convert(DumpedTypeTree typeTree, TpkStringBuffer stringBuffer, TpkUnityNodeBuffer nodeBuffer)
	{
		var clazz = new TpkUnityClass();
		clazz.Name = stringBuffer.AddString(typeTree.RTTI.ClassName);

		var baseTypeName = "";
		if (typeTree.RTTI.BasePersistentTypeId != -1)
			baseTypeName = _typeTreesByTypeId[typeTree.RTTI.BasePersistentTypeId].RTTI.ClassName;

		clazz.Base = stringBuffer.AddString(baseTypeName);
		clazz.Flags = Convert(typeTree.RTTI.Flags);

		Merge(clazz, typeTree, stringBuffer, nodeBuffer);

		return clazz;
	}

	public static void Merge(TpkUnityClass existingClass, DumpedTypeTree typeTree, TpkStringBuffer stringBuffer,
		TpkUnityNodeBuffer nodeBuffer)
	{
		if (typeTree.Nodes.Count > 0)
		{
			var rootNode = Convert(typeTree.Nodes, stringBuffer, nodeBuffer);
			if (typeTree.IsReleaseTree)
			{
				existingClass.Flags |= TpkUnityClassFlags.HasReleaseRootNode;
				existingClass.ReleaseRootNode = rootNode;
			}
			else
			{
				existingClass.Flags |= TpkUnityClassFlags.HasEditorRootNode;
				existingClass.EditorRootNode = rootNode;
			}
		}
	}

	public static ushort Convert(List<DumpedTypeTreeNode> nodes, TpkStringBuffer stringBuffer, TpkUnityNodeBuffer nodeBuffer)
	{
		var processingQueue = new Queue<DumpedTypeTreeNode>(nodes);
		var currentStack = new Stack<(TpkUnityNode, List<ushort>)>(nodes.Count);

		ushort rootNodeIndex = 0;

		while (processingQueue.Count > 0)
		{
			var currentNode = processingQueue.Dequeue();

			ProcessStack(currentNode.Level);

			var tpkNode = new TpkUnityNode();
			tpkNode.Name = stringBuffer.AddString(currentNode.Name);
			tpkNode.TypeName = stringBuffer.AddString(currentNode.Type);
			tpkNode.ByteSize = currentNode.ByteSize;
			tpkNode.Version = currentNode.Version;
			tpkNode.TypeFlags = (byte)currentNode.Flags;
			tpkNode.MetaFlag = (uint)currentNode.MetaFlags;

			currentStack.Push((tpkNode, []));
		}

		ProcessStack(0);

		return rootNodeIndex;

		void ProcessStack(int targetDepth)
		{
			while (currentStack.Count != targetDepth)
			{
				var (completedNode, subNodeIndices) = currentStack.Pop();
				completedNode.SubNodes = subNodeIndices.ToArray();

				var completedNodeIndex = nodeBuffer.AddNode(completedNode);
				if (currentStack.TryPeek(out var parentNode))
				{
					parentNode.Item2.Add(completedNodeIndex);
				}
				else
				{
					rootNodeIndex = completedNodeIndex;
				}
			}
		}
	}

	private static TpkUnityClassFlags Convert(DumpedTypeTreeRTTIFlags flags)
	{
		var result = TpkUnityClassFlags.None;
		if (flags.HasFlag(DumpedTypeTreeRTTIFlags.IsAbstract))
			result |= TpkUnityClassFlags.IsAbstract;

		if (flags.HasFlag(DumpedTypeTreeRTTIFlags.IsSealed))
			result |= TpkUnityClassFlags.IsSealed;

		if (flags.HasFlag(DumpedTypeTreeRTTIFlags.IsEditorOnly))
			result |= TpkUnityClassFlags.IsEditorOnly;

		if (flags.HasFlag(DumpedTypeTreeRTTIFlags.IsStripped))
			result |= TpkUnityClassFlags.IsStripped;

		// TPK does not have a flag for IsDeprecated

		return result;
	}
}