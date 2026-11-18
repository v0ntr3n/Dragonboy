using System;
using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class DijkstraPathFinder : IPathFinder
	{
		MapNext[] prev = Array.Empty<MapNext>();
		bool[] hasPrev = Array.Empty<bool>();
		bool[] visited = Array.Empty<bool>();
		int[] dist = Array.Empty<int>();

		public List<MapNext> FindWay(int start, int end, List<MapNext>[] graph)
		{
			if (graph == null || start < 0 || end < 0 || start >= graph.Length || end >= graph.Length)
			{
				return null;
			}

			if (start == end)
			{
				return new List<MapNext>();
			}

			int length = graph.Length;
			EnsureCapacity(length);

			for (int i = 0; i < length; i++)
			{
				prev[i] = default;
				hasPrev[i] = false;
				visited[i] = false;
				dist[i] = int.MaxValue;
			}

			dist[start] = 0;

			for (int _ = 0; _ < length; _++)
			{
				int cmap = -1;

				for (int i = 0; i < length; i++)
				{
					if (!visited[i] && (cmap == -1 || dist[i] < dist[cmap]))
					{
						cmap = i;
					}
				}

				if (cmap == -1 || dist[cmap] == int.MaxValue || cmap == end)
				{
					break;
				}

				List<MapNext> neighbors = graph[cmap];
				if (neighbors == null || neighbors.Count == 0)
				{
					visited[cmap] = true;
					continue;
				}

				foreach (MapNext mapNext in neighbors)
				{
					if (mapNext.To < 0 || mapNext.To >= length)
					{
						continue;
					}

					int cost = mapNext.IsNpcMenu(38) ? 100 : 1;
					int tentative = dist[cmap] + cost;
					if (tentative < dist[mapNext.To])
					{
						dist[mapNext.To] = tentative;
						prev[mapNext.To] = mapNext;
						hasPrev[mapNext.To] = true;
					}
				}

				visited[cmap] = true;
			}

			if (!hasPrev[end])
			{
				return null;
			}

			List<MapNext> way = new List<MapNext>();
			int index = end;
			while (index != start)
			{
				if (!hasPrev[index])
				{
					return null;
				}

				way.Add(prev[index]);
				index = prev[index].MapStart;
			}

			way.Reverse();
			return way;
		}

		void EnsureCapacity(int length)
		{
			if (dist.Length < length)
			{
				prev = new MapNext[length];
				hasPrev = new bool[length];
				visited = new bool[length];
				dist = new int[length];
			}
		}
	}

}
