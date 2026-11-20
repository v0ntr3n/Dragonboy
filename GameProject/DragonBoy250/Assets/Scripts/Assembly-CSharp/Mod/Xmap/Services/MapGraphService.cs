using System;
using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class MapGraphService
	{
		internal List<MapNext>[] CloneGraph(List<MapNext>[] source)
		{
			List<MapNext>[] clone = new List<MapNext>[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				clone[i] = source[i] != null ? new List<MapNext>(source[i]) : new List<MapNext>();
			}

			return clone;
		}

		internal List<MapNext>[] CloneGraphForStartMap(List<MapNext>[] source, int startMapId)
		{
			if (source == null)
			{
				return null;
			}

			List<MapNext>[] clone = new List<MapNext>[source.Length];
			Array.Copy(source, clone, source.Length);
			if (startMapId >= 0 && startMapId < source.Length)
			{
				clone[startMapId] = source[startMapId] != null
					? new List<MapNext>(source[startMapId])
					: new List<MapNext>();
			}

			return clone;
		}

		internal void AddCapsuleLink(List<MapNext>[] graph, int mapStart, int destinationMapId, int select)
		{
			if (graph == null || mapStart < 0 || mapStart >= graph.Length)
			{
				return;
			}

			List<MapNext> links = graph[mapStart];
			if (links != null)
			{
				for (int i = 0; i < links.Count; i++)
				{
					MapNext existing = links[i];
					if (existing.IsCapsuleSelection(select) && existing.To == destinationMapId)
					{
						return;
					}
				}
			}

			if (links == null)
			{
				links = new List<MapNext>();
				graph[mapStart] = links;
			}
			else if (mapStart < XmapData.links.Length && ReferenceEquals(links, XmapData.links[mapStart]))
			{
				links = new List<MapNext>(links);
				graph[mapStart] = links;
			}

			links.Add(new MapNext(mapStart, destinationMapId, TypeMapNext.Capsule, new[]
			{
				select
			}));
		}
	}

}
