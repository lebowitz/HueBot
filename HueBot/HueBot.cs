using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reactive;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;

namespace HueBot
{
  internal class HueBot
  {
    private static XmppClientConnection _client;
    private static Dictionary<string, string> _roster = new Dictionary<string, string>(20);

    public void Stop()
    {
    }

    public void Start()
    {
      _client = new XmppClientConnection(ConfigurationManager.AppSettings["Server"]) {AutoResolveConnectServer = false};

      //_client.ConnectServer = "talk.google.com"; //necessary if connecting to Google Talk

      _client.OnLogin += xmpp_OnLogin;
      _client.OnMessage += xmpp_OnMessage;
      _client.OnError += _client_OnError;

      _client.KeepAlive = true;
      _client.KeepAliveInterval = 60;

      Console.WriteLine("Connecting...");
      _client.Resource = ConfigurationManager.AppSettings["Resource"];
      _client.Open(ConfigurationManager.AppSettings["User"], ConfigurationManager.AppSettings["Password"]);
      Console.WriteLine("Connected.");

      _client.OnRosterStart += _client_OnRosterStart;
      _client.OnRosterItem += _client_OnRosterItem;
    }

    private static void _client_OnError(object sender, Exception ex)
    {
      Console.WriteLine("Exception: " + ex);
    }

    private static void _client_OnRosterStart(object sender)
    {
      _roster = new Dictionary<string, string>(20);
    }

    private static void _client_OnRosterItem(object sender, RosterItem item)
    {
      if (!_roster.ContainsKey(item.Jid.User))
        _roster.Add(item.Jid.User, item.Name);
    }

    private static void xmpp_OnLogin(object sender)
    {
      var mucManager = new MucManager(_client);

      string[] rooms = ConfigurationManager.AppSettings["Rooms"].Split(',');

      foreach (string room in rooms)
      {
        var jid = new Jid(room + "@" + ConfigurationManager.AppSettings["ConferenceServer"]);
        mucManager.JoinRoom(jid, ConfigurationManager.AppSettings["RoomNick"]);
      }
    }

    private static void xmpp_OnMessage(object sender, Message msg)
    {
      if (!String.IsNullOrEmpty(msg.Body))
      {
        Console.WriteLine("Message : {0} - from {1}", msg.Body, msg.From);

        string user;

        if (msg.Type != MessageType.groupchat)
        {
          if (!_roster.TryGetValue(msg.From.User, out user))
          {
            user = "Unknown User";
          }
        }
        else
        {
          user = msg.From.Resource;
        }

        if (user == ConfigurationManager.AppSettings["RoomNick"])
          return;

        var line = new ParsedLine(msg.Body.Trim(), user);

        switch (line.Command)
        {
          case "close":
            SendMessage(msg.From, "I'm a quitter...", msg.Type);
            Environment.Exit(1);
            return;
          default:
            var hue = new Hue();
            string output = hue.Evaluate(line);
            if (!string.IsNullOrEmpty(output))
            {
              SendMessage(msg.From, output, msg.Type);
            }
            break;
        }
      }
    }

    public static void SendMessage(Jid to, string text, MessageType type)
    {
      if (text == null) return;

      _client.Send(new Message(to, type, text));
    }

    public static void SendSequence(Jid to, IObservable<string> messages, MessageType type)
    {
      if (messages == null)
      {
        return;
      }

      var observer = Observer.Create<string>(
        msg => SendMessage(to, msg, type),
        exception => Trace.TraceError(exception.ToString()));

      messages.Subscribe(observer);
    }
  }
}