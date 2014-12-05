using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<string, int> ChangedPerUserPerHour = new Dictionary<string, int>();
        private static System.Timers.Timer _clear = new System.Timers.Timer();

        static Hue()
        {
            _clear.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            _clear.Elapsed += OnClear;
            _clear.Start();
        }

        static void OnClear(object sender, System.Timers.ElapsedEventArgs e)
        {
            ChangedPerUserPerHour.Clear();
        }
        
        private readonly HueClient _hue = new HueClient(ConfigurationManager.AppSettings["HueUrl"],
            ConfigurationManager.AppSettings["HueToken"]);

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
                if (!ChangedPerUserPerHour.ContainsKey(line.User))
                {
                    ChangedPerUserPerHour.Add(line.User, 1);
                }
                else
                {
                    ChangedPerUserPerHour[line.User] += 1;
                }
                if (ChangedPerUserPerHour[line.User] == 3)
                {
                    output.AppendFormat("Sorry, {0} keeps changing the lights...\n", line.User);
                }

                Thread.Sleep(150);

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