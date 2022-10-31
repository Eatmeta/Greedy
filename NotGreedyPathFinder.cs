using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Greedy.Architecture;
using Greedy.Architecture.Drawing;

namespace Greedy
{
    public class Tree : IEnumerable<Tree>
    {
        public Tree Head;
        public Point? From;
        public Point To;
        public List<Tree> Roots = new List<Tree>();
        public int TotalEnergy;
        public List<Point> Path = new List<Point>();
        public List<Point> VisitedChest;

        public Tree()
        {
            VisitedChest = new List<Point>();
        }

        public Tree(Tree tree, Point chest)
        {
            Head = tree;
            VisitedChest = new List<Point>(tree.VisitedChest) {chest};
        }

        public IEnumerator<Tree> GetEnumerator()
        {
            var stack = new Stack<Tree>();
            stack.Push(this);
            while (stack.Count != 0)
            {
                var current = stack.Pop();
                foreach (var currentRoot in current.Roots)
                {
                    stack.Push(currentRoot);
                    yield return currentRoot;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class NotGreedyPathFinder : IPathFinder
    {
        public List<Point> FindPathToCompleteGoal(State state)
        {
            var tree = new Tree {To = state.Position};
            var goodChests = FindAchievableChests(state);
            var p = new DijkstraPathFinder();
            var startPaths = new Dictionary<(Point from, Point to), PathWithCost>();
            var chestsPaths = new Dictionary<(Point from, Point to), PathWithCost>();
            var bestPath = new List<Point>();

            // добавляем пути и цену от старта до всех сундуков
            foreach (var chest in goodChests)
            {
                startPaths.Add((state.Position, chest),
                    p.GetPathsByDijkstra(state, state.Position, new[] {chest}).First());
            }

            // добавляем пути и цену от всех сундуков к другим сундукам
            for (var i = 0; i < goodChests.Count; i++)
            {
                for (var j = i + 1; j < goodChests.Count; j++)
                {
                    chestsPaths.Add((goodChests[i], goodChests[j]),
                        p.GetPathsByDijkstra(state, goodChests[i], new[] {goodChests[j]}).First());

                    chestsPaths.Add((goodChests[j], goodChests[i]),
                        p.GetPathsByDijkstra(state, goodChests[j], new[] {goodChests[i]}).First());
                }
            }
            
            // перебираем варианты от старта ко всем сундукам по очереди (если на них хватает сил, указанных в условии)
            foreach (var startPath in startPaths
                         .Where(w => w.Value.Cost <= state.Energy))
            {
                tree.Roots.Add(new Tree(tree, startPath.Key.to)
                {
                    From = startPath.Key.from,
                    To = startPath.Key.to,
                    TotalEnergy = startPath.Value.Cost,
                    Path = startPath.Value.Path
                });
            }

            // построим дерево всех возможных проходов по ящикам
            var stack = new Stack<Tree>();
            foreach (var treeRoot in tree.Roots)
            {
                stack.Push(treeRoot);
                while (stack.Count != 0)
                {
                    var currentRoot = stack.Pop();

                    var nextChests = chestsPaths
                        .Where(c =>
                            c.Key.from == currentRoot.To
                            && !currentRoot.VisitedChest.Contains(c.Key.to)
                            && currentRoot.TotalEnergy + c.Value.Cost <= state.Energy)
                        .ToList();
                    
                    foreach (var chest in nextChests)
                    {
                        var root = new Tree(currentRoot, chest.Key.to)
                        {
                            From = chest.Key.from,
                            To = chest.Key.to,
                            TotalEnergy = currentRoot.TotalEnergy + chest.Value.Cost,
                            Path = chest.Value.Path
                        };
                        
                        currentRoot.Roots.Add(root);
                        stack.Push(root);
                    }
                }
            }

            var bestTree = tree.OrderByDescending(t => t.VisitedChest.Count).FirstOrDefault();

            if (bestTree == null)
                return new List<Point>();

            while (bestTree.Head != null)
            {
                bestTree.Path.Reverse();

                bestPath.AddRange(bestPath.Count == 0 ? bestTree.Path : bestTree.Path.Skip(1));

                bestTree = bestTree.Head;
            }
            bestPath.Reverse();

            return bestPath.Skip(1).ToList();
        }

        private List<Point> FindAchievableChests(State state)
        {
            var result = new List<Point>();
            var d = new DijkstraPathFinder();
            foreach (var chest in state.Chests)
            {
                var pathWithCost = d.GetPathsByDijkstra(state, state.Position, new[] {chest}).FirstOrDefault();
                if (pathWithCost != null && pathWithCost.Path.Any())
                    result.Add(chest);
            }
            return result;
        }
    }
}