using AtCoder;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Console;
using static Program;
using System.Collections.ObjectModel;
using System.Security;
using System.Globalization;
using System.Data.Common;

public static class Extensions
{
  private static Random r = new Random();

  [MethodImpl(256)]
  public static void Shuffle<T>(this IList<T> v)
  {
    for (int i = v.Count - 1; i > 0; i--)
    {
      int j = r.Next(0, i + 1);
      var tmp = v[i];
      v[i] = v[j];
      v[j] = tmp;
    }
  }
}

public static class TimeKeeper
{
  private static Stopwatch _stopwatch = new Stopwatch();
  private static double _frequency = Stopwatch.Frequency;

  [MethodImpl(256)] public static void Start() => _stopwatch.Start();
  [MethodImpl(256)] public static void Stop() => _stopwatch.Stop();
  [MethodImpl(256)] public static void Reset() => _stopwatch.Reset();
  [MethodImpl(256)] public static long ElapsedMillisec() => _stopwatch.ElapsedMilliseconds;
  [MethodImpl(256)] public static long ElapsedMicrosec() => (long)(_stopwatch.ElapsedTicks / _frequency * 1_000_000);
}

/// <summary>
/// データ型 T に対するモノイド演算を定義するインターフェース。
/// </summary>
/// <typeparam name="T">操作対象のデータの型。</typeparam>
public interface IMonoidOperator<T>
{
  /// <summary>
  /// モノイドの単位元。
  /// 任意のxに対し Operate(x, Identity) = x および Operate(Identity, x) = x を満たします。
  /// </summary>
  T Identity { get; }

  /// <summary>
  /// モノイドの二項演算。
  /// 結合法則 Operate(a, Operate(b, c)) = Operate(Operate(a, b), c) を満たす必要があります。
  /// </summary>
  /// <param name="a">一つ目の値。</param>
  /// <param name="b">二つ目の値。</param>
  /// <returns>aとbを演算した結果。</returns>
  T Operate(T a, T b);
}

/// <summary>
/// 動的グラフの連結性を管理するクラス（削除可能なUnion-Findデータ構造）。
/// Euler Tour Treeを階層的に利用して、辺の追加と削除を効率的に処理します。
/// 連結成分ごとの集約値や、個々の頂点の値も管理できます。
/// </summary>
/// <typeparam name="T">各頂点に関連付ける値の型。IMonoidOperator&lt;T&gt;によって集約操作が定義される必要があります。</typeparam>
public class DynamicConnectivity<T>
{
  private readonly IMonoidOperator<T> _monoidOperator;
  private readonly List<EulerTourTree> _eulerTourTrees;
  private readonly List<List<HashSet<int>>> _nonTreeEdgesByLevel;
  private readonly int _vertexCount;
  private int _levelCount = 1; // 利用しているETTのレベル数 (1から始まる)

  /// <summary>
  /// Euler Tour Tree (ETT) を表す内部クラス。
  /// 各階層レベルにおけるグラフの森の構造と、関連する集約値を管理します。
  /// </summary>
  private class EulerTourTree
  {
    private readonly IMonoidOperator<T> _monoidOpETT;
    private List<Dictionary<int, Node>> _vertexPairToNodeMap; // (endpoint1, endpoint2) -> Node のマッピング

    /// <summary>
    /// Euler Tour Treeのノードを表す内部クラス。
    /// Splay Treeのノードとして機能し、頂点または辺（オイラーツアーの弧）を表します。
    /// </summary>
    internal class Node
    {
      /// <summary>
      /// 子ノードの配列。[0]が左の子、[1]が右の子。
      /// </summary>
      public Node?[] Children { get; } = new Node?[2];
      /// <summary>
      /// 親ノード。根の場合はnull。
      /// </summary>
      public Node? Parent { get; set; }
      /// <summary>
      /// このノードが表す区間または辺の始点、あるいは頂点のID。
      /// </summary>
      public int Endpoint1 { get; }
      /// <summary>
      /// このノードが表す区間または辺の終点、あるいは頂点のID。頂点ノードの場合はEndpoint1と同じ。
      /// </summary>
      public int Endpoint2 { get; }
      /// <summary>
      /// このノードを根とするSplay Treeの部分木に含まれる「頂点ノード」の数。
      /// </summary>
      public int SubtreeSize { get; set; }
      /// <summary>
      /// このノードが頂点を表す場合、その頂点に直接関連付けられた値。
      /// </summary>
      public T Value { get; set; }
      /// <summary>
      /// このノードを根とするSplay Treeの部分木に含まれる全頂点ノードのValueの集約値。
      /// </summary>
      public T AggregatedValue { get; set; }
      /// <summary>
      /// このノードが辺（Endpoint1 < Endpoint2）を表し、かつまだ処理されていない（上位レベルに昇格する可能性がある）場合にtrue。
      /// </summary>
      public bool IsExactEdge { get; set; }
      /// <summary>
      /// このノードを根とする部分木内に、IsExactEdgeがtrueのノードが存在する場合にtrue。
      /// </summary>
      public bool HasExactEdgeInChildren { get; set; }
      /// <summary>
      /// このノードが頂点を表す場合、現在のETTレベルで非ツリーエッジに接続されている場合にtrue。
      /// </summary>
      public bool IsNonTreeEdgeConnected { get; set; }
      /// <summary>
      /// このノードを根とする部分木内に、IsNonTreeEdgeConnectedがtrueの頂点ノードが存在する場合にtrue。
      /// </summary>
      public bool HasNonTreeEdgeConnectedChild { get; set; }

      /// <summary>
      /// Nodeの新しいインスタンスを初期化します。
      /// </summary>
      /// <param name="endpoint1">始点または頂点ID。</param>
      /// <param name="endpoint2">終点または頂点ID。</param>
      /// <param name="identityValue">このノードのValueとAggregatedValueの初期値となる単位元。</param>
      public Node(int endpoint1, int endpoint2, T identityValue)
      {
        Endpoint1 = endpoint1;
        Endpoint2 = endpoint2;
        Value = identityValue;
        AggregatedValue = identityValue;
        SubtreeSize = (endpoint1 == endpoint2) ? 1 : 0;
        IsExactEdge = (endpoint1 < endpoint2);
        HasExactEdgeInChildren = IsExactEdge;
      }

      /// <summary>
      /// このノードがSplay Treeの根であるかどうかを判断します。
      /// </summary>
      /// <returns>根であればtrue、そうでなければfalse。</returns>
      public bool IsRoot() => Parent == null;
    }

    /// <summary>
    /// EulerTourTreeの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="vertexCount">このETTが管理するグラフの頂点数。</param>
    /// <param name="monoidOperator">値の集約に使用するモノイド演算子。</param>
    public EulerTourTree(int vertexCount, IMonoidOperator<T> monoidOperator)
    {
      _monoidOpETT = monoidOperator;
      _vertexPairToNodeMap = new List<Dictionary<int, Node>>(vertexCount);
      for (int i = 0; i < vertexCount; i++)
      {
        _vertexPairToNodeMap.Add(new Dictionary<int, Node>());
        _vertexPairToNodeMap[i][i] = new Node(i, i, _monoidOpETT.Identity);
      }
    }

    /// <summary>
    /// 指定された端点ペアに対応するノードを取得または作成します。
    /// (endpoint1, endpoint1) は頂点ノードを、(endpoint1, endpoint2) で endpoint1 < endpoint2 は辺ノードを表します。
    /// </summary>
    private Node GetNode(int endpoint1, int endpoint2)
    {
      if (endpoint1 < 0 || endpoint1 >= _vertexPairToNodeMap.Count)
      {
        throw new ArgumentOutOfRangeException(nameof(endpoint1), "Vertex index for endpoint1 is out of bounds.");
      }
      // endpoint2の範囲チェックも、もしendpoint1 != endpoint2 の場合に必要なら追加
      if (!_vertexPairToNodeMap[endpoint1].TryGetValue(endpoint2, out Node? node) || node == null)
      {
        node = new Node(endpoint1, endpoint2, _monoidOpETT.Identity);
        _vertexPairToNodeMap[endpoint1][endpoint2] = node;
      }
      return node;
    }

    /// <summary>
    /// 指定されたノードが属するSplay Treeの根ノードを探索します。
    /// </summary>
    private Node? FindRoot(Node? node)
    {
      if (node == null) return null;
      Node current = node;
      while (current.Parent != null)
      {
        current = current.Parent;
      }
      return current;
    }

    /// <summary>
    /// 2つのノードが同じSplay Tree（つまり同じ連結成分の表現）に属するかどうかを判断します。
    /// </summary>
    private bool AreNodesInSameTree(Node? node1, Node? node2)
    {
      if (node1 != null) Splay(node1);
      if (node2 != null) Splay(node2);
      return FindRoot(node1) == FindRoot(node2);
    }

    /// <summary>
    /// 指定されたノードを、それが属するオイラーツアー表現の新しい開始点（根）にします。
    /// </summary>
    private Node? Reroot(Node node)
    {
      var (leftPart, rightPart) = Split(node);
      return Merge(rightPart, leftPart);
    }

    /// <summary>
    /// 指定されたノードでSplay Treeを分割します。ノード自身は分割後の右側の木の根になります。
    /// </summary>
    private (Node? left, Node? right) Split(Node nodeToSplitAt)
    {
      Splay(nodeToSplitAt);
      Node? leftSubtree = nodeToSplitAt.Children[0];
      if (leftSubtree != null) leftSubtree.Parent = null;
      nodeToSplitAt.Children[0] = null;
      return (leftSubtree, UpdateNode(nodeToSplitAt));
    }

    /// <summary>
    /// 指定されたノードをSplay Treeから取り除き、元の左部分木と右部分木を返します。
    /// 取り除かれたノードは単一ノードの木になります。
    /// </summary>
    private (Node? left, Node? right) SplitRemovingNode(Node nodeToRemove)
    {
      Splay(nodeToRemove);
      Node? leftSubtree = nodeToRemove.Children[0];
      Node? rightSubtree = nodeToRemove.Children[1];
      if (leftSubtree != null) leftSubtree.Parent = null;
      nodeToRemove.Children[0] = null;
      if (rightSubtree != null) rightSubtree.Parent = null;
      nodeToRemove.Children[1] = null;
      UpdateNode(nodeToRemove);
      return (leftSubtree, rightSubtree);
    }

    /// <summary>
    /// 辺を表す2つのノード (edgeNode1, edgeNode2) を横断するパスを切断し、
    /// 結果として生じる3つの部分木を返します。主に辺の削除処理で使用されます。
    /// </summary>
    private (Node? part1, Node? part2, Node? part3) SplitForEdgeRemoval(Node edgeNode1, Node edgeNode2)
    {
      var (originalLeftOfEdge1, originalRightOfEdge1) = SplitRemovingNode(edgeNode1);
      if (AreNodesInSameTree(originalLeftOfEdge1, edgeNode2))
      {
        var (originalLeftOfEdge2, originalRightOfEdge2) = SplitRemovingNode(edgeNode2);
        return (originalLeftOfEdge2, originalRightOfEdge2, originalRightOfEdge1);
      }
      else
      {
        var (originalLeftOfEdge2, originalRightOfEdge2) = SplitRemovingNode(edgeNode2);
        return (originalLeftOfEdge1, originalLeftOfEdge2, originalRightOfEdge2);
      }
    }

    /// <summary>
    /// 2つのSplay Tree (firstTree と secondTree) を連結します。
    /// firstTreeの最も右の要素とsecondTreeの最も左の要素（根）を接続します。
    /// </summary>
    private Node? Merge(Node? firstTree, Node? secondTree)
    {
      if (firstTree == null) return secondTree;
      if (secondTree == null) return firstTree;
      Node currentRightmost = firstTree;
      while (currentRightmost.Children[1] != null)
      {
        currentRightmost = currentRightmost.Children[1];
      }
      Splay(currentRightmost);

      currentRightmost.Children[1] = secondTree;
      if (secondTree != null) secondTree.Parent = currentRightmost; // secondTreeがnullでないことを確認    
      return UpdateNode(currentRightmost);
    }

    /// <summary>
    /// 4つのSplay Treeを順番に連結します。主にLink操作で使用されます。
    /// </summary>
    private Node? Merge(Node? n1, Node? n2, Node? n3, Node? n4)
    {
      return Merge(n1, Merge(n2, Merge(n3, n4)));
    }

    /// <summary>
    /// 指定されたノードを根とする部分木のサイズ（頂点ノード数）を取得します。
    /// </summary>
    private int GetSubtreeSize(Node? node) => node?.SubtreeSize ?? 0;

    /// <summary>
    /// 指定されたノードの集約値やサイズなどの情報を、その子ノードに基づいて更新します。
    /// </summary>
    private Node UpdateNode(Node node)
    {
      node.AggregatedValue = _monoidOpETT.Identity;
      if (node.Children[0] is Node leftChild) node.AggregatedValue = _monoidOpETT.Operate(node.AggregatedValue, leftChild.AggregatedValue);
      if (node.Endpoint1 == node.Endpoint2) node.AggregatedValue = _monoidOpETT.Operate(node.AggregatedValue, node.Value);
      if (node.Children[1] is Node rightChild) node.AggregatedValue = _monoidOpETT.Operate(node.AggregatedValue, rightChild.AggregatedValue);

      node.SubtreeSize = GetSubtreeSize(node.Children[0]) + ((node.Endpoint1 == node.Endpoint2) ? 1 : 0) + GetSubtreeSize(node.Children[1]);

      node.HasNonTreeEdgeConnectedChild = (node.Children[0]?.HasNonTreeEdgeConnectedChild ?? false) ||
                                          node.IsNonTreeEdgeConnected ||
                                          (node.Children[1]?.HasNonTreeEdgeConnectedChild ?? false);

      node.HasExactEdgeInChildren = (node.Children[0]?.HasExactEdgeInChildren ?? false) ||
                                     node.IsExactEdge ||
                                     (node.Children[1]?.HasExactEdgeInChildren ?? false);
      return node;
    }

    /// <summary>
    /// 遅延評価された情報を子ノードに伝播します。（現在は未実装のプレースホルダ）
    /// </summary>
    private void PropagateLazy(Node node)
    {
      // Placeholder
    }

    /// <summary>
    /// Splay Treeの回転操作を行います。
    /// </summary>
    private void Rotate(Node node, bool isNodeLeftChildOfParent)
    {
      Debug.Assert(node.Parent != null, "Parent must exist for Rotate.");
      Node parentNode = node.Parent;
      Node? grandparent = parentNode.Parent;

      parentNode.Children[isNodeLeftChildOfParent ? 0 : 1] = node.Children[isNodeLeftChildOfParent ? 1 : 0];
      if (node.Children[isNodeLeftChildOfParent ? 1 : 0] is Node childToRelink)
      {
        childToRelink.Parent = parentNode;
      }

      node.Children[isNodeLeftChildOfParent ? 1 : 0] = parentNode;
      parentNode.Parent = node;

      UpdateNode(parentNode);
      UpdateNode(node);

      node.Parent = grandparent;
      if (grandparent != null)
      {
        if (grandparent.Children[0] == parentNode) grandparent.Children[0] = node;
        else if (grandparent.Children[1] == parentNode) grandparent.Children[1] = node;
      }
    }

    /// <summary>
    /// 指定されたノードを、それが属するSplay Treeの根に移動させるSplay操作を行います。
    /// </summary>
    private void Splay(Node node)
    {
      PropagateLazy(node);

      while (!node.IsRoot())
      {
        Debug.Assert(node.Parent != null, "Node must have a parent in Splay loop.");
        Node parent = node.Parent;
        if (parent.IsRoot())
        {
          PropagateLazy(parent); PropagateLazy(node);
          Rotate(node, node == parent.Children[0]);
        }
        else
        {
          Debug.Assert(parent.Parent != null, "Parent must have a parent if not root.");
          Node grandparent = parent.Parent;
          PropagateLazy(grandparent); PropagateLazy(parent); PropagateLazy(node);
          bool parentIsLeftChild = (parent == grandparent.Children[0]);
          bool nodeIsLeftChild = (node == parent.Children[0]);

          if (parentIsLeftChild == nodeIsLeftChild)
          {
            Rotate(parent, parentIsLeftChild);
            Rotate(node, nodeIsLeftChild);
          }
          else
          {
            Rotate(node, nodeIsLeftChild);
            Rotate(node, parentIsLeftChild);
          }
        }
      }
    }

    /// <summary>
    /// 指定された頂点IDが属する連結成分（ETT内の木）のサイズ（頂点数）を取得します。
    /// </summary>
    /// <param name="vertexId">サイズを取得したい連結成分内の任意の頂点ID。</param>
    /// <returns>連結成分のサイズ。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public int GetTreeSize(int vertexId)
    {
      Node node = GetNode(vertexId, vertexId);
      Splay(node);
      return node.SubtreeSize;
    }

    /// <summary>
    /// 2つの頂点IDが同じ連結成分（ETT内の同じ木）に属するかどうかを判定します。
    /// </summary>
    /// <param name="vertexId1">1つ目の頂点ID。</param>
    /// <param name="vertexId2">2つ目の頂点ID。</param>
    /// <returns>同じ連結成分に属していればtrue、そうでなければfalse。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public bool AreSame(int vertexId1, int vertexId2)
    {
      // GetNodeはSplayを行わないため、AreNodesInSameTree内でSplayされる
      return AreNodesInSameTree(GetNode(vertexId1, vertexId1), GetNode(vertexId2, vertexId2));
    }

    /// <summary>
    /// 指定された頂点IDに関連付けられた値を、指定された新しい値とモノイド演算で合成して更新します。
    /// </summary>
    /// <param name="vertexId">値を更新する頂点のID。</param>
    /// <param name="value">現在の値と合成する新しい値。</param>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public void UpdateValue(int vertexId, T value)
    {
      Node targetNode = GetNode(vertexId, vertexId);
      Splay(targetNode);
      targetNode.Value = _monoidOpETT.Operate(targetNode.Value, value);
      UpdateNode(targetNode);
    }

    /// <summary>
    /// 指定された頂点IDを含む連結成分（ETT内の木）で、"IsExactEdge"がtrueの辺ノードを探索し、
    /// 見つかった辺に対して指定されたアクションを実行します。主に代替路探索の準備で使われます。
    /// </summary>
    /// <param name="vertexId">探索を開始する連結成分内の任意の頂点ID。</param>
    /// <param name="onExactEdgeFound">IsExactEdgeがtrueの辺 (e1, e2) が見つかったときに呼び出されるアクション。</param>
    /// <remarks>
    /// このメソッド自体が処理する辺の数に依存しますが、各辺の処理にはSplay ($O(\log N)$) が含まれます。
    /// DynamicConnectivity全体の文脈では、このコストは償却されます。
    /// </remarks>
    public void ProcessExactEdgesInComponent(int vertexId, Action<int, int> onExactEdgeFound)
    {
      Node componentRoot = GetNode(vertexId, vertexId);
      Splay(componentRoot);

      void DfsProcessExactEdges(Node currentNode)
      {
        if (currentNode.Endpoint1 < currentNode.Endpoint2 && currentNode.IsExactEdge)
        {
          Splay(currentNode);
          currentNode.IsExactEdge = false;
          UpdateNode(currentNode);
          onExactEdgeFound(currentNode.Endpoint1, currentNode.Endpoint2);
          return; // 1つ処理したら戻る（Splayで構造が変わるため）
        }

        Node? leftChild = currentNode.Children[0];
        if (leftChild != null && leftChild.HasExactEdgeInChildren)
        {
          DfsProcessExactEdges(leftChild);
          // Splayにより構造が変わった可能性があるので、再度componentRootから探索を始めるか、
          // または、このDFSの呼び出し方自体を工夫する必要があるかもしれない。
          // 元のC++コードでは、whileループでSplay(t)を繰り返していた。
          // 安全のため、1つ処理したらループに戻るようにする。
          return;
        }

        Node? rightChild = currentNode.Children[1];
        if (rightChild != null && rightChild.HasExactEdgeInChildren)
        {
          DfsProcessExactEdges(rightChild);
          return;
        }
      }

      // HasExactEdgeInChildren がtrueの間、処理を試みる
      while (componentRoot != null && componentRoot.HasExactEdgeInChildren)
      {
        // componentRoot はSplayされているので、もし自身がExactEdgeなら処理される。
        // そうでなければ、DfsProcessExactEdgesが子孫のExactEdgeを探して処理する。
        // DfsProcessExactEdges内でSplayが起きるので、ループの先頭でcomponentRootを再Splayする。
        Splay(componentRoot); // ループの各反復でcomponentRootをSplayしなおす
        if (!componentRoot.HasExactEdgeInChildren) break; // Splay後に再度確認

        // 以前のコード: DfsProcessExactEdges(componentRoot); Splay(componentRoot);
        // DfsProcessExactEdgesが return で抜けた場合、Splay(componentRoot)で元の状態に戻る。
        // ここでは、Dfsが1つ処理したらループを抜けるか、またはループを継続するなら
        // componentRootの状態を常に最新に保つ必要がある。
        // よりシンプルなのは、見つかったら処理してループを抜ける(または再度ループ条件を確認する)

        // DfsProcessExactEdgesを呼び出す前にcomponentRootがsplayされている必要がある
        // DfsProcessExactEdgesは最初に見つけたExact Edgeを処理し、その過程でSplayを行う。
        // その後、ループが継続する場合、componentRootの状態を再度確認する。
        bool processedThisIteration = false;
        void DfsSingle(Node curr) // 1つだけ処理するDFS
        {
          if (processedThisIteration) return;
          if (curr.Endpoint1 < curr.Endpoint2 && curr.IsExactEdge)
          {
            Splay(curr); curr.IsExactEdge = false; UpdateNode(curr);
            onExactEdgeFound(curr.Endpoint1, curr.Endpoint2);
            processedThisIteration = true; return;
          }
          if (curr.Children[0] is Node l && l.HasExactEdgeInChildren) DfsSingle(l);
          if (processedThisIteration) return;
          if (curr.Children[1] is Node r && r.HasExactEdgeInChildren) DfsSingle(r);
        }
        DfsSingle(componentRoot);

        if (!processedThisIteration) break; // Exact Edgeが見つからなかったらループ終了

        // componentRootが指すノードはSplayで変わっている可能性があるため、
        // 常にvertexIdに対応するノードを再取得・Splayしてループを回すのが安全。
        componentRoot = GetNode(vertexId, vertexId);
        Splay(componentRoot);
      }
    }

    /// <summary>
    /// 指定された頂点IDを含む連結成分（ETT内の木）で、非ツリーエッジに接続されている頂点 (IsNonTreeEdgeConnectedがtrue) を探し、
    /// 見つかった頂点に対して指定されたアクションを実行し、アクションがtrueを返せば探索を終了します。
    /// 主に代替路探索で使用されます。
    /// </summary>
    /// <param name="vertexId">探索を開始する連結成分内の任意の頂点ID。</param>
    /// <param name="findAction">IsNonTreeEdgeConnectedがtrueの頂点が見つかったときに呼び出される関数。
    /// この関数がtrueを返すと、TryFindReplacementEdgeもtrueを返して終了します。</param>
    /// <returns>findActionがtrueを返した場合にtrue、そうでなければfalse。</returns>
    /// <remarks>
    /// 計算量はfindActionの内容と、条件を満たす頂点の見つかり方に依存します。
    /// DynamicConnectivity全体の文脈では、このコストは償却されます。
    /// </remarks>
    public bool TryFindReplacementEdge(int vertexId, Func<int, bool> findAction)
    {
      Node componentRoot = GetNode(vertexId, vertexId);
      Splay(componentRoot);

      bool DfsFindReplacement(Node currentNode)
      {
        if (currentNode.IsNonTreeEdgeConnected)
        {
          Splay(currentNode);
          if (findAction(currentNode.Endpoint1)) return true;
        }

        Node? leftChild = currentNode.Children[0];
        if (leftChild != null && leftChild.HasNonTreeEdgeConnectedChild)
        {
          if (DfsFindReplacement(leftChild)) return true;
        }

        Node? rightChild = currentNode.Children[1];
        if (rightChild != null && rightChild.HasNonTreeEdgeConnectedChild)
        {
          if (DfsFindReplacement(rightChild)) return true;
        }
        return false;
      }

      // HasNonTreeEdgeConnectedChildがtrueの間、試行する
      // Dfs内でSplayが起きるため、ループの先頭でcomponentRootを再Splayする
      while (componentRoot != null && componentRoot.HasNonTreeEdgeConnectedChild)
      {
        Splay(componentRoot); // ループの各反復でcomponentRootをSplayしなおす
        if (!componentRoot.HasNonTreeEdgeConnectedChild) break; // Splay後に再度確認

        if (DfsFindReplacement(componentRoot)) return true;

        // DfsFindReplacement が false を返したが、HasNonTreeEdgeConnectedChild はまだ true の場合、
        // 処理されていないノードがある可能性がある。しかし、上記のDFSは全探索するはず。
        // もし DfsFindReplacement が false を返したら、このループは終了すべき。
        // (findAction が true を返さなかったため)
        break;
      }
      return false;
    }

    /// <summary>
    /// 指定された頂点IDのIsNonTreeEdgeConnectedフラグ（非ツリーエッジに接続されているか）を更新します。
    /// </summary>
    /// <param name="vertexId">フラグを更新する頂点のID。</param>
    /// <param name="isConnected">新しいフラグの値。</param>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public void UpdateNonTreeEdgeConnection(int vertexId, bool isConnected)
    {
      Node node = GetNode(vertexId, vertexId);
      Splay(node);
      node.IsNonTreeEdgeConnected = isConnected;
      UpdateNode(node);
    }

    /// <summary>
    /// 2つの頂点間に辺を接続します（ETTレベルでのオイラーツアー操作）。
    /// </summary>
    /// <param name="endpoint1">辺の一方の端点ID。</param>
    /// <param name="endpoint2">辺のもう一方の端点ID。</param>
    /// <returns>接続に成功すればtrue、既に同じ連結成分に属していた場合はfalse。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public bool Link(int endpoint1, int endpoint2)
    {
      if (AreSame(endpoint1, endpoint2)) return false;
      Node node1 = GetNode(endpoint1, endpoint1);
      Node node2 = GetNode(endpoint2, endpoint2);
      Node edge12 = GetNode(endpoint1, endpoint2);
      Node edge21 = GetNode(endpoint2, endpoint1);

      Merge(Reroot(node1), edge12, Reroot(node2), edge21);
      return true;
    }

    /// <summary>
    /// 2つの頂点間の辺を切断します（ETTレベルでのオイラーツアー操作）。
    /// </summary>
    /// <param name="endpoint1">辺の一方の端点ID。</param>
    /// <param name="endpoint2">辺のもう一方の端点ID。</param>
    /// <returns>切断に成功すればtrue、辺が存在しなかった場合はfalse。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public bool Cut(int endpoint1, int endpoint2)
    {
      if (endpoint1 < 0 || endpoint1 >= _vertexPairToNodeMap.Count ||
          endpoint2 < 0 || endpoint2 >= _vertexPairToNodeMap.Count ||
          !_vertexPairToNodeMap[endpoint1].ContainsKey(endpoint2) ||
          !_vertexPairToNodeMap[endpoint2].ContainsKey(endpoint1))
      {
        return false;
      }

      Node edge12 = GetNode(endpoint1, endpoint2);
      Node edge21 = GetNode(endpoint2, endpoint1);

      var (part1, _, part3) = SplitForEdgeRemoval(edge12, edge21);
      Merge(part1, part3);

      _vertexPairToNodeMap[endpoint1].Remove(endpoint2);
      _vertexPairToNodeMap[endpoint2].Remove(endpoint1);

      return true;
    }

    /// <summary>
    /// 特定の辺(cutEndpoint1, sumEndpoint)を一時的に切断し、sumEndpoint側の連結成分の集約値を取得します。
    /// その後、辺を再接続します。
    /// </summary>
    /// <param name="cutEndpoint1">一時的に切断する辺の端点1。</param>
    /// <param name="sumEndpoint">集約値を取得する側の連結成分に含まれる辺の端点2。</param>
    /// <returns>sumEndpointが含まれる（切断後の）連結成分の集約値。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public T GetSumAcrossCut(int cutEndpoint1, int sumEndpoint)
    {
      bool cutPerformed = false;
      if (cutEndpoint1 >= 0 && cutEndpoint1 < _vertexPairToNodeMap.Count &&
          sumEndpoint >= 0 && sumEndpoint < _vertexPairToNodeMap.Count &&
          _vertexPairToNodeMap[cutEndpoint1].ContainsKey(sumEndpoint) &&
          _vertexPairToNodeMap[sumEndpoint].ContainsKey(cutEndpoint1))
      {
        cutPerformed = Cut(cutEndpoint1, sumEndpoint);
      }

      Node sumNode = GetNode(sumEndpoint, sumEndpoint);
      Splay(sumNode);
      T result = sumNode.AggregatedValue;

      if (cutPerformed)
      {
        Link(cutEndpoint1, sumEndpoint);
      }

      return result;
    }

    /// <summary>
    /// 指定された頂点IDが属する連結成分全体の集約値を取得します。
    /// </summary>
    /// <param name="vertexId">集約値を取得したい連結成分内の任意の頂点ID。</param>
    /// <returns>連結成分全体の集約値。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public T GetComponentSum(int vertexId)
    {
      Node node = GetNode(vertexId, vertexId);
      Splay(node);
      return node.AggregatedValue;
    }

    /// <summary>
    /// 指定された頂点IDに対応するノードの持つ固有の値 (Value) を取得します。
    /// </summary>
    /// <param name="vertexId">値を取得したい頂点のID。</param>
    /// <returns>頂点sのValue。</returns>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public T GetSpecificNodeValue(int vertexId)
    {
      Node node = GetNode(vertexId, vertexId);
      Splay(node);
      return node.Value;
    }

    /// <summary>
    /// 指定された頂点IDが属する連結成分（このETTが表す木）に含まれる全ての頂点のIDリストを取得します。
    /// </summary>
    /// <param name="vertexIdInComponent">情報を取得したい連結成分内の任意の頂点ID。</param>
    /// <returns>連結成分内の全頂点IDのリスト。指定されたvertexIdが無効な場合は空のリストを返すことがあります。</returns>
    /// <remarks>計算量: 成分の頂点数を $V_c$、グラフ全体の頂点数を $N$ とすると、償却 $O(V_c + \log N)$。
    /// (初期のSplay操作に $O(\log N)$、成分内のノード走査に $O(V_c)$）。
    /// </remarks>
    public List<int> GetVerticesInComponent(int vertexIdInComponent)
    {
      // 基本的な範囲チェック (GetNodeがより詳細なチェックを行う)
      if (vertexIdInComponent < 0 || vertexIdInComponent >= _vertexPairToNodeMap.Count)
      {
        return new List<int>();
      }

      Node componentRepresentativeNode = GetNode(vertexIdInComponent, vertexIdInComponent);
      Splay(componentRepresentativeNode); // 連結成分に対応するETTの根をSplay

      var collectedVertices = new List<int>();
      CollectVertexNodesRecursive(componentRepresentativeNode, collectedVertices);

      // 必要であればソートする (現在はSplay Treeのin-order順に近い順序になる)
      // collectedVertices.Sort(); 
      return collectedVertices;
    }

    /// <summary>
    /// 指定されたノードを根とするSplay Treeを再帰的に辿り、頂点ノードを収集します。
    /// </summary>
    private void CollectVertexNodesRecursive(Node? currentNode, List<int> collectedVertices)
    {
      if (currentNode == null)
      {
        return;
      }

      // 中間順巡回 (In-order traversal) でノードを訪問
      CollectVertexNodesRecursive(currentNode.Children[0], collectedVertices);

      if (currentNode.Endpoint1 == currentNode.Endpoint2) // 頂点ノード (例: (v,v)) かどうか
      {
        // 通常、ETTの構造とSplay Treeの走査により、各頂点ノードは一度だけ現れるはず。
        // 重複を厳密に避けたい場合は、Listの代わりにHashSetを内部的に使うこともできる。
        collectedVertices.Add(currentNode.Endpoint1);
      }

      CollectVertexNodesRecursive(currentNode.Children[1], collectedVertices);
    }

    /// <summary>
    /// 指定された頂点IDに対応するノードの持つ固有の値 (Value) を、指定された新しい値で直接置き換えます。
    /// 更新後、このノードを含むSplay Treeのルートまでの集約値が再計算されます。
    /// </summary>
    /// <param name="vertexId">値を設定する頂点のID。</param>
    /// <param name="newValue">設定する新しい値。</param>
    /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
    public void SetSpecificNodeValue(int vertexId, T newValue)
    {
      // GetNodeは、(vertexId, vertexId)が頂点vertexId自身を表すノードを取得します。
      // 範囲外のvertexIdに対するエラー処理はGetNode内に含まれるか、
      // 呼び出し元のDynamicConnectivityクラスで事前に行われます。
      Node targetNode = GetNode(vertexId, vertexId);
      Splay(targetNode);         // ノードを根に移動
      targetNode.Value = newValue; // 値を直接置き換え
      UpdateNode(targetNode);    // 集約値 (AggregatedValue) などを再計算
    }
  } // EulerTourTreeクラス終わり


  /// <summary>
  /// DynamicConnectivityの新しいインスタンスを初期化します。
  /// </summary>
  /// <param name="vertexCount">グラフの頂点数。</param>
  /// <param name="monoidOperator">頂点の値に対するモノイド演算を定義したオペレータ。</param>
  /// <remarks>計算量: $O(N)$、ここで $N$ は頂点数。</remarks>
  public DynamicConnectivity(int vertexCount, IMonoidOperator<T> monoidOperator)
  {
    if (vertexCount < 0) throw new ArgumentOutOfRangeException(nameof(vertexCount), "Number of vertices cannot be negative.");
    _vertexCount = vertexCount;
    _monoidOperator = monoidOperator ?? throw new ArgumentNullException(nameof(monoidOperator));

    _eulerTourTrees = new List<EulerTourTree>();
    if (_vertexCount > 0)
    {
      _eulerTourTrees.Add(new EulerTourTree(_vertexCount, _monoidOperator));
    }

    _nonTreeEdgesByLevel = new List<List<HashSet<int>>>();
    if (_vertexCount > 0)
    {
      var initialLevelEdges = new List<HashSet<int>>(_vertexCount);
      for (int i = 0; i < _vertexCount; i++)
      {
        initialLevelEdges.Add(new HashSet<int>());
      }
      _nonTreeEdgesByLevel.Add(initialLevelEdges);
    }
  }

  /// <summary>
  /// 指定された頂点番号が有効な範囲内にあるかを確認します。
  /// </summary>
  private bool IsValidVertex(int vertexId) => vertexId >= 0 && vertexId < _vertexCount;

  /// <summary>
  /// 2つの頂点間に辺を追加します。
  /// もし2頂点が既に連結している場合、この辺は非ツリーエッジとして扱われます。
  /// </summary>
  /// <param name="sourceVertexId">辺の始点となる頂点のID。</param>
  /// <param name="targetVertexId">辺の終点となる頂点のID。</param>
  /// <returns>辺が新たに追加され、グラフの連結性が変化した場合はtrue。
  /// 既に連結していた、または自身へのループなどで辺が実質的に追加されなかった場合はfalse。</returns>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public bool Link(int sourceVertexId, int targetVertexId)
  {
    if (!IsValidVertex(sourceVertexId) || !IsValidVertex(targetVertexId)) return false;
    if (sourceVertexId == targetVertexId) return false;
    if (_vertexCount == 0) return false; // ETTが存在しない

    if (_eulerTourTrees[0].Link(sourceVertexId, targetVertexId)) return true;

    _nonTreeEdgesByLevel[0][sourceVertexId].Add(targetVertexId);
    _nonTreeEdgesByLevel[0][targetVertexId].Add(sourceVertexId);
    // Count == 1 は、その頂点にとってこれが最初の非ツリーエッジであることを意味する
    if (_nonTreeEdgesByLevel[0][sourceVertexId].Count == 1) _eulerTourTrees[0].UpdateNonTreeEdgeConnection(sourceVertexId, true);
    if (_nonTreeEdgesByLevel[0][targetVertexId].Count == 1) _eulerTourTrees[0].UpdateNonTreeEdgeConnection(targetVertexId, true);
    return false;
  }

  /// <summary>
  /// 2つの頂点が同じ連結成分に属しているかどうかを判定します。
  /// </summary>
  /// <param name="vertexId1">1つ目の頂点ID。</param>
  /// <param name="vertexId2">2つ目の頂点ID。</param>
  /// <returns>同じ連結成分に属していればtrue、そうでなければfalse。</returns>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public bool SameComponent(int vertexId1, int vertexId2)
  {
    if (!IsValidVertex(vertexId1) || !IsValidVertex(vertexId2)) return false;
    if (vertexId1 == vertexId2) return true;
    if (_vertexCount == 0) return false;
    return _eulerTourTrees[0].AreSame(vertexId1, vertexId2);
  }

  /// <summary>
  /// 指定された頂点が属する連結成分のサイズ（頂点数）を取得します。
  /// </summary>
  /// <param name="vertexId">サイズを調べる連結成分内の任意の頂点ID。</param>
  /// <returns>連結成分のサイズ。頂点が存在しない場合は0。</returns>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public int ComponentSize(int vertexId)
  {
    if (!IsValidVertex(vertexId)) return 0;
    if (_vertexCount == 0) return 0;
    return _eulerTourTrees[0].GetTreeSize(vertexId);
  }

  /// <summary>
  /// 指定された番号の頂点に設定されている値を、指定された新しい値で直接置き換えます。
  /// これはモノイド演算による合成ではなく、単純な上書きです。
  /// 関連する連結成分の集約値も適切に更新されます。
  /// </summary>
  /// <param name="vertexId">値を設定する頂点の番号。</param>
  /// <param name="newValue">設定する新しい値。</param>
  /// <exception cref="ArgumentOutOfRangeException">指定されたvertexIdが無効な場合。</exception>
  /// <exception cref="InvalidOperationException">グラフの頂点数が0の場合。</exception>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public void SetValue(int vertexId, T newValue)
  {
    if (!IsValidVertex(vertexId))
    {
      throw new ArgumentOutOfRangeException(nameof(vertexId), "Specified vertex ID is out of valid range.");
    }
    if (_vertexCount == 0) // 頂点が一つもない場合
    {
      // このケースは IsValidVertex で vertexId < 0 となるため通常は到達しないが、
      // _vertexCount が 0 の場合に _eulerTourTrees[0] にアクセスするのを防ぐ意味で重要。
      throw new InvalidOperationException("Cannot set value in a graph with zero vertices.");
    }

    // レベル0のEulerTourTreeの値を設定
    _eulerTourTrees[0].SetSpecificNodeValue(vertexId, newValue);
  }

  /// <summary>
  /// 指定された頂点に関連付けられた値を、指定された新しい値とモノイド演算で合成して更新します。
  /// </summary>
  /// <param name="vertexId">値を更新する頂点のID。</param>
  /// <param name="value">現在の値と合成する新しい値。</param>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public void UpdateValue(int vertexId, T value)
  {
    if (!IsValidVertex(vertexId)) return;
    if (_vertexCount == 0) return;
    _eulerTourTrees[0].UpdateValue(vertexId, value);
  }

  /// <summary>
  /// 指定された番号の頂点に直接設定されている値 (Value) を取得します。
  /// これは連結成分全体の集約値ではなく、その頂点固有の値です。
  /// </summary>
  /// <param name="vertexId">値を取得したい頂点の番号。</param>
  /// <returns>頂点sのValue。頂点が存在しないか無効な場合はオペレータの単位元を返します。</returns>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public T Get(int vertexId)
  {
    if (!IsValidVertex(vertexId))
    {
      return _monoidOperator.Identity;
    }
    if (_vertexCount == 0)
    {
      return _monoidOperator.Identity;
    }
    return _eulerTourTrees[0].GetSpecificNodeValue(vertexId);
  }

  /// <summary>
  /// 指定された頂点が属する連結成分全体の集約値を取得します。
  /// </summary>
  /// <param name="vertexId">集約値を取得したい連結成分内の任意の頂点ID。</param>
  /// <returns>連結成分全体の集約値。頂点が存在しない場合はオペレータの単位元。</returns>
  /// <remarks>計算量: 償却 $O(\log N)$。</remarks>
  public T GetSum(int vertexId)
  {
    if (!IsValidVertex(vertexId)) return _monoidOperator.Identity;
    if (_vertexCount == 0) return _monoidOperator.Identity;
    return _eulerTourTrees[0].GetComponentSum(vertexId);
  }

  /// <summary>
  /// 2つの頂点間の辺を削除します。
  /// もしこの辺が橋であり、代替路が見つからなければ、連結成分が分割されます。
  /// </summary>
  /// <param name="vertexId1">辺の一方の端点ID。</param>
  /// <param name="vertexId2">辺のもう一方の端点ID。</param>
  /// <returns>辺の削除によりグラフの連結性が実際に断たれた（代替路が見つからなかった）場合はtrue。
  /// 辺が非ツリーエッジだった、代替路が見つかった、または辺が存在しなかった場合はfalse。</returns>
  /// <remarks>計算量: 償却 $O(\log^2 N)$。</remarks>
  public bool Cut(int vertexId1, int vertexId2)
  {
    if (!IsValidVertex(vertexId1) || !IsValidVertex(vertexId2)) return false;
    if (vertexId1 == vertexId2) return false;
    if (_vertexCount == 0) return false;

    for (int level = 0; level < _levelCount; level++)
    {
      bool sRemoved = _nonTreeEdgesByLevel[level][vertexId1].Remove(vertexId2);
      bool tRemoved = _nonTreeEdgesByLevel[level][vertexId2].Remove(vertexId1);
      if (sRemoved && _nonTreeEdgesByLevel[level][vertexId1].Count == 0) _eulerTourTrees[level].UpdateNonTreeEdgeConnection(vertexId1, false);
      if (tRemoved && _nonTreeEdgesByLevel[level][vertexId2].Count == 0) _eulerTourTrees[level].UpdateNonTreeEdgeConnection(vertexId2, false);
    }

    for (int level = _levelCount - 1; level >= 0; level--)
    {
      if (_eulerTourTrees[level].Cut(vertexId1, vertexId2))
      {
        if (_levelCount - 1 == level)
        {
          _levelCount++;
          if (_vertexCount > 0) _eulerTourTrees.Add(new EulerTourTree(_vertexCount, _monoidOperator));
          else _eulerTourTrees.Add(new EulerTourTree(0, _monoidOperator)); // vertexCountが0の場合も考慮

          var newLevelEdges = new List<HashSet<int>>(_vertexCount); // _vertexCountが0なら空のリスト
          for (int j = 0; j < _vertexCount; j++) newLevelEdges.Add(new HashSet<int>());
          _nonTreeEdgesByLevel.Add(newLevelEdges);
        }
        return !TryReconnect(vertexId1, vertexId2, level);
      }
    }
    return false;
  }

  /// <summary>
  /// レベルlevelCutOccurredで辺(vertexId1, vertexId2)が切断された後、
  /// レベルlevelCutOccurred以下のグラフで代替路を探します。
  /// </summary>
  private bool TryReconnect(int vertexId1, int vertexId2, int levelCutOccurred)
  {
    int u = vertexId1;
    int v = vertexId2;

    for (int currentLevel = levelCutOccurred; currentLevel >= 0; currentLevel--)
    {
      // _eulerTourTrees[currentLevel] が存在することを確認 (Cutで新しいレベルが追加された直後など)
      if (currentLevel >= _eulerTourTrees.Count) continue;

      if (_eulerTourTrees[currentLevel].GetTreeSize(u) > _eulerTourTrees[currentLevel].GetTreeSize(v)) Swap(ref u, ref v);

      // currentLevel + 1 が範囲内であることを確認
      if (currentLevel + 1 >= _eulerTourTrees.Count)
      {
        // このケースは、最上位レベルのさらに上に昇格させようとする場合に発生しうる。
        // Cutで新しいレベルは事前に作成されるはずなので、通常はここに来ないか、
        // もし来た場合は新しいレベルをその場で作る必要があるが、Cut内のロジックで対応済みのはず。
        // ここで例外を投げるか、安全にスキップする。
        // Debug.Fail("Trying to access ETT level out of bounds during promoteEdgeAction setup.");
        // continue; // または、適切なエラー処理
      }

      Action<int, int> promoteEdgeAction = (edgeSource, edgeTarget) =>
      {
        if (currentLevel + 1 < _eulerTourTrees.Count)
        {
          _eulerTourTrees[currentLevel + 1].Link(edgeSource, edgeTarget);
        }
        // currentLevel + 1 が範囲外の場合の処理は、Cutでレベルが追加されるので通常発生しない想定
      };
      _eulerTourTrees[currentLevel].ProcessExactEdgesInComponent(u, promoteEdgeAction);

      Func<int, bool> findReplacementAction = (vertexToSearchFrom) =>
      {
        var neighbors = new List<int>(_nonTreeEdgesByLevel[currentLevel][vertexToSearchFrom]);
        foreach (var neighbor in neighbors)
        {
          if (!_nonTreeEdgesByLevel[currentLevel][vertexToSearchFrom].Contains(neighbor)) continue;

          _nonTreeEdgesByLevel[currentLevel][vertexToSearchFrom].Remove(neighbor);
          _nonTreeEdgesByLevel[currentLevel][neighbor].Remove(vertexToSearchFrom);

          if (_nonTreeEdgesByLevel[currentLevel][vertexToSearchFrom].Count == 0) _eulerTourTrees[currentLevel].UpdateNonTreeEdgeConnection(vertexToSearchFrom, false);
          if (_nonTreeEdgesByLevel[currentLevel][neighbor].Count == 0) _eulerTourTrees[currentLevel].UpdateNonTreeEdgeConnection(neighbor, false);

          if (_eulerTourTrees[currentLevel].AreSame(vertexToSearchFrom, neighbor))
          {
            if (currentLevel + 1 < _nonTreeEdgesByLevel.Count) // 次のレベルが存在するか確認
            {
              _nonTreeEdgesByLevel[currentLevel + 1][vertexToSearchFrom].Add(neighbor);
              _nonTreeEdgesByLevel[currentLevel + 1][neighbor].Add(vertexToSearchFrom);
              if (_nonTreeEdgesByLevel[currentLevel + 1][vertexToSearchFrom].Count == 1) _eulerTourTrees[currentLevel + 1].UpdateNonTreeEdgeConnection(vertexToSearchFrom, true);
              if (_nonTreeEdgesByLevel[currentLevel + 1][neighbor].Count == 1) _eulerTourTrees[currentLevel + 1].UpdateNonTreeEdgeConnection(neighbor, true);
            }
          }
          else
          {
            for (int linkLevel = 0; linkLevel <= currentLevel; linkLevel++)
            {
              _eulerTourTrees[linkLevel].Link(vertexToSearchFrom, neighbor);
            }
            return true;
          }
        }
        return false;
      };

      if (_eulerTourTrees[currentLevel].TryFindReplacementEdge(u, findReplacementAction)) return true;
    }
    return false;
  }

  /// <summary>
  /// 2つのアイテムを交換します。
  /// </summary>
  private void Swap<TItem>(ref TItem a, ref TItem b)
  {
    (a, b) = (b, a);
  }

  /// <summary>
  /// 指定された頂点が属する連結成分に含まれる全ての頂点のIDリストを取得します。
  /// </summary>
  /// <param name="vertexId">連結成分を特定するための頂点ID。</param>
  /// <returns>
  /// 指定された頂点が属する連結成分の全頂点IDのリスト。
  /// 指定された頂点IDが無効な場合や、グラフが空の場合は空のリストを返します。
  /// </returns>
  /// <remarks>
  /// 計算量: 成分の頂点数を $V_c$、グラフ全体の頂点数を $N$ とすると、償却 $O(V_c + \log N)$。
  /// </remarks>
  public List<int> GetVertices(int vertexId)
  {
    if (!IsValidVertex(vertexId))
    {
      return new List<int>();
    }
    if (_vertexCount == 0) // 頂点が一つもない場合
    {
      return new List<int>();
    }
    // レベル0のEulerTourTreeから頂点リストを取得
    return _eulerTourTrees[0].GetVerticesInComponent(vertexId);
  }
}

public struct Coord
{
  public int Y { get; set; }
  public int X { get; set; }

  public Coord(int y, int x)
  {
    Y = y;
    X = x;
  }

  public static int Distance(Coord c1, Coord c2)
  {
    return Math.Abs(c1.Y - c2.Y) + Math.Abs(c1.X - c2.X);
  }

  public static bool operator ==(Coord c1, Coord c2)
  {
    return c1.Y == c2.Y && c1.X == c2.X;
  }

  public static bool operator !=(Coord c1, Coord c2)
  {
    return !(c1 == c2);
  }

  public override bool Equals(object? obj)
  {
    return obj is Coord coord && this == coord;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Y, X);
  }

  public override string ToString()
  {
    return $"({Y},{X})";
  }

  public void Deconstruct(out int y, out int x)
  {
    y = Y;
    x = X;
  }
}

public struct CMY
{
  public double C { get; set; }
  public double M { get; set; }
  public double Y { get; set; }

  public CMY(double c, double m, double y)
  {
    C = c;
    M = m;
    Y = y;
  }

  public static bool operator ==(CMY cmy1, CMY cmy2) => cmy1.Equals(cmy2);

  public static bool operator !=(CMY cmy1, CMY cmy2) => !(cmy1 == cmy2);

  public static CMY operator +(CMY a, CMY b) => new CMY(a.C + b.C, a.M + b.M, a.Y + b.Y);

  public static CMY operator *(double scalar, CMY cmy) => new CMY(scalar * cmy.C, scalar * cmy.M, scalar * cmy.Y);

  public static CMY operator *(CMY cmy, double scalar) => scalar * cmy;

  public static CMY operator /(CMY cmy, double scalar)
  {
    if (Math.Abs(scalar) < 1e-9) return new CMY(0, 0, 0);
    return new CMY(cmy.C / scalar, cmy.M / scalar, cmy.Y / scalar);
  }

  public static double MaxDistance => Math.Sqrt(3);

  public static (double All, double C, double M, double Y) Distance(CMY cmy1, CMY cmy2)
  {
    double dc = cmy1.C - cmy2.C;
    double dm = cmy1.M - cmy2.M;
    double dy = cmy1.Y - cmy2.Y;
    return (Math.Sqrt(dc * dc + dm * dm + dy * dy), Math.Abs(dc), Math.Abs(dm), Math.Abs(dy));
  }

  public override bool Equals(object? obj) => obj is CMY cmy && C == cmy.C && M == cmy.M && Y == cmy.Y;

  public override int GetHashCode() => HashCode.Combine(C, M, Y);

  public override string ToString() => $"({C},{M},{Y})";

  public void Deconstruct(out double c, out double m, out double y)
  {
    c = C;
    m = M;
    y = Y;
  }
}

public static class CMYExtensions
{
  public static CMY Sum(this IEnumerable<CMY> source)
  {
    if (source == null)
    {
      throw new ArgumentNullException(nameof(source));
    }

    CMY sum = new CMY(0, 0, 0);
    foreach (CMY item in source)
    {
      sum += item;
    }
    return sum;
  }
}

public class Cell
{
  public CMY Color { get; private set; }

  public double Volume { get; private set; }

  public int Capacity { get; private set; }

  public Cell(CMY color, double volume, int capacity)
  {
    Color = color;
    Volume = Math.Max(volume, 0);
    Capacity = Math.Max(capacity, 0);
  }

  public override bool Equals(object? obj) => obj is Cell cell
    && Color.Equals(cell.Color)
    && Volume == cell.Volume
    && Capacity == cell.Capacity;

  public override int GetHashCode() => HashCode.Combine(Color, Volume, Capacity);

  public static bool operator ==(Cell c1, Cell c2) => c1.Equals(c2);

  public static bool operator !=(Cell c1, Cell c2) => !(c1 == c2);

  public static Cell operator +(Cell c1, Cell c2)
  {
    int newCapacity = c1.Capacity + c2.Capacity;
    double newVolume = c1.Volume + c2.Volume;

    CMY newColor = new CMY(0, 0, 0);
    if (newVolume > 1e-9)
    {
      double c = (c1.Volume * c1.Color.C + c2.Volume * c2.Color.C) / newVolume;
      double m = (c1.Volume * c1.Color.M + c2.Volume * c2.Color.M) / newVolume;
      double y = (c1.Volume * c1.Color.Y + c2.Volume * c2.Color.Y) / newVolume;
      newColor = new CMY(c, m, y);
    }

    return new Cell(newColor, newVolume, newCapacity);
  }

  public static Cell operator /(Cell cell, int divisor)
  {
    if (divisor <= 0) throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be a positive integer.");
    if (cell.Capacity % divisor != 0)
      throw new ArgumentException($"Cell Capacity ({cell.Capacity}) is not exactly divisible by the divisor ({divisor}).", nameof(divisor));
    return new Cell(cell.Color, cell.Volume / (double)divisor, cell.Capacity / divisor);
  }

  public override string ToString() => $"{Color}({Volume}/{Capacity})";

  public void Deconstruct(out CMY color, out double capacity, out double volume)
  {
    color = Color;
    capacity = Capacity;
    volume = Volume;
  }
}

public class CellSumOperator : IMonoidOperator<Cell>
{
  public Cell Identity => new Cell(new CMY(0, 0, 0), 0, 0);
  public Cell Operate(Cell c1, Cell c2) => c1 + c2;
}

public class Palette
{
  public static readonly int Size;
  // public static readonly int Kinds;
  // public static readonly int Targets;
  public static readonly int MaxTurns;
  public static readonly int Cost;

  static Palette()
  {
    Size = N;
    // Kinds = K;
    // Targets = H;
    MaxTurns = T;
    Cost = D;
  }

  // private Cell[,] _cells;

  private DynamicConnectivity<Cell> _dc;

  private int[,] _verticalDividers;

  private int[,] _horizontalDividers;

  private List<(int[] Log, int VisualizedScore, double EvaluatedScore)> _logs;

  private List<(CMY Color, double Deviation)> _madePaints;

  public int OpCount { get; private set; }

  public int AddCount { get; private set; }

  public (double Definite, double Tentative) Deviation { get; private set; }

  public int TargetId { get; private set; }

  public int VisualizedScore => (int)(1 + Cost * (AddCount - _madePaints.Count) + Math.Round(1e4 * Deviation.Definite));

  public double EvaluatedScore => 1 + Cost * Math.Max(AddCount - Targets.Count, 0) + Math.Round(1e4 * Deviation.Tentative);

  public Palette()
  {
    // _cells = new Cell[Size, Size];
    // for (int i = 0; i < Size; i++)
    // {
    //   for (int j = 0; j < Size; j++)
    //   {
    //     _cells[i, j] = new Cell(new CMY(0, 0, 0), 0, 1);
    //   }
    // }

    _dc = new(Size * Size, new CellSumOperator());
    for (int cy = 0; cy < Size; cy++)
    {
      for (int cx = 0; cx < Size; cx++)
      {
        int f1 = cy * Size + cx;
        _dc.SetValue(f1, new Cell(new CMY(0, 0, 0), 0, 1));

        for (int i = 0; i < DY.Length; i++)
        {
          int ey = cy + DY[i];
          int ex = cx + DX[i];
          if (ey < 0 || ey >= Size || ex < 0 || ex >= Size) continue;
          if ((cy == 1 && i == 2) || (cy == 2 && i == 0)) continue; // tmp
          int f2 = ey * Size + ex;
          _dc.Link(f1, f2);
        }
      }
    }

    _verticalDividers = new int[Size, Size - 1];
    _horizontalDividers = new int[Size - 1, Size];
    for (int i = 0; i < Size; i++)
    {
      for (int j = 0; j < Size - 1; j++)
      {
        _verticalDividers[i, j] = 0;
        _horizontalDividers[j, i] = 0;
      }
    }
    for (int i = 0; i < Size; i++)
    {
      _horizontalDividers[1, i] = 1; // tmp
    }

    // Error.WriteLine($"(0,0)の属するウェルのセル数 {_dc.GetVertices(0).Count}個");
    // Error.WriteLine($"(1,0)の属するウェルのセル数 {_dc.GetVertices(Size).Count}個");
    // Error.WriteLine($"(2,0)の属するウェルのセル数 {_dc.GetVertices(Size * 2).Count}個");
    // Error.WriteLine($"(3,0)の属するウェルのセル数 {_dc.GetVertices(Size * 3).Count}個");
    // Error.WriteLine($"(4,0)の属するウェルのセル数 {_dc.GetVertices(Size * 4).Count}個");

    _logs = new();
    _madePaints = new();

    OpCount = 0;
    AddCount = 0;
    Deviation = (0, CMY.MaxDistance * Targets.Count);
    TargetId = 0;
  }

  public Cell this[int y, int x]
  {
    get
    {
      if (y < 0 || y >= Size || x < 0 || x >= Size) throw new ArgumentOutOfRangeException();
      // return _cells[y, x];
      return _dc.Get(y * Size + x);
    }
    private set
    {
      if (y < 0 || y >= Size || x < 0 || x >= Size) throw new ArgumentOutOfRangeException();
      // _cells[y, x] = value;
      _dc.SetValue(y * Size + x, value);
    }
  }

  // ターゲットとなる色を調合して差し出す一連の操作を行ったときのスコアの悪化量を取得する
  public double GetScoreDeltaByAddition(int addCount, CMY color)
  {
    return Cost * addCount + 1e4 * CMY.Distance(color, Targets[TargetId]).All;
  }

  // ターン数が残っていて操作可能な状態かどうか判定する
  public bool IsOperatable() => OpCount <= MaxTurns;

  // ゲームの終了条件を満たしているか確認する
  public bool IsSubmittable() => TargetId >= Targets.Count;

  // 引数で渡されたリストの内容に応じて操作を行い1ターン進める
  public void Operate(int[] args)
  {
    int type = args[0];
    switch (type)
    {
      case 1:
        Add(new Coord(args[1], args[2]), args[3]);
        break;
      case 2:
        Give(new Coord(args[1], args[2]));
        break;
      case 3:
        Discard(new Coord(args[1], args[2]), false);
        break;
      case 4:
        SwitchDivider(new Coord(args[1], args[2]), new Coord(args[3], args[4]));
        break;
      default:
        throw new ArgumentException();
    }

    _logs.Add((args, VisualizedScore, EvaluatedScore));
    OpCount++;
  }

  // 操作1に対応するメソッド
  private void Add(Coord coord, int tubeId)
  {
    // 引数で指定したセルが属するウェルを取得
    int cid = coord.Y * Size + coord.X;
    var well = _dc.GetSum(cid);

    // 追加する絵の具の情報を最大容量0のセルで表現
    var tmpCell = new Cell(Tubes[tubeId], Math.Max(Math.Min(well.Capacity - well.Volume, 1), 0), 0);

    // ウェルに合成してから各セルに再分配する
    well += tmpCell;
    foreach (int vid in _dc.GetVertices(cid))
    {
      _dc.SetValue(vid, well / _dc.ComponentSize(cid));
    }

    _dc.GetVertices(cid);
    AddCount++;
  }

  // 操作2に対応するメソッド
  private void Give(Coord coord)
  {
    var cell = Discard(coord, true);
    double d = CMY.Distance(cell.Color, Targets[TargetId]).All;
    _madePaints.Add((cell.Color, d));
    Deviation = (Deviation.Definite + d, Deviation.Tentative - (CMY.MaxDistance - d));

    TargetId++;
  }

  // 操作3に対応するメソッド
  private Cell Discard(Coord coord, bool strict)
  {
    // 引数で指定したセルが属するウェルを取得
    int cid = coord.Y * Size + coord.X;
    var well = _dc.GetSum(cid);

    // strictが有効で絵の具の量が1gに満たない場合は例外
    if (well.Volume < 1 - 1e-6)
    {
      throw new InvalidOperationException("選択したウェルの絵の具の量が1g未満です。");
    }

    // 取り出す絵の具の情報を最大容量0のセルで表現
    double takenAmount = Math.Max(Math.Min(well.Capacity - well.Volume, 1), 0);
    var taken = new Cell(well.Color, takenAmount, 0);

    // 取り出す分だけ絵の具をウェルから廃棄し各セルに再分配する
    well = new Cell(well.Color, well.Volume - takenAmount, well.Capacity);
    foreach (int vid in _dc.GetVertices(cid))
    {
      _dc.SetValue(vid, well / _dc.ComponentSize(cid));
    }

    return taken;
  }

  // 操作4に対応するメソッド
  private void SwitchDivider(Coord c1, Coord c2)
  {
    throw new NotImplementedException();
  }

  public void Print(bool verbose = false)
  {
    // 仕切りの初期状態を出力
    for (int i = 0; i < Size; i++)
    {
      for (int j = 0; j < Size - 1; j++)
      {
        Write($"{_verticalDividers[i, j]} ");
      }
      WriteLine();
    }
    for (int i = 0; i < Size - 1; i++)
    {
      for (int j = 0; j < Size; j++)
      {
        Write($"{_horizontalDividers[i, j]} ");
      }
      WriteLine();
    }

    // ログを出力
    foreach ((int[] log, int v, double e) in _logs)
    {
      WriteLine(string.Join(' ', log));
      if (verbose) Error.WriteLine($"Visualized: {v}, Evaluated: {e}");
    }
  }
}

public static class Program
{
  public static readonly int[] DY = new int[] { -1, 0, 1, 0 };
  public static readonly int[] DX = new int[] { 0, 1, 0, -1 };

  public static int N { get; private set; }
  public static int K { get; private set; }
  public static int H { get; private set; }
  public static int T { get; private set; }
  public static int D { get; private set; }
  private static CMY[] _tubes;
  private static CMY[] _targets;
  public static ReadOnlyCollection<CMY> Tubes;
  public static ReadOnlyCollection<CMY> Targets;

  public static void Main(string[] args)
  {
    TimeKeeper.Start();
    Input();
    Greedy();
  }

  public static void Input()
  {
    int[] buf = ReadLine().Split().Select(int.Parse).ToArray();
    (N, K, H, T, D) = (buf[0], buf[1], buf[2], buf[3], buf[4]);

    Double[] buf2;
    _tubes = new CMY[K];
    for (int i = 0; i < K; i++)
    {
      buf2 = ReadLine().Split().Select(double.Parse).ToArray();
      _tubes[i] = new CMY(buf2[0], buf2[1], buf2[2]);
    }

    _targets = new CMY[H];
    for (int i = 0; i < H; i++)
    {
      buf2 = ReadLine().Split().Select(double.Parse).ToArray();
      _targets[i] = new CMY(buf2[0], buf2[1], buf2[2]);
    }

    Tubes = new(_tubes);
    Targets = new(_targets);
  }

  public static void Greedy()
  {
    var rand = new Random();

    // 1回のランダムな探索にかけられる実行時間
    int maxRandomMicroSec = 500;

    var plt = new Palette();
    for (int i = 0; i < H; i++)
    {
      // コストと操作可能な回数から1回の調合にかけられる操作回数
      int maxMixingCount = Math.Max(
        1,
        Math.Min(
          // int.MaxValue,
          32,
          // 8,
          Math.Min(10000 / D, (T - plt.OpCount) / (H - plt.TargetId) / 2)
        )
      );

      // 最初の数手は貪欲に最適なパターンを見つける
      int greedyCount = Math.Min(maxMixingCount, (int)Math.Log(1e4, Tubes.Count));
      var firstBests = new (CMY Color, double Delta, List<int> Tubes)[greedyCount + 1];
      for (int j = 0; j < firstBests.Length; j++) firstBests[j] = (new CMY(-1, -1, -1), double.MaxValue, new());
      [MethodImpl(256)] void Dfs(List<int> ptn, int kinds, int size)
      {
        if (ptn.Count >= 1)
        {
          CMY color = ptn.Select((id) => Tubes[id]).Sum() / ptn.Count;
          double delta = plt.GetScoreDeltaByAddition(ptn.Count, color);
          if (delta < firstBests[ptn.Count].Delta)
          {
            firstBests[ptn.Count] = (color, delta, ptn);
          }

          if (ptn.Count >= size) return;
        }

        for (int i = 0; i < kinds; i++)
        {
          List<int> newPtn = new(ptn);
          newPtn.Add(i);
          Dfs(newPtn, kinds, size);
        }
      }
      Dfs(new(), Tubes.Count, greedyCount);

      (CMY Color, double Delta, List<int> Tubes) best = (firstBests[0].Color, firstBests[0].Delta, new(firstBests[0].Tubes));
      long st = TimeKeeper.ElapsedMicrosec();

      int searchCount = 0;
      while (TimeKeeper.ElapsedMicrosec() - st < maxRandomMicroSec)
      {
        searchCount++;
        // 毎回パターンを作り直す
        // List<int> selectedTubes = new();

        // 最初の数手を固定してパターンを作っていく
        List<int> selectedTubes = new(firstBests[Math.Min(searchCount, greedyCount)].Tubes);

        // 現時点でベストな調合手順をコピーしてシャッフルし、半分を捨ててパターンを作り直す
        // List<int> selectedTubes = new(best.Tubes);
        // selectedTubes.Shuffle();
        // int discardCount = (selectedTubes.Count + 1) / 2;
        // for (int _ = 0; _ < discardCount; _++) selectedTubes.RemoveAt(selectedTubes.Count - 1);

        CMY? currentColor = (selectedTubes.Count >= 1
          ? selectedTubes.Select((id) => Tubes[id]).Sum() / selectedTubes.Count
          : null
        );

        // for (int cnt = 1; cnt <= maxMixingCount; cnt++)
        bool skip = true;
        do
        {
          if (skip)
          {
            skip = false;
          }
          else
          {
            int tubeId = rand.Next(0, Tubes.Count);
            CMY selectedColor = Tubes[tubeId];
            if (currentColor is null) currentColor = selectedColor;
            else currentColor = (currentColor + selectedColor) / 2;
            selectedTubes.Add(tubeId);

            // 選択した色のリストを同じ要素を持つ2つのリストに分けられる場合は操作回数を半分に削る
            if (selectedTubes.Count % 2 == 0)
            {
              List<int> t0 = new(selectedTubes);
              t0.Sort();
              List<int> t1 = new();
              List<int> t2 = new();
              while (t0.Count >= 1)
              {
                int x = t0.Last();
                t0.RemoveAt(t0.Count - 1);
                if (t1.Count <= t2.Count) t1.Add(x);
                else t2.Add(x);
              }

              bool check = true;
              for (int j = 0; j < t1.Count; j++)
              {
                if (t1[j] != t2[j])
                {
                  check = false;
                  break;
                }
              }

              if (check)
              {
                // Error.WriteLine($"cut! {string.Join(',', selectedTubes)} -> {string.Join(',', t1)}");
                selectedTubes = t1;
                currentColor = t1.Select((id) => Tubes[id]).Sum() / t1.Count;
              }
            }
          }

          double delta = plt.GetScoreDeltaByAddition(selectedTubes.Count, (CMY)currentColor);
          if (delta < best.Delta)
          {
            Error.WriteLine($"update: {best.Delta}({string.Join(',', best.Tubes)}) -> {delta}({string.Join(',', selectedTubes)})");
            best = ((CMY)currentColor, delta, new(selectedTubes));
          }
        } while (selectedTubes.Count < maxMixingCount && TimeKeeper.ElapsedMicrosec() - st < maxRandomMicroSec);
      }

      foreach (int tubeId in best.Tubes)
      {
        plt.Operate(new int[] { 1, 0, 0, tubeId });
      }

      for (int j = 0; j < best.Tubes.Count; j++)
      {
        if (j == 0) plt.Operate(new int[] { 2, 0, 0 });
        else plt.Operate(new int[] { 3, 0, 0 });
      }
    }

    plt.Print();
  }
}
