using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;
using agsXMPP.Xml.Dom;
using HipChat.Net;
using HipChat.Net.Http;

namespace HueBot
{
  internal class HueBot
  {
    private static XmppClientConnection _client;
    private static Dictionary<string, string> _roster = new Dictionary<string, string>(20);
    private static readonly string HipchatGroupId = ConfigurationManager.AppSettings["HipchatGroupId"];
    private static readonly Regex RoomRegex = new Regex(ConfigurationManager.AppSettings["RoomRegex"]);
    private static readonly Regex IgnoreUserRegex = new Regex(ConfigurationManager.AppSettings["IgnoreUserRegex"]);
    private static readonly string ConferenceServer = ConfigurationManager.AppSettings["ConferenceServer"];
    private static readonly Hue Hue = new Hue();
    private static readonly HipChatClient HipChatApiClient = new HipChatClient(new ApiConnection(new Credentials("ltxVo6YZTSYINPdnU3V8T0olBZUI1VOYZFjUjp3N")));
    private static readonly string Resource = ConfigurationManager.AppSettings["Resource"];
    private static readonly string Server = ConfigurationManager.AppSettings["Server"];
    private static readonly string User = ConfigurationManager.AppSettings["User"];
    private static readonly string Password = ConfigurationManager.AppSettings["Password"];
    private static readonly string RoomNick = ConfigurationManager.AppSettings["RoomNick"];
    private static List<string> JoinedRooms = new List<string>();

    public void Stop()
    {
    }

    public void Start()
    {
      _client = new XmppClientConnection(Server) {AutoResolveConnectServer = false};
      _client.OnLogin += OnLogin;
      _client.OnMessage += OnMessage;
      _client.OnError += OnError;

      _client.KeepAlive = true;
      _client.KeepAliveInterval = 60;

      Console.WriteLine("Connecting...");
      _client.Resource = Resource;
      _client.Open(User, Password);
      _client.OnAuthError += OnAuthError;
      _client.OnXmppConnectionStateChanged += OnXmppConnectionStateChanged;
      Console.WriteLine("Connected.");

      _client.OnRosterStart += OnRosterStart;
      _client.OnRosterItem += OnRosterItem;
    }

    public void OnXmppConnectionStateChanged(object sender, XmppConnectionState state)
    {
      Console.WriteLine("Connection state change: " + state);
    }

    private void OnAuthError(object sender, Element element)
    {
      Console.WriteLine("Auth error: " + element);
    }

    private static void OnError(object sender, Exception ex)
    {
      Console.WriteLine("Exception: " + ex);
    }

    private static void OnRosterStart(object sender)
    {
      _roster = new Dictionary<string, string>();
    }

    private static void OnRosterItem(object sender, RosterItem item)
    {
      if (!_roster.ContainsKey(item.Jid.User))
        _roster.Add(item.Jid.User, item.Name);
    }

    private static void OnLogin(object sender)
    {
      var mucManager = new MucManager(_client);
      JoinRooms(mucManager);
    }

    private static void JoinRooms(MucManager mucManager)
    {
      var rooms = HipChatApiClient.Rooms.GetAllAsync();

      rooms.Wait();

      foreach (var room in rooms.Result.Model.Items)
      {
        string jabberRoomId = HipchatGroupId + "_" + room.Name.ToLower().Replace("'", "").Replace(" ", "_") + "@" +
                              ConferenceServer;
        if (RoomRegex.IsMatch(jabberRoomId))
        {
          JoinedRooms.Add(jabberRoomId);
          mucManager.JoinRoom(new Jid(jabberRoomId), RoomNick);
          Console.WriteLine("Joined " + jabberRoomId);
        }
      }
    }

    private static void OnMessage(object sender, Message msg)
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

        if (IgnoreUserRegex.IsMatch(user)
            || user == RoomNick)
        {
          return;
        }

        var line = new ParsedLine(msg.Body.Trim(), user);

        string output = Hue.Evaluate(line);
        if (!string.IsNullOrEmpty(output))
        {
          SendMessage(msg.From, output, msg.Type);
        }
      }
    }

    public static void SendMessage(Jid to, string text, MessageType type)
    {
      if (text == null) return;

      /*foreach (var r in JoinedRooms)
      {
        _client.Send(new Message(r, MessageType.groupchat, text));
      }*/
    }
  }
}