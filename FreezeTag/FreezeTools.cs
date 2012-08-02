using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace FreezeTag
{
    class FreezeTools
    {
        public static FTPlayer GetFTPlayerByID(int index)
        {
            FTPlayer player = null;
            lock (FreezeMain.Players)
            {
            foreach (FTPlayer ply in FreezeMain.Players)
            {
                if (ply.Index == index)
                    return ply;
            }
            }
            return player;
        }
        public static FTPlayer GetRandomPlayer(List<FTPlayer> list)
        {
            Random rand = new Random();
            return list[rand.Next(list.Count - 1)];
        }
        public static void RemoveGame(FTGame game)
        {
            lock (FreezeMain.Games)
                FreezeMain.Games.Remove(game);
        }
        public static List<Point> GetRectOutline(Rectangle rect)
        {
            List<Point> val = new List<Point>();
            for (int i=rect.X; i <= rect.X + rect.Width; i++)
            {
                val.Add(new Point(i, rect.Y));
                val.Add(new Point(i, rect.Y+rect.Height));
            }
            for (int i = rect.Y; i <= rect.Y + rect.Height; i++)
            {
                val.Add(new Point(rect.X, i));
                val.Add(new Point(rect.X + rect.Width, i));
            }
            return val;
        }
        public static void UpdateTile(int x, int y)
        {
            x = Netplay.GetSectionX(x);
            y = Netplay.GetSectionY(y);

            foreach (TSPlayer ply in TShock.Players)
            {
                Netplay.serverSock[ply.Index].tileSection[x, y] = false;
            }
        }
    }
}
