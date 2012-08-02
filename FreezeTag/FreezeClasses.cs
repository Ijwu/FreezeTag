using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using TShockAPI;
using Terraria;

namespace FreezeTag
{
    public class FTPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool Tagged = false;
        public bool IsIt = false;
        public bool Ready = false;
        public Rectangle TagBox { get { return new Rectangle(TSPlayer.TileX - 2, TSPlayer.TileY - 1, 4, 4); } }
        public FTGame CurrentGame { get; set; }
        public List<Point> FreezeBox = new List<Point>();

        public FTPlayer(int index)
        {
            Index = index;
        }
    }

    public class FTGame
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public bool Running = false;
        public List<FTPlayer> Players = new List<FTPlayer>();
        public int Amount = 0;
        public int Ready { get; set; }

        public FTGame(string name, string password = "")
        {
            Name = name;
            if (password != "")
                Password = password;
        }

        public void Freeze(int index)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(index);
            List<Point> val = FreezeTools.GetRectOutline(ply.TagBox);
            ply.FreezeBox = new List<Point>();
            foreach (Point pt in val)
            {
                if (!Main.tile[pt.X, pt.Y].active && Main.tile[pt.X, pt.Y].type == 0)
                {
                    Main.tile[pt.X, pt.Y].type = 54;
                    Main.tile[pt.X, pt.Y].active = true;
                    Main.tile[pt.X, pt.Y].liquid = 0;
                    Main.tile[pt.X, pt.Y].skipLiquid = true;
                    Main.tile[pt.X, pt.Y].frameNumber = 0;
                    Main.tile[pt.X, pt.Y].frameX = -1;
                    Main.tile[pt.X, pt.Y].frameY = -1;

                    TShockAPI.TSPlayer.All.SendTileSquare(pt.X, pt.Y);
                    ply.FreezeBox.Add(pt);
                }
            }
            lock (Players)
            {
                foreach (FTPlayer plyr in Players)
                {
                    if (plyr.IsIt && ply.TagBox.Contains(new Point(plyr.TSPlayer.TileX, plyr.TSPlayer.TileY)))
                    {
                        plyr.TSPlayer.Teleport(ply.TagBox.X, ply.TagBox.Y - 3);
                    }
                }
            }
            ply.Tagged = true;
            ply.TSPlayer.Teleport(ply.TagBox.X + ply.TagBox.Width / 2, ply.TagBox.Y + ply.TagBox.Height / 2);
            if (CheckIfAllTagged())
            {
                EndGame();
            }
        }
        public void Unfreeze(int index)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(index);
            foreach (Point pt in ply.FreezeBox)
            {               
                Main.tile[pt.X, pt.Y].type = 0;
                Main.tile[pt.X, pt.Y].active = false;
                Main.tile[pt.X, pt.Y].liquid = 0;
                Main.tile[pt.X, pt.Y].skipLiquid = true;
                Main.tile[pt.X, pt.Y].frameNumber = 0;
                Main.tile[pt.X, pt.Y].frameX = -1;
                Main.tile[pt.X, pt.Y].frameY = -1;
                TShockAPI.TSPlayer.All.SendTileSquare(pt.X, pt.Y);
            }
            ply.Tagged = false;
            
        }
        public bool CheckIfAllTagged()
        {
            int amt = 0;
            lock (Players)
            {
                foreach (FTPlayer ply in Players)
                {
                    if (ply.Tagged)
                        amt++;
                }
            }
            if (amt == Amount)
                return true;
            return false;
        }
        public void AddMember(int index)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(index);
            lock (Players)
                Players.Add(ply);
            ply.CurrentGame = this;
            Amount++;
            ply.TSPlayer.SendMessage(String.Format("You were added to the FreezeTag game: \"{0}\"", this.Name), Color.Aqua);
            string inply = "";
            lock (Players)
            {
                foreach (FTPlayer gm in Players)
                {
                    if (gm != ply)
                    {
                        inply += String.Format(" {0}", gm.TSPlayer.Name);
                        gm.TSPlayer.SendMessage(ply.TSPlayer.Name + " has joined the FTgame.", Color.Aqua);
                    }
                }
            }
            if (Amount < 3)
            {
                ply.TSPlayer.SendMessage("Players in FTgame: " + inply, Color.Aqua);
                ply.TSPlayer.SendMessage("Cannot start the FTgame until at least 3 people are in the game.", Color.Aqua);
            }
            else
                StartGame();
        }
        public void RemoveMember(int index)
        {
            FTPlayer ply = FreezeTools.GetFTPlayerByID(index);
            lock (Players)
            {
                Players.Remove(ply);
                foreach (FTPlayer gm in Players)
                {
                    gm.TSPlayer.SendMessage(ply.TSPlayer.Name + " has left the FTgame.", Color.Aqua);
                }
            }
            ply.CurrentGame = null;
            Amount--;
            if (Amount == 0)
                FreezeTools.RemoveGame(this);
        }
        public void StartGame()
        {
            lock (Players)
            {
                if (CheckReady() && Amount > 2)
                {
                    foreach (FTPlayer ply in Players)
                    {
                        ply.Tagged = false;
                    }
                    FTPlayer It = FreezeTools.GetRandomPlayer(Players);
                    lock (Players)
                        It.IsIt = true;
                    It.TSPlayer.SendMessage("You are it! Tag everyone else to freeze them!", Color.Aqua);
                    foreach (FTPlayer ply in Players)
                    {
                        if (!ply.IsIt)
                            ply.TSPlayer.SendMessage(String.Format("{0} is it! Run away from them. Tag frozen players to unfreeze them!", It.TSPlayer.Name), Color.Aqua);
                    }
                    Running = true;
                }
                else
                {
                    foreach (FTPlayer ply in Players)
                    {
                        ply.TSPlayer.SendMessage(String.Format("Need at least {0} more player(s) ready.", (Math.Ceiling((Amount * .7) - Ready)).ToString()), Color.Aqua);
                    }
                }
            }
        }
        public void EndGame()
        {
            Running = false;
            lock (Players)
            {
                foreach (FTPlayer ply in Players)
                {
                    ply.TSPlayer.SendMessage("The game has ended. A new game will begin as soon as all players are ready.", Color.Aqua);
                    ply.Tagged = false;
                    ply.IsIt = false;
                    ply.Ready = false;
                }
            }
            StartGame();
        }
        public bool CheckReady()
        {
            Ready = 0;
            lock (Players)
            {
                foreach (FTPlayer ply in Players)
                {
                    if (ply.Ready)
                        Ready++;
                }
            }
            double pct = Ready / Amount;
            if (pct >= .7)
                return true;
            return false;
        }
    }
}
