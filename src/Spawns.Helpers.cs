using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using System;
using System.Globalization;

using Vector = SwiftlyS2.Shared.Natives.Vector;
using QAngle = SwiftlyS2.Shared.Natives.QAngle;
using Color = SwiftlyS2.Shared.Natives.Color;

namespace Spawns;

public partial class Spawns
{
  private static bool TryParseVector(string s, out Vector v)
  {
    v = Vector.Zero;
    var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 3) return false;

    if (!TryParsePosFloat(parts[0], out var x)) return false;
    if (!TryParsePosFloat(parts[1], out var y)) return false;
    if (!TryParsePosFloat(parts[2], out var z)) return false;

    v = new Vector(x, y, z);
    return true;
  }

  private static bool TryParseQAngle(string s, out QAngle a)
  {
    a = QAngle.Zero;
    var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 3) return false;

    if (!TryParsePosFloat(parts[0], out var p)) return false;
    if (!TryParsePosFloat(parts[1], out var y)) return false;
    if (!TryParsePosFloat(parts[2], out var r)) return false;

    a = new QAngle(p, y, r);
    return true;
  }

  private static string NormalizeTeamLabel(string? team)
  {
    if (string.IsNullOrWhiteSpace(team)) return "ANY";
    var t = team.Trim().ToLowerInvariant();
    if (t == "t") return "T";
    if (t == "ct") return "CT";
    if (t == "dm") return "DM";
    return team.Trim();
  }

  private static Color GetTeamColor(string? team)
  {
    var t = team?.Trim().ToLowerInvariant();
    return t switch
    {
      "ct" => new Color(0, 128, 255, 255),
      "t" => new Color(255, 140, 0, 255),
      "dm" => new Color(180, 0, 255, 255),
      _ => new Color(255, 255, 255, 255)
    };
  }

  private static string CreateKey(string team, string pos, string angle)
  {
    _ = angle;
    return $"{team}|{pos}";
  }

  private static string CreateKey(string team, Vector pos)
  {
    return $"{team.Trim().ToLowerInvariant()}|{FormatKeyFloat(pos.X)}|{FormatKeyFloat(pos.Y)}|{FormatKeyFloat(pos.Z)}";
  }

  private static string CreatePosKey(Vector pos)
  {
    return $"{FormatKeyFloat(pos.X)}|{FormatKeyFloat(pos.Y)}|{FormatKeyFloat(pos.Z)}";
  }

  private static bool TryCreateKey(string team, string pos, out string key)
  {
    key = string.Empty;
    var normalizedTeam = team.Trim().ToLowerInvariant();
    if (normalizedTeam.Length == 0)
    {
      return false;
    }

    var parts = pos.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length != 3)
    {
      return false;
    }

    if (!TryParsePosFloat(parts[0], out var x) || !TryParsePosFloat(parts[1], out var y) || !TryParsePosFloat(parts[2], out var z))
    {
      return false;
    }

    key = $"{normalizedTeam}|{FormatKeyFloat(x)}|{FormatKeyFloat(y)}|{FormatKeyFloat(z)}";
    return true;
  }

  private static bool TryParsePosFloat(string s, out float value)
  {
    s = s.Replace(",", string.Empty);
    return float.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
  }

  private static string FormatKeyFloat(float value)
  {
    var rounded = Math.Round(value, 2);
    return rounded.ToString("F2", CultureInfo.InvariantCulture);
  }

  private static string FormatVector(Vector v)
  {
    return string.Join(
      " ",
      v.X.ToString("N2", CultureInfo.InvariantCulture),
      v.Y.ToString("N2", CultureInfo.InvariantCulture),
      v.Z.ToString("N2", CultureInfo.InvariantCulture)
    );
  }

  private static string FormatQAngle(QAngle a)
  {
    return string.Join(
      " ",
      a.Pitch.ToString("N2", CultureInfo.InvariantCulture),
      a.Yaw.ToString("N2", CultureInfo.InvariantCulture),
      a.Roll.ToString("N2", CultureInfo.InvariantCulture)
    );
  }

  private static Vector AngleToForward(QAngle ang)
  {
    var pitchRad = ang.Pitch * (MathF.PI / 180f);
    var yawRad = ang.Yaw * (MathF.PI / 180f);

    var cp = MathF.Cos(pitchRad);
    var sp = MathF.Sin(pitchRad);
    var cy = MathF.Cos(yawRad);
    var sy = MathF.Sin(yawRad);

    return new Vector(cp * cy, cp * sy, -sp);
  }
}
