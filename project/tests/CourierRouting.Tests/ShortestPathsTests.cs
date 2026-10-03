using CourierRouting.Graph;

namespace CourierRouting.Tests;

public class ShortestPathsTests
{
    [Fact]
    public void FindsShorterPathThroughMoreRoads()
    {
        // Прямая дорога A–C (10) длиннее, чем A–B–C (3 + 4).
        var net = TestData.Network(("A", "B", 3), ("B", "C", 4), ("A", "C", 10));

        var tree = ShortestPaths.Dijkstra(net, net.IndexOf("A"));

        Assert.Equal(7, tree.DistanceTo(net.IndexOf("C")));
        Assert.Equal(["A", "B", "C"], tree.PathTo(net.IndexOf("C")).Select(net.Name));
    }

    [Fact]
    public void SourceIsAtDistanceZero()
    {
        var net = TestData.Network(("A", "B", 5));
        var tree = ShortestPaths.Dijkstra(net, net.IndexOf("A"));

        Assert.Equal(0, tree.DistanceTo(net.IndexOf("A")));
        Assert.Equal(["A"], tree.PathTo(net.IndexOf("A")).Select(net.Name));
    }

    [Fact]
    public void IsolatedNodeIsUnreachable()
    {
        var net = TestData.Network(("A", "B", 5));
        int island = net.AddNode("Остров");

        var tree = ShortestPaths.Dijkstra(net, net.IndexOf("A"));

        Assert.False(tree.CanReach(island));
        Assert.Equal(ShortestPathTree.Unreachable, tree.DistanceTo(island));
        Assert.Empty(tree.PathTo(island));
    }

    [Fact]
    public void OneWayRoadWorksInOneDirectionOnly()
    {
        var net = new RoadNetwork();
        net.AddRoad("A", "B", 5, twoWay: false);

        Assert.True(ShortestPaths.Dijkstra(net, net.IndexOf("A")).CanReach(net.IndexOf("B")));
        Assert.False(ShortestPaths.Dijkstra(net, net.IndexOf("B")).CanReach(net.IndexOf("A")));
    }

    [Fact]
    public void ZeroLengthRoadsAndCyclesAreHandled()
    {
        var net = TestData.Network(("A", "B", 0), ("B", "C", 0), ("C", "A", 0), ("C", "D", 2));
        Assert.Equal(2, ShortestPaths.Dijkstra(net, net.IndexOf("A")).DistanceTo(net.IndexOf("D")));
    }

    [Fact]
    public void NegativeRoadIsRejected()
    {
        var net = new RoadNetwork();
        Assert.Throws<ArgumentOutOfRangeException>(() => net.AddRoad("A", "B", -1));
    }

    [Fact]
    public void MatchesFloydWarshallOnRandomGraphs()
    {
        var random = new Random(1);
        for (int t = 0; t < 300; t++)
        {
            var problem = TestData.RandomProblem(random, maxNodes: 12, maxOrders: 0);
            var net = problem.Network;
            var reference = Reference.FloydWarshall(net);

            for (int s = 0; s < net.NodeCount; s++)
            {
                var tree = ShortestPaths.Dijkstra(net, s);
                for (int v = 0; v < net.NodeCount; v++)
                {
                    long expected = reference[s, v] >= Reference.Inf ? ShortestPathTree.Unreachable : reference[s, v];
                    Assert.Equal(expected, tree.DistanceTo(v));
                }
            }
        }
    }
}
