using AtCoder;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Console;
using static Program;
using System.Collections.ObjectModel;

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

public struct Coord
{
  public int Y { get; set; }
  public int X { get; set; }

  [MethodImpl(256)]
  public Coord(int y, int x)
  {
    Y = y;
    X = x;
  }

  [MethodImpl(256)]
  public static int Distance(Coord c1, Coord c2)
  {
    return Math.Abs(c1.Y - c2.Y) + Math.Abs(c1.X - c2.X);
  }

  [MethodImpl(256)]
  public static bool operator ==(Coord c1, Coord c2)
  {
    return c1.Y == c2.Y && c1.X == c2.X;
  }

  [MethodImpl(256)]
  public static bool operator !=(Coord c1, Coord c2)
  {
    return !(c1 == c2);
  }

  [MethodImpl(256)]
  public override bool Equals(object? obj)
  {
    return obj is Coord coord && this == coord;
  }

  [MethodImpl(256)]
  public override int GetHashCode()
  {
    return HashCode.Combine(Y, X);
  }

  [MethodImpl(256)]
  public override string ToString()
  {
    return $"({Y},{X})";
  }

  [MethodImpl(256)]
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

  [MethodImpl(256)]
  public CMY(double c, double m, double y)
  {
    C = c;
    M = m;
    Y = y;
  }

  [MethodImpl(256)] public static bool operator ==(CMY cmy1, CMY cmy2) => cmy1.Equals(cmy2);

  [MethodImpl(256)] public static bool operator !=(CMY cmy1, CMY cmy2) => !(cmy1 == cmy2);

  [MethodImpl(256)] public static CMY operator +(CMY a, CMY b) => new CMY(a.C + b.C, a.M + b.M, a.Y + b.Y);

  [MethodImpl(256)] public static CMY operator -(CMY a, CMY b) => new CMY(a.C - b.C, a.M - b.M, a.Y - b.Y);

  [MethodImpl(256)] public static CMY operator *(double scalar, CMY cmy) => new CMY(scalar * cmy.C, scalar * cmy.M, scalar * cmy.Y);

  [MethodImpl(256)] public static CMY operator *(CMY cmy, double scalar) => scalar * cmy;

  [MethodImpl(256)]
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

  [MethodImpl(256)] public override bool Equals(object? obj) => obj is CMY cmy && C == cmy.C && M == cmy.M && Y == cmy.Y;

  [MethodImpl(256)] public override int GetHashCode() => HashCode.Combine(C, M, Y);

  [MethodImpl(256)] public override string ToString() => $"({C},{M},{Y})";

  [MethodImpl(256)]
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

/// <summary>
/// KD木クラス。
/// (CMY Color, int Turn) のタプルを扱う。
/// </summary>
public class KDTree
{
  /// <summary>
  /// KD木のノードを表す、KDTreeクラス専用の内部クラス。
  /// </summary>
  private class Node
  {
    /// <summary>
    /// ノードが保持するデータポイント（色とターン数）。
    /// </summary>
    public (CMY Color, int Turn) Point { get; }

    /// <summary>
    /// このノードで空間を分割する軸 (0:C, 1:M, 2:Y)。
    /// </summary>
    public int Axis { get; }

    /// <summary>
    /// 左の子ノード（このノードの分割軸において値が小さい側）。
    /// </summary>
    public Node Left { get; set; }

    /// <summary>
    /// 右の子ノード（このノードの分割軸において値が大きい側）。
    /// </summary>
    public Node Right { get; set; }

    /// <summary>
    /// Nodeクラスの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="point">ノードが保持するデータポイント。</param>
    /// <param name="axis">このノードでの分割軸。</param>
    public Node((CMY Color, int Turn) point, int axis)
    {
      Point = point;
      Axis = axis;
    }
  }

  /// <summary>
  /// KD木を構成する点の内部配列。構築後は変更されません。
  /// </summary>
  private readonly (CMY Color, int Turn)[] points;

  /// <summary>
  /// KD木のルートノード。
  /// </summary>
  private Node Root { get; }

  /// <summary>
  /// KDTreeクラスの新しいインスタンスを初期化し、指定された点のリストから木を構築します。
  /// </summary>
  /// <param name="initialPoints">木の構築に使用する点のリスト。(CMY Color, int Turn)のタプル形式です。</param>
  public KDTree(IList<(CMY Color, int Turn)> initialPoints)
  {
    this.points = initialPoints.ToArray();
    this.Root = Build(0, this.points.Length - 1, 0);
  }

  /// <summary>
  /// 指定された範囲の点を使用して、再帰的にKD木を構築します。
  /// </summary>
  /// <param name="left">処理範囲の開始インデックス。</param>
  /// <param name="right">処理範囲の終了インデックス。</param>
  /// <param name="depth">現在の木の深さ。分割軸の決定に使用します。</param>
  /// <returns>構築された部分木のルートノード。</returns>
  private Node Build(int left, int right, int depth)
  {
    if (left > right) return null;
    int axis = depth % 3;
    int mid = left + (right - left) / 2;
    FindMedianAndPartition(left, right, mid, axis);
    var node = new Node(this.points[mid], axis);
    node.Left = Build(left, mid - 1, depth + 1);
    node.Right = Build(mid + 1, right, depth + 1);
    return node;
  }

  /// <summary>
  /// 指定範囲内のk番目に小さい要素（中央値）を見つけ、その要素を基準に配列を分割します。
  /// この操作はインプレース（元の配列を直接変更）で行われます。
  /// </summary>
  /// <param name="left">処理範囲の開始インデックス。</param>
  /// <param name="right">処理範囲の終了インデックス。</param>
  /// <param name="k">見つけたい要素のインデックス（中央値のインデックス）。</param>
  /// <param name="axis">比較に使用する次元 (0:C, 1:M, 2:Y)。</param>
  private void FindMedianAndPartition(int left, int right, int k, int axis)
  {
    while (left < right)
    {
      int pivotIndex = Partition(left, right, axis);
      if (pivotIndex == k) return;
      if (pivotIndex < k) left = pivotIndex + 1;
      else right = pivotIndex - 1;
    }
  }

  /// <summary>
  /// 配列の指定範囲を、ピボット要素を基準に分割します。
  /// ピボットより小さいすべての要素がピボットの左に来るように配置します。
  /// </summary>
  /// <param name="left">処理範囲の開始インデックス。</param>
  /// <param name="right">処理範囲の終了インデックス。この位置の要素がピボットとして使われます。</param>
  /// <param name="axis">比較に使用する次元。</param>
  /// <returns>分割後のピボットの最終的なインデックス。</returns>
  private int Partition(int left, int right, int axis)
  {
    int pivotIndex = right;
    var pivotValue = this.points[pivotIndex];
    int storeIndex = left;
    Swap(pivotIndex, right);
    for (int i = left; i < right; i++)
    {
      if (GetValueByAxis(this.points[i].Color, axis) < GetValueByAxis(pivotValue.Color, axis))
      {
        Swap(i, storeIndex);
        storeIndex++;
      }
    }
    Swap(storeIndex, right);
    return storeIndex;
  }

  /// <summary>
  /// 内部配列の2つの要素を交換します。
  /// </summary>
  /// <param name="a">交換する要素のインデックス。</param>
  /// <param name="b">交換するもう一方の要素のインデックス。</param>
  [MethodImpl(256)]
  private void Swap(int a, int b)
  {
    var temp = this.points[a];
    this.points[a] = this.points[b];
    this.points[b] = temp;
  }

  /// <summary>
  /// 指定された色とターン数上限に合致する、最も近い点（最近傍点）を探します。
  /// 距離が同じ候補が複数ある場合は、ターン数が最も小さいものが選択されます。
  /// </summary>
  /// <param name="targetColor">検索の基準となる目標のCMY色。</param>
  /// <param name="maxTurn">許容される最大のターン数。この値以下のターンを持つ点のみが検索対象となります。</param>
  /// <returns>条件に合致する最も近い点。見つからない場合はnullを返します。</returns>
  public (CMY Color, int Turn)? FindNearest(CMY targetColor, int maxTurn)
  {
    if (Root == null) return null;
    Node? bestNode = null;
    double bestDist = double.MaxValue;
    FindNearest(Root, targetColor, maxTurn, ref bestNode, ref bestDist);
    return bestNode?.Point;
  }

  /// <summary>
  /// 最近傍点を探索するための再帰的なヘルパーメソッド。
  /// 候補の更新は、(1)より距離が近い場合、または(2)距離が同じでターン数が小さい場合に行われます。
  /// </summary>
  /// <param name="currentNode">現在訪問中のノード。</param>
  /// <param name="targetColor">目標のCMY色。</param>
  /// <param name="maxTurn">許容される最大のターン数。</param>
  /// <param name="bestNode">現在見つかっている最近傍ノードへの参照。</param>
  /// <param name="bestDist">現在見つかっている最近傍点までの距離（の2乗）への参照。</param>
  private void FindNearest(Node currentNode, CMY targetColor, int maxTurn, ref Node? bestNode, ref double bestDist)
  {
    if (currentNode == null) return;

    if (currentNode.Point.Turn <= maxTurn)
    {
      double d = GetSquaredDistance(targetColor, currentNode.Point.Color);

      bool shouldUpdate = false;
      if (bestNode == null || d < bestDist)
      {
        shouldUpdate = true;
      }
      else if (d == bestDist && currentNode.Point.Turn < bestNode.Point.Turn)
      {
        shouldUpdate = true;
      }

      if (shouldUpdate)
      {
        bestDist = d;
        bestNode = currentNode;
      }
    }

    int axis = currentNode.Axis;
    double targetVal = GetValueByAxis(targetColor, axis);
    double nodeVal = GetValueByAxis(currentNode.Point.Color, axis);
    Node goodSide = (targetVal < nodeVal) ? currentNode.Left : currentNode.Right;
    Node badSide = (targetVal < nodeVal) ? currentNode.Right : currentNode.Left;
    FindNearest(goodSide, targetColor, maxTurn, ref bestNode, ref bestDist);
    double distToPlaneSq = (targetVal - nodeVal) * (targetVal - nodeVal);
    if (distToPlaneSq < bestDist)
    {
      FindNearest(badSide, targetColor, maxTurn, ref bestNode, ref bestDist);
    }
  }

  /// <summary>
  /// CMY色の指定された軸の値を取得します。
  /// </summary>
  /// <param name="color">値を取得する対象のCMY色。</param>
  /// <param name="axis">軸 (0:C, 1:M, 2:Y)。</param>
  /// <returns>指定された軸の成分値。</returns>
  [MethodImpl(256)]
  private static double GetValueByAxis(CMY color, int axis)
  {
    return axis == 0 ? color.C : (axis == 1 ? color.M : color.Y);
  }

  /// <summary>
  /// 2つのCMY色間のユークリッド距離の2乗を計算します。
  /// 平方根の計算を省略することで、距離の大小比較を高速に行えます。
  /// </summary>
  /// <param name="p1">1つ目のCMY色。</param>
  /// <param name="p2">2つ目のCMY色。</param>
  /// <returns>2点間の距離の2乗。</returns>
  [MethodImpl(256)]
  public static double GetSquaredDistance(CMY p1, CMY p2)
  {
    double dc = p1.C - p2.C;
    double dm = p1.M - p2.M;
    double dy = p1.Y - p2.Y;
    return dc * dc + dm * dm + dy * dy;
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

  [MethodImpl(256)]
  public override bool Equals(object? obj) => obj is Cell cell
    && Color.Equals(cell.Color)
    && Volume == cell.Volume
    && Capacity == cell.Capacity;

  public override int GetHashCode() => HashCode.Combine(Color, Volume, Capacity);

  [MethodImpl(256)] public static bool operator ==(Cell c1, Cell c2) => c1.Equals(c2);

  [MethodImpl(256)] public static bool operator !=(Cell c1, Cell c2) => !(c1 == c2);

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

  [MethodImpl(256)] public override string ToString() => $"{Color}({Volume}/{Capacity})";

  public void Deconstruct(out CMY color, out double capacity, out double volume)
  {
    color = Color;
    capacity = Capacity;
    volume = Volume;
  }
}

public class Grid
{
  protected static readonly int[] _dy = { -1, 0, 1, 0 };
  protected static readonly int[] _dx = { 0, 1, 0, -1 };
  protected static int _nextWellId = 1;

  protected Cell[,] _cells;
  protected int[,] _wellId;
  protected Dictionary<int, HashSet<Coord>> _wellsDict;
  protected bool[,] _verticalDividers;
  protected bool[,] _horizontalDividers;
  protected bool[,] _savedVerticalDividers;
  protected bool[,] _savedHorizontalDividers;

  public int Height => _wellId.GetLength(0);
  public int Width => _wellId.GetLength(1);

  public IReadOnlyCollection<int> AvailableWellIds => _wellsDict.Keys;

  public Grid(int size, bool allDividersUp = false) : this(size, size, allDividersUp) { }

  public Grid(int height, int width, bool allDividersUp = false)
  {
    _cells = new Cell[height, width];
    for (int i = 0; i < height; i++)
    {
      for (int j = 0; j < width; j++)
      {
        _cells[i, j] = new Cell(new CMY(0, 0, 0), 0, 1);
      }
    }

    _wellId = new int[height, width];
    _wellsDict = new();

    _verticalDividers = new bool[height, width - 1];
    _horizontalDividers = new bool[height - 1, width];

    if (allDividersUp)
    {
      for (int i = 0; i < height; i++)
      {
        for (int j = 0; j < width - 1; j++)
        {
          _verticalDividers[i, j] = true;
        }
      }
      for (int i = 0; i < height - 1; i++)
      {
        for (int j = 0; j < width; j++)
        {
          _horizontalDividers[i, j] = true;
        }
      }

      UpdateWellsId(new List<Coord>());
    }
    else
    {
      UpdateWellsId(new List<Coord>() { new Coord(0, 0) });
    }

    SaveDividers();
  }

  public Cell this[int y, int x]
  {
    get
    {
      return GetCell(y, x);
    }
  }

  // セルの情報を取得する
  [MethodImpl(256)]
  public Cell GetCell(Coord coord)
  {
    return GetCell(coord.Y, coord.X);
  }

  public Cell GetCell(int y, int x)
  {
    var cell = _cells[y, x];
    return new Cell(cell.Color, cell.Volume, cell.Capacity);
  }

  // ウェルに関する情報(ウェルに含まれるセルを集約した情報)を取得する
  [MethodImpl(256)]
  public Cell GetWell(int wellId)
  {
    return GetWell(_wellsDict[wellId].First());
  }

  [MethodImpl(256)]
  public Cell GetWell(Coord coord)
  {
    return GetWell(coord.Y, coord.X);
  }

  public Cell GetWell(int y, int x)
  {
    var cell = _cells[y, x];
    var group = _wellsDict[_wellId[y, x]];
    return new Cell(cell.Color, cell.Volume * group.Count, cell.Capacity * group.Count);
  }

  [MethodImpl(256)]
  public int GetWellId(Coord coord) => GetWellId(coord.Y, coord.X);

  [MethodImpl(256)]
  public int GetWellId(int y, int x) => _wellId[y, x];

  [MethodImpl(256)]
  public bool IsSameWell(Coord c1, Coord c2) => IsSameWell(c1.Y, c1.X, c2.Y, c2.X);

  [MethodImpl(256)]
  public bool IsSameWell(int y1, int x1, int y2, int x2) => _wellId[y1, x1] == _wellId[y2, x2];

  // ウェルに属するセルの一覧を取得する
  [MethodImpl(256)]
  public IReadOnlyCollection<Coord> GetCellsInWell(int wellId)
  {
    return _wellsDict[wellId];
  }

  // 指定したセル(が属するウェル)に絵の具を追加する
  public void Add(Coord coord, CMY color)
  {
    var group = _wellsDict[_wellId[coord.Y, coord.X]];
    var startCell = _cells[coord.Y, coord.X];

    var add = new Cell(
      color,
      Math.Min(1.0 / group.Count, (double)startCell.Capacity - startCell.Volume),
      0
    );

    foreach (Coord c in group)
    {
      _cells[c.Y, c.X] += add;
    }
  }

  // 指定したセル(が属するウェル)に溜まっている絵の具を廃棄する
  public void Discard(Coord coord, bool strict)
  {
    var group = _wellsDict[_wellId[coord.Y, coord.X]];

    // strictが有効で取り出し量が不足している場合は例外
    if (strict && !CanDiscardStrict(coord))
    {
      throw new InvalidOperationException("選択したウェルから取り出せる絵の具の量の上限を超えています");
    }

    foreach (Coord c in group)
    {
      _cells[c.Y, c.X] = new Cell(
        _cells[c.Y, c.X].Color,
        _cells[c.Y, c.X].Volume - 1.0 / group.Count,
        _cells[c.Y, c.X].Capacity);
    }
  }

  // 指定した量だけ絵の具を取り出す操作が厳密に行えるか判定する
  [MethodImpl(256)]
  public bool CanDiscardStrict(Coord coord) => CanDiscardStrict(GetWell(coord));

  [MethodImpl(256)]
  public bool CanDiscardStrict(int wellId) => CanDiscardStrict(GetWell(wellId));

  [MethodImpl(256)]
  private bool CanDiscardStrict(Cell well) => well.Volume >= 1 - 1e-6;

  // 基準となるセルから見て縦方向なら右側、横方向なら下側の仕切りが上がっているか確認する
  [MethodImpl(256)]
  public bool IsDividerUp(Coord coord, bool isVertical)
  {
    return isVertical
      ? _verticalDividers[coord.Y, coord.X]
      : _horizontalDividers[coord.Y, coord.X];
  }

  // 基準となるセルから見て縦方向なら右側、横方向なら下側の仕切りの状態を変更する 
  public void SwitchDivider(Coord c1, bool isVertical)
  {
    // 仕切りの状態を変更する
    Coord c2;
    if (isVertical)
    {
      _verticalDividers[c1.Y, c1.X] ^= true;
      c2 = new Coord(c1.Y, c1.X + 1);
    }
    else
    {
      _horizontalDividers[c1.Y, c1.X] ^= true;
      c2 = new Coord(c1.Y + 1, c1.X);
    }

    // 連結状態の変更前に隣り合うマスがそれぞれ属するウェルの情報を取得しておく
    var oldWell1 = GetWell(_wellId[c1.Y, c1.X]);
    var oldWell2 = GetWell(_wellId[c2.Y, c2.X]);

    // ウェルのIDを更新する
    (var newIds, var oldIds) = UpdateWellsId(new Coord[] { c1, c2 });

    // 新しく発行されたIDのリストを参考に、各セルの状態を操作後にあるべき形にする
    // 異なるウェルが連結になった場合は色を混ぜる処理を行い対象のセルに全て反映させる
    if (oldIds.Count == 2 && newIds.Count == 1)
    {
      // 更新前に取得したウェルを合体させる
      var newWell = oldWell1 + oldWell2;

      // 新しく作られたウェルに属するセルの情報を構築し反映させる
      var cell = new Cell(
        newWell.Color,
        newWell.Volume / _wellsDict[newIds[0]].Count,
        newWell.Capacity / _wellsDict[newIds[0]].Count
      );
      foreach (Coord coord in _wellsDict[newIds[0]])
      {
        _cells[coord.Y, coord.X] = cell;
      }
    }
  }

  // 引数で渡された始点から順にBFSを行い、複数のウェルのIDを新しいものに更新する
  // 操作によって新しく発行されたID、廃棄されたIDをそれぞれリストにして返す
  protected (List<int> newIds, List<int> oldIds) UpdateWellsId(IList<Coord> startPoints)
  {
    // 空のリストが渡された場合は全てのセルを始点として設定する
    if (startPoints.Count == 0)
    {
      for (int i = 0; i < Height; i++)
      {
        for (int j = 0; j < Width; j++)
        {
          startPoints.Add(new Coord(i, j));
        }
      }
    }

    // 一つずつ始点を決めて、BFSで始点と連結なマスを探していく
    List<int> newIds = new();
    List<int> oldIds = new();
    HashSet<Coord> visited = new();
    foreach (Coord sp in startPoints)
    {
      // 探索済みなら始点を選び直す
      if (visited.Contains(sp)) continue;

      // 新しいIDの発行・探索が確定するのでそれぞれ必要な情報を追加
      newIds.Add(_nextWellId);
      visited.Add(sp);

      // BFSで連結なマスを探索していき1つのグループの情報を完成させる
      Queue<Coord> q = new();
      q.Enqueue(sp);
      while (q.Count >= 1)
      {
        Coord cp = q.Dequeue();
        if (!oldIds.Contains(_wellId[cp.Y, cp.X])) oldIds.Add(_wellId[cp.Y, cp.X]);

        _wellId[cp.Y, cp.X] = _nextWellId;
        if (!_wellsDict.ContainsKey(_nextWellId)) _wellsDict[_nextWellId] = new();
        _wellsDict[_nextWellId].Add(cp);

        for (int i = 0; i < _dy.Length; i++)
        {
          Coord ep = new(cp.Y + _dy[i], cp.X + _dx[i]);
          if (ep.Y < 0 || ep.Y >= Height || ep.X < 0 || ep.X >= Width) continue;
          if (i == 0 && IsDividerUp(ep, false)) continue;
          if (i == 1 && IsDividerUp(cp, true)) continue;
          if (i == 2 && IsDividerUp(cp, false)) continue;
          if (i == 3 && IsDividerUp(ep, true)) continue;
          if (visited.Contains(ep)) continue;
          visited.Add(ep);
          q.Enqueue(ep);
        }
      }

      // BFSが終わった時点で1つのグループの情報が確定するので次に付与するグループIDをずらす
      _nextWellId++;
    }

    // 使われなくなったウェルのIDに結び付くリストを削除する
    foreach (int oldId in oldIds)
    {
      _wellsDict.Remove(oldId);
    }

    return (newIds, oldIds);
  }

  // 現在の仕切りの状態をセーブする
  public void SaveDividers()
  {
    _savedVerticalDividers = new bool[_verticalDividers.GetLength(0), _verticalDividers.GetLength(1)];
    for (int i = 0; i < _verticalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < _verticalDividers.GetLength(1); j++)
      {
        _savedVerticalDividers[i, j] = _verticalDividers[i, j];
      }
    }
    _savedHorizontalDividers = new bool[_horizontalDividers.GetLength(0), _horizontalDividers.GetLength(1)];
    for (int i = 0; i < _horizontalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < _horizontalDividers.GetLength(1); j++)
      {
        _savedHorizontalDividers[i, j] = _horizontalDividers[i, j];
      }
    }
  }

  // セーブされた仕切りの状態を出力する
  public void PrintSavedDividers()
  {
    PrintDividers(_savedVerticalDividers, _savedHorizontalDividers);
  }

  // 現在の仕切りの状態を出力する
  public void PrintCurrentDividers()
  {
    PrintDividers(_verticalDividers, _horizontalDividers);
  }

  private void PrintDividers(in bool[,] vertical, in bool[,] horizontal)
  {
    for (int i = 0; i < vertical.GetLength(0); i++)
    {
      for (int j = 0; j < vertical.GetLength(1); j++)
      {
        Write($"{(vertical[i, j] ? 1 : 0)} ");
      }
      WriteLine();
    }
    for (int i = 0; i < horizontal.GetLength(0); i++)
    {
      for (int j = 0; j < horizontal.GetLength(1); j++)
      {
        Write($"{(horizontal[i, j] ? 1 : 0)} ");
      }
      WriteLine();
    }
  }

  // 全セルの情報を一覧表示する
  public void PrintGroupStatus()
  {
    for (int i = 0; i < Height; i++)
    {
      for (int j = 0; j < Width; j++)
      {
        Error.WriteLine($"({i},{j}): {_cells[i, j]}[{_wellId[i, j]}]");
      }
    }
  }
}

public class Palette : Grid
{
  public static readonly int Size;
  public static readonly int MaxTurns;
  public static readonly int Cost;

  static Palette()
  {
    Size = N;
    MaxTurns = T;
    Cost = D;
  }

  private List<(int[] Log, int VisualizedScore, double EvaluatedScore)> _logs;
  private List<(CMY Color, double Deviation)> _madePaints;

  public int OpCount { get; private set; }
  public int AddCount { get; private set; }
  public (double Definite, double Tentative) Deviation { get; private set; }
  public int TargetId { get; private set; }

  public int VisualizedScore => (int)(1 + Cost * (AddCount - _madePaints.Count) + Math.Round(1e4 * Deviation.Definite));
  public double EvaluatedScore => 1 + Cost * Math.Max(AddCount - Targets.Count, 0) + Math.Round(1e4 * Deviation.Tentative);

  public IReadOnlyList<(CMY Color, double Deviation)> MadePaints => _madePaints;

  public Palette() : base(Size, false)
  {
    _logs = new();
    _madePaints = new();

    OpCount = 0;
    AddCount = 0;
    Deviation = (0, CMY.MaxDistance * Targets.Count);
    TargetId = 0;
  }

  public Palette(int[,] verticalDividers, int[,] horizontalDividers) : this()
  {
    for (int i = 0; i < verticalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < verticalDividers.GetLength(1); j++)
      {
        if ((verticalDividers[i, j] == 1) ^ IsDividerUp(new Coord(i, j), true))
        {
          SwitchDivider(new Coord(i, j), true);
        }
      }
    }
    for (int i = 0; i < horizontalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < horizontalDividers.GetLength(1); j++)
      {
        if ((horizontalDividers[i, j] == 1) ^ IsDividerUp(new Coord(i, j), false))
        {
          SwitchDivider(new Coord(i, j), false);
        }
      }
    }
    SaveDividers();
  }

  public Palette(bool[,] verticalDividers, bool[,] horizontalDividers) : this()
  {
    for (int i = 0; i < verticalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < verticalDividers.GetLength(1); j++)
      {
        if (verticalDividers[i, j] ^ IsDividerUp(new Coord(i, j), true))
        {
          SwitchDivider(new Coord(i, j), true);
        }
      }
    }
    for (int i = 0; i < horizontalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < horizontalDividers.GetLength(1); j++)
      {
        if (horizontalDividers[i, j] ^ IsDividerUp(new Coord(i, j), false))
        {
          SwitchDivider(new Coord(i, j), false);
        }
      }
    }
    SaveDividers();
  }

  // ターゲットとなる色を調合して差し出す一連の操作を行ったときのスコアの悪化量を取得する
  [MethodImpl(256)]
  public double GetScoreDeltaByAddition(int addCount, CMY color)
  {
    return Cost * addCount + 1e4 * CMY.Distance(color, Targets[TargetId]).All;
  }

  // ターン数が残っていて操作可能な状態かどうか判定する
  [MethodImpl(256)] public bool IsOperatable() => OpCount <= MaxTurns;

  // ゲームの終了条件を満たしているか確認する
  [MethodImpl(256)] public bool IsSubmittable() => TargetId >= Targets.Count;

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
        base.Discard(new Coord(args[1], args[2]), false);
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
    base.Add(coord, Tubes[tubeId]);
    AddCount++;
  }

  // 操作2に対応するメソッド
  private void Give(Coord coord)
  {
    var cell = GetCell(coord);
    base.Discard(coord, true);
    double d = CMY.Distance(cell.Color, Targets[TargetId]).All;
    _madePaints.Add((cell.Color, d));
    Deviation = (Deviation.Definite + d, Deviation.Tentative - (CMY.MaxDistance - d));

    TargetId++;
  }

  // 操作4に対応するメソッド
  private void SwitchDivider(Coord c1, Coord c2)
  {
    if (c1.Y == c2.Y && c1.X + 1 == c2.X) SwitchDivider(c1, true);
    else if (c1.Y + 1 == c2.Y && c1.X == c2.X) SwitchDivider(c1, false);
    else throw new ArgumentException($"セル{c2}はセル{c1}の右隣または下隣には存在していません");
  }

  public void Print(bool verbose = false)
  {
    // 保存された仕切りの状態を出力
    PrintSavedDividers();

    // ログを出力
    foreach ((int[] log, int v, double e) in _logs)
    {
      WriteLine(string.Join(' ', log));
      if (verbose) Error.WriteLine($"Visualized: {v}, Evaluated: {e}");
    }

    if (verbose)
    {
      for (int i = 0; i < MadePaints.Count; i++)
      {
        Error.Write($"delta: {MadePaints[i].Deviation}\t");
        Error.WriteLine($"target[{i}]: {Targets[i]}\t<---> made[{i}]: {MadePaints[i].Color}");
      }
    }
  }
}

public static class Program
{
  public static readonly long Timeout = 2950;
  // public static readonly int[] PrecomputableCount = new int[] { -1, -1, -1, -1, 10, 8, 7, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 5, 4, 4 };
  public static readonly (int MixingCount, int CalcCount)[] PrecomputeData = new (int, int)[]
  {
    // (-1, -1),
    // (-1, -1),
    // (-1, -1),
    // (-1, -1),
    // (8, 87380),
    // (7, 97655),
    // (6, 55986),
    // (6, 137256),
    // (5, 37448),
    // (5, 66429),
    // (5, 111110),
    // (5, 177155),
    // (4, 22620),
    // (4, 30940),
    // (4, 41370),
    // (4, 54240),
    // (4, 69904),
    // (4, 88740),
    // (4, 111150),
    // (4, 137560),
    // (4, 168420)

    (-1, -1),
    (-1, -1),
    (-1, -1),
    (-1, -1),
    (9, 349524),
    (8, 488280),
    (7, 335922),
    (6, 137256),
    (6, 299592),
    (6, 597870),
    (6, 1111110),
    (5, 177155),
    (5, 271452),
    (5, 402233),
    (5, 579194),
    (5, 813615),
    (5, 1118480),
    (4, 88740),
    (4, 111150),
    (4, 137560),
    (4, 168420)
  };

  public static readonly int[] DY = new int[] { -1, 0, 1, 0 };
  public static readonly int[] DX = new int[] { 0, 1, 0, -1 };
  private static readonly Random _rand = new();
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
    // Greedy();
    // Greedy2();
    // Greedy3();
    Greedy4();
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
    // 仕切りの設計
    bool[,] verticalDividers = new bool[N, N - 1];
    verticalDividers[0, 15] = true;
    verticalDividers[1, 15] = true;
    bool[,] horizontalDividers = new bool[N - 1, N];
    for (int i = 0; i < 16; i++) horizontalDividers[1, i] = true;

    var plt = new Palette(verticalDividers, horizontalDividers);
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
      int maxRandomMicroSec = 1000; // 1回のランダムな探索にかけられる実行時間
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
            int tubeId = _rand.Next(0, Tubes.Count);
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
            // Error.WriteLine($"update: {best.Delta}({string.Join(',', best.Tubes)}) -> {delta}({string.Join(',', selectedTubes)})");
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

  public static void Greedy2()
  {
    // 時間いっぱい適当な盤面を作っていき最適なものを選択する
    var bestPalette = new Palette();
    int searchCount = 0;
    while (TimeKeeper.ElapsedMillisec() < Timeout)
    {
      searchCount++;

      // 仕切りの設計
      // 全てランダムに配置する
      int dividerUpPercent = _rand.Next(60, 80);
      bool[,] verticalDividers = new bool[N, N - 1];
      bool[,] horizontalDividers = new bool[N - 1, N];
      for (int i = 0; i < N; i++)
      {
        for (int j = 0; j < N - 1; j++)
        {
          verticalDividers[i, j] = _rand.Next(0, 100) < dividerUpPercent;
          horizontalDividers[j, i] = _rand.Next(0, 100) < dividerUpPercent;
        }
      }

      // 全方向が仕切りで囲まれているセルが存在しないように仕切りの状態を調整する
      for (int cy = 0; cy < N; cy++)
      {
        for (int cx = 0; cx < N; cx++)
        {
          bool isEnclosed = true;
          try { if (!verticalDividers[cy, cx]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy, cx]) isEnclosed = false; } catch { }
          try { if (!verticalDividers[cy, cx - 1]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy - 1, cx]) isEnclosed = false; } catch { }
          if (isEnclosed)
          {
            try { verticalDividers[cy, cx] ^= true; }
            catch { verticalDividers[cy, cx - 1] ^= true; }
          }
        }
      }

      // パレットの作成
      var plt = new Palette(verticalDividers, horizontalDividers);

      // 全てのセルを対象に1gずつランダムな絵の具をセットしていく
      for (int i = 0; i < Palette.Size; i++)
      {
        for (int j = 0; j < Palette.Size; j++)
        {
          plt.Operate(new int[] { 1, i, j, _rand.Next(0, Tubes.Count) });
        }
      }

      // 仕切りを破壊する起点となるセルのリストを作成
      List<Coord> dividerBreakOrigins = new();
      for (int i = 0; i < N - 1; i++)
      {
        for (int j = 0; j < N - 1; j++)
        {
          dividerBreakOrigins.Add(new Coord(i, j));
        }
      }
      dividerBreakOrigins.Shuffle();

      while (!plt.IsSubmittable() && TimeKeeper.ElapsedMillisec() < Timeout)
      {
        // // 全てのマスを見て一番誤差の小さい色が置かれているマスを特定する
        // (double Delta, Coord Coord) best = (double.MaxValue, new Coord(-1, -1));
        // for (int i = 0; i < N; i++)
        // {
        //   for (int j = 0; j < N; j++)
        //   {
        //     if (!plt.CanDiscardStrict(new Coord(i, j))) continue;
        //     double delta = CMY.Distance(plt[i, j].Color, Targets[plt.TargetId]).All;
        //     if (delta < best.Delta)
        //     {
        //       best = (delta, new Coord(i, j));
        //     }
        //   }
        // }

        // 全てのウェルを見て一番誤差の小さい色が置かれている場所を特定する
        (double Delta, Coord Coord) best = (double.MaxValue, new Coord(-1, -1));
        foreach (int wellId in plt.AvailableWellIds)
        {
          // ウェルから絵の具を取り出せない場合はスキップする
          if (!plt.CanDiscardStrict(wellId)) continue;

          var coord = plt.GetCellsInWell(wellId).First();
          double delta = CMY.Distance(plt[coord.Y, coord.X].Color, Targets[plt.TargetId]).All;
          if (delta < best.Delta)
          {
            best = (delta, new Coord(coord.Y, coord.X));
          }
        }

        // 絵の具を差し出す操作を実行
        plt.Operate(new int[] { 2, best.Coord.Y, best.Coord.X });

        // 絵の具を追加した回数がターゲットの総数に満たない場合は絵の具を新しく追加
        if (plt.AddCount < Targets.Count)
        {
          plt.Operate(new int[] { 1, best.Coord.Y, best.Coord.X, _rand.Next(0, Tubes.Count) });
        }
        // 新しく絵の具が追加されなくなったら仕切りを少しずつ壊していく
        else if (Targets.Count - plt.TargetId < 200 && dividerBreakOrigins.Count >= 1)
        {
          var origin = dividerBreakOrigins.Last();
          dividerBreakOrigins.RemoveAt(dividerBreakOrigins.Count - 1);

          // 縦方向の仕切りがあれば下げる
          if (plt.IsDividerUp(origin, true))
          {
            plt.Operate(new int[] { 4, origin.Y, origin.X, origin.Y, origin.X + 1 });
          }
          // 横方向の仕切りがあれば下げる
          if (plt.IsDividerUp(origin, false))
          {
            plt.Operate(new int[] { 4, origin.Y, origin.X, origin.Y + 1, origin.X });
          }
        }
      }

      // 評価スコアが元より高くなるなら更新する
      if (plt.EvaluatedScore < bestPalette.EvaluatedScore)
      {
        Error.WriteLine($"update! searchCount={searchCount} dividerUpPercent={dividerUpPercent}");
        Error.WriteLine($"{bestPalette.EvaluatedScore} -> {plt.EvaluatedScore}");
        bestPalette = plt;
      }
    }

    Error.WriteLine($"finish! searchCount={searchCount}");
    bestPalette.Print();
  }

  public static void Greedy3()
  {
    // 数ターン分、絵の具の組み合わせを全探索する
    // 作成した色から使用した絵の具の組み合わせを解決する辞書も同時に作成する
    var points = new (CMY Color, int Turn)[PrecomputeData[K].CalcCount];
    Dictionary<CMY, int[]> colorToTubeIds = new();
    int idx = 0;
    for (int mcnt = 1; mcnt <= PrecomputeData[K].MixingCount; mcnt++)
    {
      int[] tubeIds = new int[mcnt];
      CMY colorSum = tubeIds.Select((id) => Tubes[id]).Sum();

      for (int i = 0; i < (int)Math.Pow(K, mcnt); i++)
      {
        CMY color = colorSum / mcnt;
        points[idx] = (color, mcnt);
        if (!colorToTubeIds.ContainsKey(color))
        {
          colorToTubeIds[color] = new int[tubeIds.Length];
          for (int j = 0; j < tubeIds.Length; j++) colorToTubeIds[color][j] = tubeIds[j];
        }
        idx++;

        for (int pos = mcnt - 1; pos >= 0; pos--)
        {
          colorSum -= Tubes[tubeIds[pos]];
          tubeIds[pos]++;

          if (tubeIds[pos] < K)
          {
            colorSum += Tubes[tubeIds[pos]];
            break;
          }

          tubeIds[pos] = 0;
          colorSum += Tubes[tubeIds[pos]];
        }
      }
    }

    // 調合で完成した色とかかったターンの情報がまとまっているリストを渡してKD木を構築
    var kdt = new KDTree(points);

    // コストと操作可能な回数から1回の調合にかけられる操作回数
    int maxMixingCount = Math.Max(
      1,
      Math.Min(
        PrecomputeData[K].MixingCount,
        T / H / 2
      )
    );

    var bestPlt = new Palette();
    for (int mixingCount = 1; mixingCount <= maxMixingCount; mixingCount++)
    {
      var plt = new Palette();
      for (int i = 0; i < H; i++)
      {

        // KD木から最適なパターンを取得する
        // int mixingCount = Math.Min(maxMixingCount, PrecomputeData[K].MixingCount);
        var nearest = kdt.FindNearest(Targets[plt.TargetId], mixingCount);
        var tubeIds = colorToTubeIds[(CMY)nearest?.Color];

        foreach (int tubeId in tubeIds)
        {
          plt.Operate(new int[] { 1, 0, 0, tubeId });
        }

        for (int j = 0; j < tubeIds.Length; j++)
        {
          if (j == 0) plt.Operate(new int[] { 2, 0, 0 });
          else plt.Operate(new int[] { 3, 0, 0 });
        }
      }

      Error.WriteLine($"mixingCount: {mixingCount} score: {plt.EvaluatedScore}");
      if (plt.EvaluatedScore < bestPlt.EvaluatedScore)
      {
        bestPlt = plt;
      }
    }

    bestPlt.Print();
  }

  public static void Greedy4()
  {
    // 複数のKD木を格納した配列
    // [0]のKD木は全ての状態を持つ
    // [1~Length-1]のKD木は添字に対応するターン数の状態だけを管理する
    var kdtree = new KDTree[PrecomputeData[K].CalcCount + 1];

    // 数ターン分、絵の具の組み合わせを全探索する
    // 作成した色から使用した絵の具の組み合わせを解決する辞書も同時に作成する
    // var points = new (CMY Color, int Turn)[PrecomputeData[K].CalcCount];
    List<(CMY Color, int Turn)> allPoints = new();
    Dictionary<CMY, int[]> colorToTubeIds = new();
    for (int mcnt = 1; mcnt <= PrecomputeData[K].MixingCount; mcnt++)
    {
      List<(CMY Color, int Turn)> currentPoints = new();
      int[] tubeIds = new int[mcnt];
      CMY colorSum = tubeIds.Select((id) => Tubes[id]).Sum();

      for (int i = 0; i < (int)Math.Pow(K, mcnt); i++)
      {
        CMY color = colorSum / mcnt;
        allPoints.Add((color, mcnt));
        currentPoints.Add((color, mcnt));

        if (!colorToTubeIds.ContainsKey(color))
        {
          colorToTubeIds[color] = new int[tubeIds.Length];
          for (int j = 0; j < tubeIds.Length; j++) colorToTubeIds[color][j] = tubeIds[j];
        }

        for (int pos = mcnt - 1; pos >= 0; pos--)
        {
          colorSum -= Tubes[tubeIds[pos]];
          tubeIds[pos]++;

          if (tubeIds[pos] < K)
          {
            colorSum += Tubes[tubeIds[pos]];
            break;
          }

          tubeIds[pos] = 0;
          colorSum += Tubes[tubeIds[pos]];
        }
      }

      kdtree[mcnt] = new KDTree(currentPoints);
    }
    // kdtree[0] = new KDTree(allPoints);

    // 時間いっぱい探索する
    var bestPalette = new Palette();
    int searchCount = 0;
    while (TimeKeeper.ElapsedMillisec() < Timeout)
    {
      searchCount++;

      // 仕切りの設計
      // int dividerUpPercent = _rand.Next(30, 50);
      // int dividerUpPercent = 100;
      int dividerUpPercent = Math.Max(100 - searchCount, 0);
      bool[,] verticalDividers = new bool[N, N - 1];
      bool[,] horizontalDividers = new bool[N - 1, N];
      for (int i = 0; i < N; i++)
      {
        for (int j = 0; j < N - 1; j++)
        {
          verticalDividers[i, j] = _rand.Next(0, 100) < dividerUpPercent;
          horizontalDividers[j, i] = _rand.Next(0, 100) < dividerUpPercent;
        }
      }

      // 全方向が仕切りで囲まれているセルが存在しないように仕切りの状態を調整する
      for (int cy = 0; cy < N - 1; cy += 2)
      {
        for (int cx = 0; cx < N - 1; cx += 2)
        {
          if (_rand.Next(0, 2) == 0) horizontalDividers[cy, cx] = false;
          verticalDividers[cy, cx] = false;
        }
      }
      for (int cy = 1; cy < N; cy += 2)
      {
        for (int cx = 0; cx < N; cx++)
        {
          bool isEnclosed = true;
          try { if (!verticalDividers[cy, cx]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy, cx]) isEnclosed = false; } catch { }
          try { if (!verticalDividers[cy, cx - 1]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy - 1, cx]) isEnclosed = false; } catch { }
          if (isEnclosed)
          {
            int d = _rand.Next(0, 2);
            try { horizontalDividers[cy - d, cx] = false; }
            catch { horizontalDividers[cy - (d ^ 1), cx] = false; }
          }
        }
      }
      for (int cy = 0; cy < N; cy += 2)
      {
        for (int cx = 0; cx < N; cx += 2)
        {
          bool isEnclosed = true;
          try { if (!horizontalDividers[cy - 1, cx]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy, cx]) isEnclosed = false; } catch { }
          try { if (!verticalDividers[cy, cx - 1]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy - 1, cx + 1]) isEnclosed = false; } catch { }
          try { if (!verticalDividers[cy, cx + 1]) isEnclosed = false; } catch { }
          try { if (!horizontalDividers[cy, cx + 1]) isEnclosed = false; } catch { }
          if (isEnclosed)
          {
            try { verticalDividers[cy, cx + 1] = false; }
            catch { verticalDividers[cy, cx - 1] = false; }
          }
        }
      }

      // パレットの作成
      var plt = new Palette(verticalDividers, horizontalDividers);

      // 全てのウェルが埋まるまでターゲットとなる色を順に盤面で作成していく
      int wellCount = 0;
      foreach (int wellId in plt.AvailableWellIds)
      {
        var well = plt.GetWell(wellId);
        Coord representative = plt.GetCellsInWell(wellId).First();

        int searchTurn = Math.Min(well.Capacity, PrecomputeData[K].MixingCount);
        var nearest = kdtree[searchTurn].FindNearest(Targets[wellCount], searchTurn);
        var tubeIds = colorToTubeIds[(CMY)nearest?.Color];

        foreach (int tubeId in tubeIds)
        {
          plt.Operate(new int[] { 1, representative.Y, representative.X, tubeId });
        }

        wellCount++;
      }

      for (int i = 0; i < H; i++)
      {
        if (TimeKeeper.ElapsedMillisec() >= Timeout) break;
        (CMY Color, double scoreDelta, List<int[]> Operations) bestOperate = (new CMY(-1, -1, -1), double.MaxValue, new());

        // 全てのウェルを探索する
        foreach (int wellId in plt.AvailableWellIds)
        {
          var well = plt.GetWell(wellId);
          Coord representative = plt.GetCellsInWell(wellId).First();

          // 既にウェルに溜まっている絵の具を差し出す
          if (plt.CanDiscardStrict(wellId))
          {
            double scoreDelta = plt.GetScoreDeltaByAddition(0, well.Color);
            if (scoreDelta < bestOperate.scoreDelta)
            {
              bestOperate = (well.Color, scoreDelta, new() {
              new int[] { 2, representative.Y, representative.X }
            });
            }
          }

          // 空のウェルに最適な絵の具を作り出して差し出す
          if (well.Volume < 1e-6)
          {
            int searchTurn = Math.Min(well.Capacity, PrecomputeData[K].MixingCount);
            var nearest = kdtree[searchTurn].FindNearest(Targets[plt.TargetId], searchTurn);
            var tubeIds = colorToTubeIds[(CMY)nearest?.Color];

            List<int[]> operations = new();
            foreach (int tubeId in tubeIds)
            {
              operations.Add(new int[] { 1, representative.Y, representative.X, tubeId });
            }
            operations.Add(new int[] { 2, representative.Y, representative.X });

            double scoreDelta = plt.GetScoreDeltaByAddition(
              Math.Min(tubeIds.Length, Math.Max(plt.AddCount - H, 0)),
              (CMY)nearest?.Color);
            if (scoreDelta < bestOperate.scoreDelta)
            {
              bestOperate = ((CMY)nearest?.Color, scoreDelta, operations);
            }
          }

          // ウェルに1gの絵の具を追加して差し出す
          if (well.Volume + 1 < well.Capacity + 1e-6)
          {
            for (int tubeId = 0; tubeId < Tubes.Count; tubeId++)
            {
              CMY tube = Tubes[tubeId];
              var tmpCell = well + new Cell(tube, 1, 0);
              double scoreDelta = plt.GetScoreDeltaByAddition(
                Math.Min(1, Math.Max(plt.AddCount - H, 0)),
                tmpCell.Color);
              if (scoreDelta < bestOperate.scoreDelta)
              {
                bestOperate = (tmpCell.Color, scoreDelta, new() {
                  new int[] { 1, representative.Y, representative.X, tubeId },
                  new int[] { 2, representative.Y, representative.X }
                });
              }
            }
          }
        }

        // 終盤で新しい絵の具を使いづらく盤面の絵の具も限られてきた場合に仕切りを破壊する
        if (plt.TargetId >= 900 && bestOperate.scoreDelta >= 1000)
        {
          Error.WriteLine($"plt.TargetId: {plt.TargetId} bestOperate.scoreDelta: {bestOperate.scoreDelta}");

          for (int sy = 0; sy < N - 1; sy++)
          {
            for (int sx = 0; sx < N - 1; sx++)
            {
              for (int b = 0; b < 2; b++)
              {
                // 仕切りを破壊し異なるウェルを合体させてから差し出す
                Coord c1 = new(sy, sx);
                bool isVertical = (b == 0);
                Coord c2 = (isVertical ? new(c1.Y, c1.X + 1) : new(c1.Y + 1, c1.X));
                if (c2.Y < 0 || c2.Y >= N || c2.X < 0 || c2.X >= N) continue;
                if (plt.IsSameWell(c1, c2)) continue;

                var w1 = plt.GetWell(c1);
                var w2 = plt.GetWell(c2);
                var mergedWell = w1 + w2;
                if (mergedWell.Volume < 1 - 1e-6) continue;

                // CMY tube = Tubes[tubeId];
                // var tmpCell = well + new Cell(tube, 1, 0);
                double scoreDelta = plt.GetScoreDeltaByAddition(0, mergedWell.Color);
                if (scoreDelta < bestOperate.scoreDelta)
                {
                  bestOperate = (mergedWell.Color, scoreDelta, new() {
                    new int[] { 4, c1.Y, c1.X, c2.Y, c2.X },
                    new int[] { 2, c1.Y, c1.X }
                  });
                }
              }
            }
          }
        }

        // 最も良い手順に従って操作を行う
        foreach (var operation in bestOperate.Operations)
        {
          plt.Operate(operation);
        }
      }

      // 評価スコアが元より高くなるなら更新する
      if (plt.IsSubmittable() && plt.EvaluatedScore < bestPalette.EvaluatedScore)
      {
        Error.WriteLine($"update! searchCount={searchCount} dividerUpPercent={dividerUpPercent}");
        Error.WriteLine($"{bestPalette.EvaluatedScore} -> {plt.EvaluatedScore}");
        bestPalette = plt;
      }
    }

    Error.WriteLine($"searchCount: {searchCount}");
    bestPalette.Print();
    // bestPalette.Print(verbose: true);
  }
}
