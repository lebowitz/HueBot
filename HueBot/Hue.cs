using System;
using System.Configuration;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Q42.HueApi;

namespace HueBot
{
  public class Hue
  {
    private readonly Regex _colors =
      new Regex(ConfigurationManager.AppSettings["ColorRegex"],
        RegexOptions.IgnoreCase);

    private readonly HueClient _hue = new HueClient(ConfigurationManager.AppSettings["HueUrl"], ConfigurationManager.AppSettings["HueToken"]);

    public string Evaluate(ParsedLine line)
    {
      var output = new StringBuilder();

      var matches = _colors.Matches(line.Raw);
      int i = 1;

      foreach (Match m in matches)
      {
        i++;
        Console.WriteLine(m.Value);
        Color c = Color.FromName(m.Value);
        var lc = new LightCommand();
        var xy = HueColorConverter.XyFromColor(c.R, c.G, c.B);
        lc.TurnOn().SetColor(xy.x, xy.y);
        lc.Effect = Effect.None;
        if (line.Raw.Contains("!"))
        {
          lc.Alert = Alert.Once;
        }
        if (line.Raw.Contains("!!"))
        {
          lc.Alert = Alert.Multiple;
        }


        _hue.SendGroupCommandAsync(lc);
        output.AppendFormat("{0} set all lights {1}.\n", line.User, m.Value);
        Thread.Sleep(100);
        if (i > 5) break;
      }

      return output.ToString();
    }

    public string Name
    {
      get { return "Hue"; }
    }
  }
}