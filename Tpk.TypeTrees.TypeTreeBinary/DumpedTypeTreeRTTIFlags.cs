using System;

namespace AssetRipper.Tpk.TypeTrees.TypeTreeBinary;

[Flags]
public enum DumpedTypeTreeRTTIFlags : uint
{
	IsAbstract = 1 << 0,
	IsSealed = 1 << 1,
	IsEditorOnly = 1 << 2,
	IsStripped = 1 << 3,
	IsDeprecated = 1 << 4
}