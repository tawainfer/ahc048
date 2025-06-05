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
  private static readonly int[] _dy = { -1, 0, 1, 0 };
  private static readonly int[] _dx = { 0, 1, 0, -1 };
  private static int _nextgroupId = 1;

  private Cell[,] _cells;

  private int[,] _groupId;

  private Dictionary<int, HashSet<Coord>> _groupList;

  private bool[,] _verticalDividers;

  private bool[,] _horizontalDividers;

  private bool[,] _savedVerticalDividers;

  private bool[,] _savedHorizontalDividers;

  public int Width => _groupId.GetLength(1);

  public int Height => _groupId.GetLength(0);

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

    _groupId = new int[height, width];
    _groupList = new();

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

      UpdateConnectivity(new List<Coord>());
    }
    else
    {
      UpdateConnectivity(new List<Coord>() { new Coord(0, 0) });
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
    return GetWell(_groupList[wellId].First());
  }

  [MethodImpl(256)]
  public Cell GetWell(Coord coord)
  {
    return GetWell(coord.Y, coord.X);
  }

  public Cell GetWell(int y, int x)
  {
    var cell = _cells[y, x];
    var group = _groupList[_groupId[y, x]];
    return new Cell(cell.Color, cell.Volume * group.Count, cell.Capacity * group.Count);
  }

  // 指定したセル(が属するウェル)に絵の具を追加する
  public void Add(Coord coord, CMY color)
  {
    var group = _groupList[_groupId[coord.Y, coord.X]];
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
    var group = _groupList[_groupId[coord.Y, coord.X]];

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
  public bool CanDiscardStrict(Coord coord)
  {
    return GetWell(coord).Volume >= 1 - 1e-6;
  }

  // 基準となるセルから見て縦方向なら右側、横方向なら下側の仕切りが上がっているか確認する
  [MethodImpl(256)]
  public bool IsDividerUp(Coord coord, bool isVertical)
  {
    return isVertical
      ? _verticalDividers[coord.Y, coord.X]
      : _horizontalDividers[coord.Y, coord.X];
  }

  // 基準となるセルから見て縦方向なら右側、横方向なら下側の仕切りの状態を変更する 
  public void SwitchDivider(Coord coord, bool isVertical)
  {
    if (isVertical)
    {
      _verticalDividers[coord.Y, coord.X] ^= true;
      UpdateConnectivity(new Coord[] { coord, new Coord(coord.Y, coord.X + 1) });
    }
    else
    {
      _horizontalDividers[coord.Y, coord.X] ^= true;
      UpdateConnectivity(new Coord[] { coord, new Coord(coord.Y + 1, coord.X) });
    }
  }

  // 引数で指定したリストの要素を順に取り出し、それを始点としてBFSを行い連結状態を更新していく
  private void UpdateConnectivity(IList<Coord> startPoints)
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
    HashSet<Coord> visited = new();
    HashSet<int> releasedGroupIds = new();
    foreach (Coord sp in startPoints)
    {
      // 始点が現在属しているグループのIDをメモしておく
      if (!releasedGroupIds.Contains(_groupId[sp.Y, sp.X]))
      {
        releasedGroupIds.Add(_groupId[sp.Y, sp.X]);
      }

      // 探索済みなら始点を選び直す
      if (visited.Contains(sp)) continue;
      visited.Add(sp);

      // BFSで連結なマスを探索していき1つのグループの情報を完成させる
      Queue<Coord> q = new();
      q.Enqueue(sp);
      while (q.Count >= 1)
      {
        Coord cp = q.Dequeue();
        _groupId[cp.Y, cp.X] = _nextgroupId;
        if (!_groupList.ContainsKey(_nextgroupId)) _groupList[_nextgroupId] = new();
        _groupList[_nextgroupId].Add(cp);

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
      _nextgroupId++;
    }

    // 使われなくなったグループのリストを削除する(メモリ解放)
    // foreach (int id in releasedGroupIds)
    // {
    //   _groupList.Remove(id);
    //   Error.WriteLine($"release! {id}");
    // }
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
    }
    for (int i = 0; i < horizontal.GetLength(0); i++)
    {
      for (int j = 0; j < horizontal.GetLength(1); j++)
      {
        Write($"{(horizontal[i, j] ? 1 : 0)} ");
      }
    }
  }

  // 各セルが現在どの番号のグループに属しているか一覧表示する
  public void PrintGroupStatus()
  {
    for (int i = 0; i < Height; i++)
    {
      for (int j = 0; j < Width; j++)
      {
        Error.Write($"{_groupId[i, j]} ");
      }
      Error.WriteLine();
    }
  }
}

public class Palette
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

  private Grid _grid;

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
    _grid = new Grid(Size, false);

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
        if ((verticalDividers[i, j] == 1) ^ _grid.IsDividerUp(new Coord(i, j), true))
        {
          _grid.SwitchDivider(new Coord(i, j), true);
        }
      }
    }
    for (int i = 0; i < horizontalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < horizontalDividers.GetLength(1); j++)
      {
        if ((horizontalDividers[i, j] == 1) ^ _grid.IsDividerUp(new Coord(i, j), false))
        {
          _grid.SwitchDivider(new Coord(i, j), false);
        }
      }
    }
    _grid.SaveDividers();
  }

  public Palette(bool[,] verticalDividers, bool[,] horizontalDividers) : this()
  {
    for (int i = 0; i < verticalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < verticalDividers.GetLength(1); j++)
      {
        if (verticalDividers[i, j] ^ _grid.IsDividerUp(new Coord(i, j), true))
        {
          _grid.SwitchDivider(new Coord(i, j), true);
        }
      }
    }
    for (int i = 0; i < horizontalDividers.GetLength(0); i++)
    {
      for (int j = 0; j < horizontalDividers.GetLength(1); j++)
      {
        if (horizontalDividers[i, j] ^ _grid.IsDividerUp(new Coord(i, j), false))
        {
          _grid.SwitchDivider(new Coord(i, j), false);
        }
      }
    }
    _grid.SaveDividers();
  }

  public Cell this[int y, int x]
  {
    get
    {
      return _grid[y, x];
    }
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
    _grid.Add(coord, Tubes[tubeId]);
    AddCount++;
  }

  // 操作2に対応するメソッド
  private void Give(Coord coord)
  {
    var cell = _grid.GetCell(coord);
    _grid.Discard(coord, true);
    double d = CMY.Distance(cell.Color, Targets[TargetId]).All;
    _madePaints.Add((cell.Color, d));
    Deviation = (Deviation.Definite + d, Deviation.Tentative - (CMY.MaxDistance - d));

    TargetId++;
  }

  // 操作3に対応するメソッド
  private void Discard(Coord coord, bool strict)
  {
    _grid.Discard(coord, false);
  }

  [MethodImpl(256)]
  public bool CanDiscardStrict(Coord coord) => _grid.CanDiscardStrict(coord);

  // 操作4に対応するメソッド
  private void SwitchDivider(Coord c1, Coord c2)
  {
    if (c1.Y == c2.Y && c1.X + 1 == c2.X) SwitchDivider(c1, true);
    else if (c1.Y + 1 == c2.Y && c1.X == c2.X) SwitchDivider(c1, false);
    else throw new ArgumentException($"セル{c2}はセル{c1}の右隣または下隣には存在していません");
  }

  [MethodImpl(256)]
  private void SwitchDivider(Coord coord, bool isVertical)
  {
    _grid.SwitchDivider(coord, isVertical);
  }

  [MethodImpl(256)]
  public bool IsDividerUp(Coord coord, bool isVertical)
  {
    return _grid.IsDividerUp(coord, isVertical);
  }

  [MethodImpl(256)]
  public void PrintGroupStatus()
  {
    _grid.PrintGroupStatus();
  }

  public void Print(bool verbose = false)
  {
    // 保存された仕切りの状態を出力
    _grid.PrintSavedDividers();

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
  public static readonly long Timeout = 2900;
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
    Greedy2();
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
      int dividerUpPercent = _rand.Next(30, 70);
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

      while (!plt.IsSubmittable() && TimeKeeper.ElapsedMillisec() < Timeout)
      {
        // 全てのマスを見て一番誤差の小さい色が置かれているマスを特定する
        (double Delta, Coord Coord) best = (double.MaxValue, new Coord(-1, -1));
        for (int i = 0; i < N; i++)
        {
          for (int j = 0; j < N; j++)
          {
            if (!plt.CanDiscardStrict(new Coord(i, j))) continue;
            double delta = CMY.Distance(plt[i, j].Color, Targets[plt.TargetId]).All;
            if (delta < best.Delta)
            {
              best = (delta, new Coord(i, j));
            }
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
        // else
        // {
        //   // 縦方向の仕切りがあれば下げる
        //   if (best.Coord.X < N - 1 && plt.IsDividerUp(best.Coord, true))
        //   {
        //     plt.Operate(new int[] { 4, best.Coord.Y, best.Coord.X, best.Coord.Y, best.Coord.X + 1 });
        //   }
        //   // 横方向の仕切りがあれば下げる
        //   if (best.Coord.Y < N - 1 && plt.IsDividerUp(best.Coord, false))
        //   {
        //     plt.Operate(new int[] { 4, best.Coord.Y, best.Coord.X, best.Coord.Y + 1, best.Coord.X });
        //   }
        // }
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
}
