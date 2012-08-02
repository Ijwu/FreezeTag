using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using Hooks;
using System.ComponentModel;

namespace FreezeTag
{
    /*To-Do:
     * Game starting and ending - Done
     * Game rules implementation - Done?
     * Player freezing and tagging - Done
     * Commands - Done
     * Player and Game tracking - Done
     * Removal on player leave - Done
     * disposal of hooks - Done
     */
    [APIVersion(1, 12)]
    public class FreezeMain : TerrariaPlugin
    {
        public override string Author
        {
            get { return "Ijwu"; }
        }

        public override string Description
        {
            get { return "Freeze Tag"; }
        }

        public override string Name
        {
            get { return "Freeze Tag"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0, 0); }
        }

        public static List<FTPlayer> Players = new List<FTPlayer>();
        public static List<FTGame> Games = new List<FTGame>();

        public FreezeMain(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            ServerHooks.Join += OnJoin;
            GameHooks.Update += OnUpdate;
            ServerHooks.Leave += OnLeave;
            GetDataHandlers.TileEdit += OnTileEdit;

            Commands.ChatCommands.Add(new Command("joinft", StartGame, "joinft"));
            Commands.ChatCommands.Add(new Command("joinft", QuitGame, "quitft"));
            Commands.ChatCommands.Add(new Command("joinft", ReadyUp, "readyft"));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerHooks.Join -= OnJoin;
                ServerHooks.Leave -= OnLeave;
                GameHooks.Update -= OnUpdate;
            }
            base.Dispose(disposing);
        }

        public void OnJoin(int who, HandledEventArgs e)
        {
            FTPlayer player = new FTPlayer(who);

            lock (Players)
                Players.Add(player);
        }

        public void OnLeave(int who)
        {
            lock (Players)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == who)
                    {
                        if (Players[i].CurrentGame != null)
                            Players[i].CurrentGame.RemoveMember(i);
                        Players.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        public void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(args.Player.Index);
            if (args.Handled)
            {
                return;
            }
            if (ply.Tagged)
            {
                args.Player.SendTileSquare(args.X, args.Y);
                args.Handled = true;
            }
            if (ply.IsIt)
            {
                if (ply.CurrentGame != null)
                {
                    lock (ply.CurrentGame.Players)
                    {
                        foreach (FTPlayer plyr in ply.CurrentGame.Players)
                        {
                            if (plyr.Tagged && FreezeTools.GetRectOutline(plyr.TagBox).Contains(new Point((int)args.X, (int)args.Y)))
                            {
                                args.Handled = true;
                            }
                        }
                    }
                }
            }
        }
        public void OnUpdate()
        {
            lock (Players)
            {
                foreach (FTPlayer ply in Players)
                {
                    if (!ply.Tagged)
                    {
                        if (ply.CurrentGame != null && ply.CurrentGame.Running)
                        {
                            foreach (FTPlayer gamer in ply.CurrentGame.Players)
                            {
                                if (ply.TagBox.Intersects(gamer.TagBox) && gamer != ply)
                                {
                                    if (ply.IsIt && !gamer.Tagged)
                                    {
                                        TShock.Utils.Broadcast(String.Format("{0} has tagged {1}.", ply.TSPlayer.Name, gamer.TSPlayer.Name), Color.Aqua);
                                        ply.CurrentGame.Freeze(gamer.Index);
                                        gamer.TSPlayer.SendMessage(String.Format("{0} has frozen you.", ply.TSPlayer.Name), Color.Aqua);
                                    }
                                    else
                                    {
                                        if (!ply.IsIt && gamer.Tagged)
                                        {
                                            TShock.Utils.Broadcast(String.Format("{0} has un - tagged {1}.", ply.TSPlayer.Name, gamer.TSPlayer.Name), Color.Aqua);
                                            ply.CurrentGame.Unfreeze(gamer.Index);
                                            gamer.TSPlayer.SendMessage(String.Format("{0} has unfrozen you.", ply.TSPlayer.Name), Color.Aqua);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Point current = new Point(ply.TSPlayer.TileX, ply.TSPlayer.TileY+1);
                        if (!ply.TagBox.Contains(current))
                            ply.TSPlayer.Teleport(ply.TagBox.X + ply.TagBox.Width / 2, ply.TagBox.Y + ply.TagBox.Height / 2);
                    }
                }
            }
        }
        public static void StartGame(CommandArgs args)
        {
            switch (args.Parameters.Count)
            {
                default:
                    {
                        args.Player.SendMessage("Invalid syntax. Proper usage: /joinft <game name> [password]", Color.Red);
                        break;
                    }
                case 1:
                    {
                        foreach (FTGame gms in Games)
                        {
                            if (gms.Name == args.Parameters[0])
                            {
                                gms.AddMember(args.Player.Index);
                                return;
                            }
                        }
                        args.Player.SendMessage(String.Format("Game not found. Creating a new one called \"{0}\".", args.Parameters[0]), Color.Aqua);
                        FTGame newgame = new FTGame(args.Parameters[0]);
                        newgame.AddMember(args.Player.Index);
                        Games.Add(newgame);
                        break;
                    }
                case 2:
                    {
                        foreach (FTGame gms in Games)
                        {
                            if (gms.Name == args.Parameters[0])
                            {
                                if (gms.Password == args.Parameters[1])
                                {
                                    gms.AddMember(args.Player.Index);
                                    return;
                                }
                                else
                                {
                                    args.Player.SendMessage(String.Format("Wrong password for game: \"{0}\"", gms.Name), Color.Red);
                                    return;
                                }
                            }
                        }
                        args.Player.SendMessage(String.Format("Game not found. Creating a new one called \"{0}\" with the password \"{1}\".", args.Parameters[0], args.Parameters[1]), Color.Aqua);
                        FTGame newgame = new FTGame(args.Parameters[0], args.Parameters[1]);
                        newgame.AddMember(args.Player.Index);
                        Games.Add(newgame);
                        break;
                    }
            }
        }
        public static void ReadyUp(CommandArgs args)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(args.Player.Index);
            if (ply.CurrentGame != null && !ply.CurrentGame.Running)
            {
                ply.Ready = (!ply.Ready);
                args.Player.SendMessage(String.Format("You are{0}ready.", (ply.Ready ? " " : " not ")), Color.Aqua);
                lock (ply.CurrentGame.Players)
                {
                    foreach (FTPlayer gm in ply.CurrentGame.Players)
                    {
                        if (gm != ply)
                        {
                            gm.TSPlayer.SendMessage(String.Format("{0} is {1} ready.", ply.TSPlayer.Name, (ply.Ready ? "now" : "NOT")), Color.Aqua);
                        }
                    }
                }
                ply.CurrentGame.StartGame();
            }
            else
                args.Player.SendMessage("You must join a FreezeTag game before readying/unreadying.", Color.Red);
        }
        public static void QuitGame(CommandArgs args)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(args.Player.Index);
            if (ply.CurrentGame != null)
            {
                lock (ply)
                {
                    ply.CurrentGame.RemoveMember(ply.Index);
                    ply.Ready = false;
                    ply.IsIt = false;
                    ply.Tagged = false;
                    ply.TSPlayer.SendMessage("You have quit your current FreezeTag game.", Color.Aqua);
                }
            }
            else
                args.Player.SendMessage("You must be in a FreezeTag game before being able to quit one.", Color.Red);
        }
    }
}
