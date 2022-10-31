using System.Collections.Generic;
using System.Linq;
using Greedy.Architecture;
using System.Drawing;

namespace Greedy
{
    public class DijkstraData
    {
        public Point Previous { get; set; }
        public int Price { get; set; }
    }

    public class PointAndPrice
    {
        public Point Point { get; set; }
        public int Price { get; set; }
    }

    public class DijkstraPathFinder
    {
        public readonly Point[] Offsets = {new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1)};

        public HashSet<PointAndPrice> GetPossibleNextSteps(State state, HashSet<Point> visited, Point current)
        {
            var lastPoint = current;
            var nextSteps = Offsets
                .Where(offset => !visited.Contains(new Point(offset.X + lastPoint.X, offset.Y + lastPoint.Y))
                                 && state.InsideMap(offset + (Size) lastPoint)
                                 && state.CellCost[(offset + (Size) lastPoint).X, (offset + (Size) lastPoint).Y] != 0)
                .Select(point => new PointAndPrice
                {
                    Point = point + (Size) lastPoint,
                    Price = state.CellCost[(point + (Size) lastPoint).X, (point + (Size) lastPoint).Y]
                })
                .ToHashSet();
            return nextSteps;
        }

        public PathWithCost GetPathWithCost(Dictionary<Point, DijkstraData> dict, Point target, Point start)
        {
            var cost = dict[target].Price;
            var path = new List<Point> {target};
            var point = new Point(-int.MaxValue, -int.MaxValue);
            while (point != start)
            {
                point = dict[target].Previous;
                path.Add(point);
                target = point;
            }
            path.Reverse();
            return new PathWithCost(cost, path.ToArray());
        }

        public IEnumerable<PathWithCost> GetPathsByDijkstra(State state, Point start, IEnumerable<Point> targets)
        {
            var dict = new Dictionary<Point, DijkstraData> {[start] = new DijkstraData {Price = 0, Previous = start}};
            var visited = new HashSet<Point> {start};
            var listTargets = targets.ToList(); // список еще не найденных сундуков
            var toOpen = start; // вершина которую мы сейчас собираемся раскрывать

            while (true)
            {
                // если в toOpen находится сундук - лениво возвращаем его 
                if (listTargets.Contains(toOpen))
                {
                    yield return GetPathWithCost(dict, toOpen, start);
                    listTargets.Remove(toOpen);
                }

                if (listTargets.Count == 0) // если сундуков не осталось
                    yield break;

                // находим всех кандидатов для след шага кроме уже посещенных точек
                var listPossiblePoints = GetPossibleNextSteps(state, visited, toOpen);

                // если нет ни одного кандидата куда перейти, значит мы зашли в тупик => пытаемся
                // перейти на клетку из Dict с минимальной ценой, иначе нет пути
                if (listPossiblePoints.Count == 0)
                {
                    try
                    {
                        toOpen = dict
                            .Where(d => !visited.Contains(d.Key))
                            .OrderBy(d => d.Value.Price)
                            .First().Key;
                    }
                    catch
                    {
                        yield break;
                    }
                }

                foreach (var possiblePoint in listPossiblePoints.Where(possiblePoint =>
                             !dict.ContainsKey(possiblePoint.Point)))
                {
                    dict.Add(possiblePoint.Point, new DijkstraData {Price = int.MaxValue, Previous = start});
                }

                // если цена с учетом перехода меньше чем записанная для этой точки, то перезаписываем
                foreach (var possiblePoint in listPossiblePoints.Where(possiblePoint =>
                             possiblePoint.Price + dict[toOpen].Price < dict[possiblePoint.Point].Price))
                {
                    dict[possiblePoint.Point].Price = possiblePoint.Price + dict[toOpen].Price;
                    dict[possiblePoint.Point].Previous = toOpen;
                }

                visited.Add(toOpen); // добавляем старую точку в список посещенных

                // ищем новую самую дешевую точку для посещения из непосещенных, иначе нет пути
                try
                {
                    toOpen = dict
                        .Where(d => !visited.Contains(d.Key))
                        .OrderBy(d => d.Value.Price)
                        .First().Key;
                }
                catch
                {
                    yield break;
                }
            }
        }
    }
}