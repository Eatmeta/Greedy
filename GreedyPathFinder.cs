using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Greedy.Architecture;
using Greedy.Architecture.Drawing;

namespace Greedy
{
    public class GreedyPathFinder : IPathFinder
    {
        public List<Point> FindPathToCompleteGoal(State state)
        {
            var pathFinder = new DijkstraPathFinder();
            var listOfList = new List<List<Point>>();
            var start = state.Position;
            var listChests = state.Chests;
            var spentEnergy = 0;

            if (listChests.Count < state.Goal)	// если сундуков на карте меньше, чем требуется вернуть по условию
                return new List<Point>();
				
            while (listOfList.Count != state.Goal)
            {
                var pathWithCost = pathFinder.GetPathsByDijkstra(state, start, listChests).FirstOrDefault();
				
                if (pathWithCost == null)		// если получили null
                    return new List<Point>();
				
                spentEnergy += pathWithCost.Cost;
				
                if (spentEnergy > state.Energy)	// если энергии требуется больше, чем есть по условию
                    return new List<Point>();
				
                pathWithCost.Path.RemoveAt(0);	// удаляем точку старта, т.к. по условию она нам не нужна 
                start = pathWithCost.Path.LastOrDefault();
                listOfList.Add(pathWithCost.Path);
                listChests.Remove(start);
            }
            return listOfList.SelectMany(x => x).ToList();
        }
    }
}