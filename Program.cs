using AtCoder;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Console;

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
/// 動的グラフの連結性を管理するクラス（削除可能なUnion-Find）。
/// Euler Tour Treeを階層的に利用して、辺の追加と削除を効率的に処理します。
/// </summary>
/// <typeparam name="T">各頂点に関連付ける値の型。IMonoidOperator&lt;T&gt;によって集約操作が定義される必要があります。</typeparam>
public class DynamicConnectivity<T>
{
  private readonly IMonoidOperator<T> _operator;
  private readonly List<EulerTourTree> _ett;
  private readonly List<List<HashSet<int>>> _edges; // 各レベルの非ツリーエッジの隣接リスト
  private readonly int _sz; // グラフの頂点数
  private int _dep = 1; // 現在のレベル数 (1から始まる)

  /// <summary>
  /// Euler Tour Treeを表す内部クラス。
  /// 各レベルの連結性を管理します。
  /// </summary>
  private class EulerTourTree
  {
    private readonly IMonoidOperator<T> _opETT;
    private List<Dictionary<int, Node>> _ptr; // 辺(l,r)および頂点(i,i)に対応するノードを管理するポインタのリスト。

    /// <summary>
    /// Euler Tour Treeのノードを表す内部クラス。
    /// Splay Treeのノードとして機能します。
    /// </summary>
    internal class Node
    {
      /// <summary>
      /// [0]が左の子、[1]が右の子。
      /// </summary>
      public Node?[] Ch { get; } = new Node?[2];
      /// <summary>
      /// 親ノード。
      /// </summary>
      public Node? P { get; set; }
      /// <summary>
      /// このノードが表す辺の始点、または頂点のID。
      /// </summary>
      public int L { get; }
      /// <summary>
      /// このノードが表す辺の終点、または頂点のID。頂点ノードの場合はLと同じ。
      /// </summary>
      public int R { get; }
      /// <summary>
      /// このノードを根とする部分木のサイズ（頂点ノードの数）。
      /// </summary>
      public int Sz { get; set; }
      /// <summary>
      /// このノード（頂点の場合）に関連付けられた値。
      /// </summary>
      public T Val { get; set; }
      /// <summary>
      /// このノードを根とする部分木の集約値。
      /// </summary>
      public T Sum { get; set; }
      /// <summary>
      /// このノードが辺を表し(L < R)、まだ処理されていない（上位レベルに昇格する可能性がある）かを示すフラグ。
      /// </summary>
      public bool Exact { get; set; }
      /// <summary>
      /// 子孫にExactなノードが存在するかを示すフラグ。
      /// </summary>
      public bool ChildExact { get; set; }
      /// <summary>
      /// この頂点ノードが、現在のETTレベルで非ツリーエッジに接続されているかを示すフラグ。
      /// </summary>
      public bool EdgeConnected { get; set; }
      /// <summary>
      /// 子孫にEdgeConnectedなノードが存在するかを示すフラグ。
      /// </summary>
      public bool ChildEdgeConnected { get; set; }

      /// <summary>
      /// Nodeの新しいインスタンスを初期化します。
      /// </summary>
      /// <param name="l">始点または頂点ID。</param>
      /// <param name="r">終点または頂点ID。</param>
      /// <param name="identityElementForNode">このノードのValとSumの初期値となる単位元。</param>
      public Node(int l, int r, T identityElementForNode)
      {
        L = l;
        R = r;
        Val = identityElementForNode; // ノード生成時は渡された単位元で初期化
        Sum = identityElementForNode;
        Sz = (l == r) ? 1 : 0; // 頂点ノードならサイズ1、辺ノードなら0
        Exact = (l < r);      // 辺ノードならtrue
        ChildExact = (l < r); // 初期状態では自身がExactならtrue
      }

      /// <summary>
      /// このノードが根であるかどうかを判断します。
      /// </summary>
      /// <returns>根であればtrue、そうでなければfalse。</returns>
      public bool IsRoot() => P == null;
    }


    /// <summary>
    /// EulerTourTreeの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="size">グラフの頂点数。</param>
    /// <param name="monoidOperator">値を集約するためのオペレータ。</param>
    public EulerTourTree(int size, IMonoidOperator<T> monoidOperator)
    {
      _opETT = monoidOperator;
      _ptr = new List<Dictionary<int, Node>>(size);
      for (int i = 0; i < size; i++)
      {
        _ptr.Add(new Dictionary<int, Node>());
        _ptr[i][i] = new Node(i, i, _opETT.Identity); // 各頂点iに対応するノード(i,i)を作成
      }
    }

    /// <summary>
    /// 指定されたl, rに対応するノードを取得または作成します。
    /// </summary>
    private Node GetNode(int l, int r)
    {
      if (!_ptr[l].TryGetValue(r, out var node))
      {
        node = new Node(l, r, _opETT.Identity); // 新規作成時はオペレータの単位元で初期化
        _ptr[l][r] = node;
      }
      return node;
    }

    /// <summary>
    /// 指定されたノードが属する木の根を返します。
    /// </summary>
    private Node? Root(Node? t)
    {
      if (t == null) return null;
      while (t.P != null) t = t.P;
      return t;
    }

    /// <summary>
    /// 2つのノードが同じ木に属するかどうかを判断します。
    /// </summary>
    public bool Same(Node? s, Node? t)
    {
      if (s != null) Splay(s);
      if (t != null) Splay(t);
      return Root(s) == Root(t);
    }

    /// <summary>
    /// 指定されたノードを木の新しい根にします（オイラーツアーの始点を変更）。
    /// </summary>
    private Node? Reroot(Node t)
    {
      var (s1, s2) = Split(t);
      return Merge(s2, s1);
    }

    /// <summary>
    /// 指定されたノードsで木を分割します。sは分割後の右側の木の根になります。
    /// </summary>
    /// <returns>左側の木と右側の木（sを根とする）。</returns>
    private (Node? left, Node? right) Split(Node s)
    {
      Splay(s);
      Node? t = s.Ch[0];
      if (t != null) t.P = null;
      s.Ch[0] = null;
      return (t, Update(s));
    }

    /// <summary>
    /// 指定されたノードsで木を分割し、sの左右の子木をそれぞれ独立させます。
    /// s自体は単一ノードの木になります。
    /// </summary>
    /// <returns>元のsの左の子木と右の子木。</returns>
    private (Node? left, Node? right) Split2(Node s)
    {
      Splay(s);
      Node? t = s.Ch[0];
      Node? u = s.Ch[1];
      if (t != null) t.P = null;
      s.Ch[0] = null;
      if (u != null) u.P = null;
      s.Ch[1] = null;
      Update(s);
      return (t, u);
    }

    /// <summary>
    /// 2つのエッジノードsとtに基づいて木を3つに分割します。
    /// </summary>
    public (Node? s1, Node? s2, Node? s3) Split(Node s, Node t)
    {
      var u = Split2(s);
      if (Same(u.left, t))
      {
        var r = Split2(t);
        return (r.left, r.right, u.right);
      }
      else
      {
        var r = Split2(t);
        return (u.left, r.left, r.right);
      }
    }

    /// <summary>
    /// 2つの木sとtを連結します。sの右端とtの左端を接続します。
    /// </summary>
    private Node? Merge(Node? s, Node? t)
    {
      if (s == null) return t;
      if (t == null) return s;

      while (s?.Ch[1] != null) s = s.Ch[1];
      Splay(s!);

      s!.Ch[1] = t;
      if (t != null) t.P = s;
      return Update(s);
    }

    /// <summary>
    /// 4つの木を順番に連結します。Linkメソッド内で使用されます。
    /// </summary>
    private Node? Merge(Node? n1, Node? n2, Node? n3, Node? n4)
    {
      return Merge(n1, Merge(n2, Merge(n3, n4)));
    }

    /// <summary>
    /// 指定されたノードを根とする部分木のサイズを取得します。
    /// </summary>
    private int GetSize(Node? t) => t?.Sz ?? 0;

    /// <summary>
    /// 指定されたノードの情報を（子の情報に基づいて）更新します。
    /// </summary>
    private Node Update(Node t)
    {
      t.Sum = _opETT.Identity;
      if (t.Ch[0] != null) t.Sum = _opETT.Operate(t.Sum, t.Ch[0]!.Sum);
      if (t.L == t.R) t.Sum = _opETT.Operate(t.Sum, t.Val);
      if (t.Ch[1] != null) t.Sum = _opETT.Operate(t.Sum, t.Ch[1]!.Sum);

      t.Sz = GetSize(t.Ch[0]) + ((t.L == t.R) ? 1 : 0) + GetSize(t.Ch[1]);

      t.ChildEdgeConnected = (t.Ch[0]?.ChildEdgeConnected ?? false) || t.EdgeConnected || (t.Ch[1]?.ChildEdgeConnected ?? false);

      t.ChildExact = (t.Ch[0]?.ChildExact ?? false) || t.Exact || (t.Ch[1]?.ChildExact ?? false);
      return t;
    }

    /// <summary>
    /// 遅延評価された情報を子ノードに伝播します。（現在は未実装）
    /// </summary>
    private void Push(Node t)
    {
      // 将来遅延評価が必要な場合のためのプレースホルダ
    }

    /// <summary>
    /// Splay Treeの回転操作を行います。
    /// </summary>
    private void Rotate(Node t, bool isTLeftChildOfParent)
    {
      Node x = t.P!;
      Node? y = x.P;

      x.Ch[isTLeftChildOfParent ? 0 : 1] = t.Ch[isTLeftChildOfParent ? 1 : 0];
      if (t.Ch[isTLeftChildOfParent ? 1 : 0] != null)
      {
        t.Ch[isTLeftChildOfParent ? 1 : 0]!.P = x;
      }

      t.Ch[isTLeftChildOfParent ? 1 : 0] = x;
      x.P = t;

      Update(x);
      Update(t);

      t.P = y;
      if (y != null)
      {
        if (y.Ch[0] == x) y.Ch[0] = t;
        else if (y.Ch[1] == x) y.Ch[1] = t;
      }
    }

    /// <summary>
    /// 指定されたノードを木の根に移動させるSplay操作。
    /// </summary>
    private void Splay(Node t)
    {
      Push(t);

      while (!t.IsRoot())
      {
        Node q = t.P!;
        if (q.IsRoot())
        {
          Push(q); Push(t);
          Rotate(t, t == q.Ch[0]);
        }
        else
        {
          Node r = q.P!;
          Push(r); Push(q); Push(t);
          bool qIsLeftChild = (q == r.Ch[0]);
          bool tIsLeftChild = (t == q.Ch[0]);

          if (qIsLeftChild == tIsLeftChild)
          {
            Rotate(q, qIsLeftChild);
            Rotate(t, tIsLeftChild);
          }
          else
          {
            Rotate(t, tIsLeftChild);
            Rotate(t, qIsLeftChild);
          }
        }
      }
    }

    /// <summary>
    /// デバッグ用に木構造（ノードのL-Rペア）を標準エラーストリームに出力します。
    /// </summary>
    public void DebugTree(Node? t)
    {
      if (t == null) return;
      DebugTree(t.Ch[0]);
      System.Diagnostics.Debug.Write($"{t.L}-{t.R} ");
      DebugTree(t.Ch[1]);
    }

    /// <summary>
    /// 指定された頂点sが属する連結成分のサイズを取得します。
    /// </summary>
    public int GetTreeSize(int s)
    {
      Node t = GetNode(s, s);
      Splay(t);
      return t.Sz;
    }

    /// <summary>
    /// 2つの頂点が同じ連結成分に属するかどうかを判定します。
    /// </summary>
    public bool AreSame(int s, int t)
    {
      return Same(GetNode(s, s), GetNode(t, t));
    }

    /// <summary>
    /// EulerTourTreeのサイズ（頂点数）を再設定し、内部構造を初期化します。
    /// </summary>
    public void SetSize(int newSize)
    {
      _ptr = new List<Dictionary<int, Node>>(newSize);
      for (int i = 0; i < newSize; i++)
      {
        _ptr.Add(new Dictionary<int, Node>());
        _ptr[i][i] = new Node(i, i, _opETT.Identity);
      }
    }

    /// <summary>
    /// 指定された頂点sに関連付けられた値をxで更新します（現在の値とxを集約）。
    /// </summary>
    public void UpdateValue(int s, T x)
    {
      Node t = GetNode(s, s);
      Splay(t);
      t.Val = _opETT.Operate(t.Val, x);
      Update(t);
    }

    /// <summary>
    /// 頂点sを含むコンポーネント内の「Exact」なエッジを処理し、アクションgに渡します。
    /// </summary>
    public void EdgeUpdate(int s, Action<int, int> g)
    {
      Node t = GetNode(s, s);
      Splay(t);

      Action<Node>? dfs = null;
      dfs = (Node curr) =>
      {
        Debug.Assert(curr != null, "DFSの現在のノードがnullです。");
        if (curr.L < curr.R && curr.Exact)
        {
          Splay(curr);
          curr.Exact = false;
          Update(curr);
          g(curr.L, curr.R);
          return;
        }

        if (curr.Ch[0]?.ChildExact ?? false) dfs!(curr.Ch[0]!);
        else if (curr.Ch[1]?.ChildExact ?? false) dfs!(curr.Ch[1]!);
      };

      while (t != null && t.ChildExact)
      {
        dfs(t);
        Splay(t);
      }
    }

    /// <summary>
    /// 頂点sを含むコンポーネントで、条件fを満たす非ツリーエッジを探し、再接続を試みます。
    /// </summary>
    public bool TryReconnect(int s, Func<int, bool> f)
    {
      Node t = GetNode(s, s);
      Splay(t);

      Func<Node, bool>? dfs = null;
      dfs = (Node curr) =>
      {
        Debug.Assert(curr != null, "DFSの現在のノードがnullです。");
        if (curr.EdgeConnected)
        {
          Splay(curr);
          return f(curr.L);
        }

        if (curr.Ch[0]?.ChildEdgeConnected ?? false)
        {
          if (dfs!(curr.Ch[0]!)) return true;
        }
        if (curr.Ch[1]?.ChildEdgeConnected ?? false)
        {
          if (dfs!(curr.Ch[1]!)) return true;
        }
        return false;
      };

      while (t.ChildEdgeConnected)
      {
        if (dfs(t)) return true;
        Splay(t);
      }
      return false;
    }

    /// <summary>
    /// 指定された頂点sのEdgeConnectedフラグを更新します。
    /// </summary>
    public void EdgeConnectedUpdate(int s, bool b)
    {
      Node t = GetNode(s, s);
      Splay(t);
      t.EdgeConnected = b;
      Update(t);
    }

    /// <summary>
    /// 2つの頂点lとrの間に辺を接続します（ETTレベルでの操作）。
    /// </summary>
    public bool Link(int l, int r)
    {
      if (AreSame(l, r)) return false;
      Node nodeL = GetNode(l, l);
      Node nodeR = GetNode(r, r);
      Node edgeLR = GetNode(l, r);
      Node edgeRL = GetNode(r, l);

      Merge(Reroot(nodeL), edgeLR, Reroot(nodeR), edgeRL);
      return true;
    }

    /// <summary>
    /// 2つの頂点lとrの間の辺を切断します（ETTレベルでの操作）。
    /// </summary>
    public bool Cut(int l, int r)
    {
      if (!_ptr[l].ContainsKey(r) || !_ptr[r].ContainsKey(l)) return false;

      Node edgeLR = GetNode(l, r);
      Node edgeRL = GetNode(r, l);

      var (s, tVal, u) = Split(edgeLR, edgeRL); // tVal is a temporary name, not used
      Merge(s, u);

      _ptr[l].Remove(r);
      _ptr[r].Remove(l);

      return true;
    }

    /// <summary>
    /// 指定された頂点vを含む連結成分の、頂点pから見た部分木の合計値を取得します。
    /// </summary>
    public T GetPathSum(int p, int v)
    {
      bool cutSuccess = false;
      if (_ptr.Count > p && _ptr[p].ContainsKey(v) &&
          _ptr.Count > v && _ptr[v].ContainsKey(p))
      {
        cutSuccess = Cut(p, v);
      }

      Node tNode = GetNode(v, v);
      Splay(tNode);
      T res = tNode.Sum;

      if (cutSuccess)
      {
        Link(p, v);
      }

      return res;
    }

    /// <summary>
    /// 指定された頂点sが属する連結成分の合計値を取得します。
    /// </summary>
    public T GetComponentSum(int s)
    {
      Node t = GetNode(s, s);
      Splay(t);
      return t.Sum;
    }
  } // EulerTourTreeクラス終わり

  /// <summary>
  /// DynamicConnectivityの新しいインスタンスを初期化します。
  /// </summary>
  /// <param name="sz">グラフの頂点数。</param>
  /// <param name="monoidOperator">頂点の値に対するモノイド演算を定義したオペレータ。</param>
  public DynamicConnectivity(int sz, IMonoidOperator<T> monoidOperator)
  {
    _sz = sz;
    _operator = monoidOperator;

    _ett = new List<EulerTourTree>();
    _ett.Add(new EulerTourTree(_sz, _operator));

    _edges = new List<List<HashSet<int>>>();
    var initialLevelEdges = new List<HashSet<int>>(_sz);
    for (int i = 0; i < _sz; i++)
    {
      initialLevelEdges.Add(new HashSet<int>());
    }
    _edges.Add(initialLevelEdges);
  }

  /// <summary>
  /// 頂点sと頂点tの間に辺を追加します。
  /// </summary>
  public bool Link(int s, int t)
  {
    if (s == t) return false;
    if (_ett[0].Link(s, t)) return true;

    _edges[0][s].Add(t);
    _edges[0][t].Add(s);
    if (_edges[0][s].Count == 1) _ett[0].EdgeConnectedUpdate(s, true);
    if (_edges[0][t].Count == 1) _ett[0].EdgeConnectedUpdate(t, true);
    return false;
  }

  /// <summary>
  /// 2つの頂点sとtが同じ連結成分に属しているかどうかを判定します。
  /// </summary>
  public bool SameComponent(int s, int t)
  {
    return _ett[0].AreSame(s, t);
  }

  /// <summary>
  /// 指定された頂点sが属する連結成分のサイズを取得します。
  /// </summary>
  public int ComponentSize(int s)
  {
    return _ett[0].GetTreeSize(s);
  }

  /// <summary>
  /// 指定された頂点sに関連付けられた値をxで更新します（現在の値とxを集約）。
  /// </summary>
  public void UpdateValue(int s, T x)
  {
    _ett[0].UpdateValue(s, x);
  }

  /// <summary>
  /// 指定された頂点sが属する連結成分の合計値を取得します。
  /// </summary>
  public T GetSum(int s)
  {
    return _ett[0].GetComponentSum(s);
  }

  /// <summary>
  /// 頂点sと頂点tの間の辺を削除します。
  /// </summary>
  public bool Cut(int s, int t)
  {
    if (s == t) return false;

    for (int i = 0; i < _dep; i++)
    {
      bool sRemoved = _edges[i][s].Remove(t);
      bool tRemoved = _edges[i][t].Remove(s);
      // Removeが成功した場合のみEdgeConnectedUpdateを評価
      if (sRemoved && _edges[i][s].Count == 0) _ett[i].EdgeConnectedUpdate(s, false);
      if (tRemoved && _edges[i][t].Count == 0) _ett[i].EdgeConnectedUpdate(t, false);
    }

    for (int i = _dep - 1; i >= 0; i--)
    {
      if (_ett[i].Cut(s, t))
      {
        if (_dep - 1 == i)
        {
          _dep++;
          _ett.Add(new EulerTourTree(_sz, _operator)); // _operator を使用
          var newLevelEdges = new List<HashSet<int>>(_sz);
          for (int j = 0; j < _sz; j++) newLevelEdges.Add(new HashSet<int>());
          _edges.Add(newLevelEdges);
        }
        return !TryReconnect(s, t, i);
      }
    }
    return false;
  }

  /// <summary>
  /// レベルkで辺(s,t)が切断された後、レベルk以下のグラフで代替路を探します。
  /// </summary>
  private bool TryReconnect(int s, int t, int k)
  {
    for (int i = k; i >= 0; i--)
    {
      if (_ett[i].GetTreeSize(s) > _ett[i].GetTreeSize(t)) Swap(ref s, ref t);

      Action<int, int> g = (u, v) => _ett[i + 1].Link(u, v);
      _ett[i].EdgeUpdate(s, g);

      Func<int, bool> f = (int x) =>
      {
        foreach (var y_loop_var in new List<int>(_edges[i][x]))
        {
          var y = y_loop_var;
          if (!_edges[i][x].Contains(y)) continue;

          _edges[i][x].Remove(y);
          _edges[i][y].Remove(x);

          if (_edges[i][x].Count == 0) _ett[i].EdgeConnectedUpdate(x, false);
          if (_edges[i][y].Count == 0) _ett[i].EdgeConnectedUpdate(y, false);

          if (_ett[i].AreSame(x, y))
          {
            _edges[i + 1][x].Add(y);
            _edges[i + 1][y].Add(x);
            if (_edges[i + 1][x].Count == 1) _ett[i + 1].EdgeConnectedUpdate(x, true);
            if (_edges[i + 1][y].Count == 1) _ett[i + 1].EdgeConnectedUpdate(y, true);
          }
          else
          {
            for (int j = 0; j <= i; j++)
            {
              _ett[j].Link(x, y);
            }
            return true;
          }
        }
        return false;
      };

      if (_ett[i].TryReconnect(s, f)) return true;
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

  public static CMY operator /(CMY cmy, double scalar) => new CMY(cmy.C / scalar, cmy.M / scalar, cmy.Y / scalar);

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

public class Cell
{
  public CMY Color { get; private set; }

  public double Volume { get; private set; }

  public int Capacity { get; private set; }

  public Cell(CMY color, double volume, int capacity)
  {
    Color = color;
    Capacity = Math.Max(capacity, 0);
    Volume = Math.Min(Math.Max(volume, 0), Capacity);
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
    double c = (c1.Volume * c1.Color.C + c2.Volume * c2.Color.C) / newVolume;
    double m = (c1.Volume * c1.Color.M + c2.Volume * c2.Color.M) / newVolume;
    double y = (c1.Volume * c1.Color.Y + c2.Volume * c2.Color.Y) / newVolume;
    CMY newColor = new CMY(c, m, y);
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

public static class Program
{
  private static long Timeout => 2950;

  private static int _n;
  private static int _k;
  private static int _h;
  private static int _t;
  private static int _d;
  private static CMY[] _tubes;
  private static CMY[] _targets;

  public static void Main(string[] args)
  {
    TimeKeeper.Start();
    Input();
    Greedy();
  }

  public static void Input()
  {
    int[] buf = ReadLine().Split().Select(int.Parse).ToArray();
    (_n, _k, _h, _t, _d) = (buf[0], buf[1], buf[2], buf[3], buf[4]);

    Double[] buf2;
    _tubes = new CMY[_k];
    for (int i = 0; i < _k; i++)
    {
      buf2 = ReadLine().Split().Select(double.Parse).ToArray();
      _tubes[i] = new CMY(buf2[0], buf2[1], buf2[2]);
    }

    _targets = new CMY[_h];
    for (int i = 0; i < _h; i++)
    {
      buf2 = ReadLine().Split().Select(double.Parse).ToArray();
      _targets[i] = new CMY(buf2[0], buf2[1], buf2[2]);
    }
  }

  public static void Greedy()
  {
    // マス(i, j)とマス(i, j + 1)の間の縦の仕切りの初期状態を設定
    for (int i = 0; i < _n; i++)
    {
      for (int j = 0; j < _n - 1; j++)
      {
        // Write($"{(j % 2 != 0 ? 1 : 0)} ");
        Write($"0 ");
      }
      WriteLine();
    }

    // マス(i, j)とマス(i + 1, j)の間の横の仕切りの初期状態を設定
    for (int i = 0; i < _n - 1; i++)
    {
      for (int j = 0; j < _n; j++)
      {
        // Write("1 ");
        Write("0 ");
      }
      WriteLine();
    }

    for (int i = 0; i < _h; i++)
    {
      (double Diff, HashSet<int> Indexes) best = (double.MaxValue, new());
      for (int t1 = 0; t1 < _k; t1++)
      {
        for (int t2 = 0; t2 < _k; t2++)
        {
          var tmpCell1 = new Cell(_tubes[t1], 1, 1);
          var tmpCell2 = new Cell(_tubes[t2], 1, 1);
          var mixedCell = (tmpCell1 + tmpCell2) / 2;
          double diff = CMY.Distance(mixedCell.Color, _targets[i]).All;

          if (diff < best.Diff)
          {
            HashSet<int> indexes = new() { t1, t2 };
            Error.WriteLine($"update: {best.Diff}({string.Join(',', best.Indexes)}) -> {diff}({string.Join(',', indexes)})");
            best = (diff, indexes);
          }
        }
      }

      Error.WriteLine($"best: {best.Diff}({string.Join(',', best.Indexes)})");
      foreach (int idx in best.Indexes)
      {
        WriteLine($"1 0 0 {idx}");
      }

      // 調合した絵の具を画伯に差し出す
      // 1gより多くの絵の具をウェルに出した場合は勿体無いが廃棄する
      for (int j = 0; j < best.Indexes.Count; j++)
      {
        if (j == 0) WriteLine("2 0 0");
        else WriteLine("3 0 0");
      }
    }
  }
}
