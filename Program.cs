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

  public override bool Equals(object? obj) => obj is CMY cmy && C == cmy.C && M == cmy.M && Y == cmy.Y;

  public override int GetHashCode() => HashCode.Combine(C, M, Y);

  public static bool operator ==(CMY cmy1, CMY cmy2) => cmy1.Equals(cmy2);

  public static bool operator !=(CMY cmy1, CMY cmy2) => !(cmy1 == cmy2);

  public override string ToString() => $"({C},{M},{Y})";

  public void Deconstruct(out double c, out double m, out double y)
  {
    c = C;
    m = M;
    y = Y;
  }
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
    for (int i = 0; i < _n; i++)
    {
      for (int j = 0; j < _n - 1; j++)
      {
        Write("0 ");
      }
      WriteLine();
    }
    for (int i = 0; i < _n - 1; i++)
    {
      for (int j = 0; j < _n; j++)
      {
        Write("0 ");
      }
      WriteLine();
    }

    for (int i = 0; i < _h; i++)
    {
      (double Diff, int Idx) best = (double.MaxValue, -1);
      for (int j = 0; j < _k; j++)
      {
        double diff = Math.Pow(_tubes[j].C - _targets[i].C, 2)
          + Math.Pow(_tubes[j].M - _targets[i].M, 2)
          + Math.Pow(_tubes[j].Y - _targets[i].Y, 2);
        if (diff < best.Diff)
        {
          // Error.WriteLine($"update: {best.Diff}[{best.Idx}] -> {diff}[{j}]");
          best = (diff, j);
        }
      }

      // Error.WriteLine($"best: {best.Diff}[{best.Idx}]");
      WriteLine($"1 0 0 {best.Idx}");
      WriteLine("2 0 0");
    }
  }
}