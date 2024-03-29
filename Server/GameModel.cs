﻿using System;
using System.Collections.Generic;
using System.Timers;
using Newtonsoft.Json;
using WebSocketSample.RPC;

namespace WebSocketSample.Server
{
    public class GameModel
    {
        Dictionary<int, Player> players = new Dictionary<int, Player>();
        Dictionary<int, Item> items = new Dictionary<int, Item>();

        int uidCounter;

        public event Action<string, string> sendTo;
        public event Action<string> broadcast;

        public struct Spawn_Position
        {
            public float x;
            public float y;
            public float z;

            public Spawn_Position(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        };


        Spawn_Position[] spawn_point = new Spawn_Position[4];

        public GameModel()
        {
            spawn_point[0] = new Spawn_Position(0.0f, 13.0f, 0.0f);
            spawn_point[1] = new Spawn_Position(0.0f, 13.0f, 25.0f);
            spawn_point[2] = new Spawn_Position(-25.0f, 13.0f, 0.0f);
            spawn_point[3] = new Spawn_Position(-25.0f, 13.0f, 25.0f);

            StartSpawnTimer();
            PlayerSmallerTimer();
        }

        public void OnUpdate()
        {
            Sync();
        }

        public void OnPing(string senderId)
        {
            Console.WriteLine(">> Ping");

            var pingRpc = new Ping(new PingPayload("pong"));
            var pingJson = JsonConvert.SerializeObject(pingRpc);
            sendTo(pingJson, senderId);

            Console.WriteLine("<< Pong");
        }

        public void OnLogin(string senderId, LoginPayload loginPayload)
        {
            Console.WriteLine(">> Login");

            Random rand = new System.Random();
            int result = rand.Next() % 4;
            var pos = spawn_point[result];
            var player = new Player(uidCounter++, loginPayload.Name, new Position(pos.x, pos.y, pos.z), 0, 1.0f);
            lock (players)
            {
                players[player.Uid] = player;
            }

            var loginResponseRpc = new LoginResponse(new LoginResponsePayload(player.Uid, pos.x, pos.y, pos.z));
            var loginResponseJson = JsonConvert.SerializeObject(loginResponseRpc);
            sendTo(loginResponseJson, senderId);

            Console.WriteLine(player.ToString() + " login.");

            Environment(senderId);
        }

        public void OnLogout(string senderId, LogoutPayload logoutPayload)
        {
            Console.WriteLine(">> Logout");

            // 生成してる、削除に変更
            //var player = new Player(uidCounter++, logoutPayload.Name, new Position(0f, 0f, 0f), 0);
            //lock (players)
            //{
            //    players[player.Uid] = player;
            //}

            //var logoutResponseRpc = new LogoutResponse(new LogoutResponsePayload());
            //var logoutResponseJson = JsonConvert.SerializeObject(logoutResponseRpc);
            //sendTo(logoutResponseJson, senderId);

            //Console.WriteLine(player.ToString() + " logout.");
            Console.WriteLine(" logout.");

            //Environment(senderId);
        }

        public void OnPlayerUpdate(string senderId, PlayerUpdatePayload playerUpdatePayload)
        {
            Console.WriteLine(">> PlayerUpdate");

            Player player;
            if (players.TryGetValue(playerUpdatePayload.Id, out player))
            {
                player.SetPosition(playerUpdatePayload.Position);
            }
        }

        public void OnGetItem(string senderId, GetItemPayload getItemPayload)
        {
            Console.WriteLine(">> GetItem");

            var itemId = getItemPayload.ItemId;
            if (items.ContainsKey(itemId))
            {
                items.Remove(itemId);
                players[getItemPayload.PlayerId].Scale += 0.3f;

                // スケールの変更
                if(players[getItemPayload.PlayerId].Scale >= 3.0f)
                {
                    players[getItemPayload.PlayerId].Scale = 3.0f;
                }                

                var deleteItemRpc = new DeleteItem(new DeleteItemPayload(itemId));
                var deleteItemJson = JsonConvert.SerializeObject(deleteItemRpc);
                broadcast(deleteItemJson);
            }
            else
            {
                Console.WriteLine("Not found ItemId: "+ itemId);
            }
        }

        public void OnCollision(string senderId, CollisionPayload payload)
        {
            if (!players.ContainsKey(payload.AlphaId)) { return; }
            if (!players.ContainsKey(payload.BravoId)) { return; }

            var alphaPlayer = players[payload.AlphaId];
            var bravoPlayer = players[payload.BravoId];

            if (alphaPlayer.Score == bravoPlayer.Score) { return; }

            var loser = alphaPlayer.Score < bravoPlayer.Score ? alphaPlayer : bravoPlayer;

            lock (players)
            {
                players.Remove(loser.Uid);
            }

            var deletePlayerRpc = new DeletePlayer(new DeletePlayerPayload(loser.Uid));
            var deletePlayerJson = JsonConvert.SerializeObject(deletePlayerRpc);
            broadcast(deletePlayerJson);
        }

        void Sync()
        {
            if (players.Count == 0) return;

            var movedPlayers = new List<RPC.Player>();
            lock (players)
            {
                foreach (var player in players.Values)
                {
                    if (!player.isPositionChanged) continue;

                    var playerRpc = new RPC.Player(player.Uid, player.Position, player.Score, player.Scale);
                    movedPlayers.Add(playerRpc);
                    player.isPositionChanged = false;
                }
            }

            if (movedPlayers.Count == 0) return;

            var syncRpc = new Sync(new SyncPayload(movedPlayers));
            var syncJson = JsonConvert.SerializeObject(syncRpc);
            broadcast(syncJson);
        }

        void StartSpawnTimer()
        {
            var random = new Random();
            var timer = new Timer(3000);
            timer.Elapsed += (_, __) =>
            {
                if (players.Count == 0) return;

                var m_random = random.Next(0, 4);

                if (m_random == 0)
                {
                    var randomX = random.Next(-9, 9);
                    var randomZ = random.Next(-9, 9);
                    var position = new Position(randomX, 0.5f, randomZ);
                    var item = new Item(uidCounter++, position);
                    lock (items)
                    {
                        items.Add(item.Id, item);
                    }

                    var rpcItem = new RPC.Item(item.Id, item.Position);
                    var spawnRpc = new Spawn(new SpawnPayload(rpcItem));
                    var spawnJson = JsonConvert.SerializeObject(spawnRpc);
                    broadcast(spawnJson);

                }


                if (m_random == 1)
                {
                    var randomX = random.Next(-33, -15);
                    var randomZ = random.Next(-9, 9);
                    var position = new Position(randomX, 0.5f, randomZ);
                    var item = new Item(uidCounter++, position);
                    lock (items)
                    {
                        items.Add(item.Id, item);
                    }

                    var rpcItem = new RPC.Item(item.Id, item.Position);
                    var spawnRpc = new Spawn(new SpawnPayload(rpcItem));
                    var spawnJson = JsonConvert.SerializeObject(spawnRpc);
                    broadcast(spawnJson);

                }

                if (m_random == 2)
                {
                    var randomX = random.Next(-9, 9);
                    var randomZ = random.Next(18, 33);
                    var position = new Position(randomX, 0.5f, randomZ);
                    var item = new Item(uidCounter++, position);
                    lock (items)
                    {
                        items.Add(item.Id, item);
                    }

                    var rpcItem = new RPC.Item(item.Id, item.Position);
                    var spawnRpc = new Spawn(new SpawnPayload(rpcItem));
                    var spawnJson = JsonConvert.SerializeObject(spawnRpc);
                    broadcast(spawnJson);

                }


                if (m_random == 3)
                {
                    var randomX = random.Next(-33, -15);
                    var randomZ = random.Next(18, 33);
                    var position = new Position(randomX, 0.5f, randomZ);
                    var item = new Item(uidCounter++, position);
                    lock (items)
                    {
                        items.Add(item.Id, item);
                    }

                    var rpcItem = new RPC.Item(item.Id, item.Position);
                    var spawnRpc = new Spawn(new SpawnPayload(rpcItem));
                    var spawnJson = JsonConvert.SerializeObject(spawnRpc);
                    broadcast(spawnJson);

                }

                Console.WriteLine("<< Spawn");

            };
            timer.Start();
        }

        void PlayerSmallerTimer()
        {
            var random = new Random();
            var timer = new Timer(100);
            timer.Elapsed += (_, __) =>
            {
                if (players.Count == 0) return;

                var movedPlayers = new List<RPC.Player>();
                lock (players)
                {
                    foreach (var player in players.Values)
                    {
                        if (player.Scale <= 1) continue;
                        player.Scale -= 0.005f;
                        if (player.Scale < 1.0f) player.Scale = 1.0f;
                        
                        var playerRpc = new RPC.Player(player.Uid, player.Position, player.Score, player.Scale);
                        movedPlayers.Add(playerRpc);
                        player.isPositionChanged = false;
                    }
                }

                if (movedPlayers.Count != 0)
                {
                    Console.WriteLine("<< Smaller");
                    var syncRpc = new Sync(new SyncPayload(movedPlayers));
                    var syncJson = JsonConvert.SerializeObject(syncRpc);
                    broadcast(syncJson);
                }
            };
            timer.Start();

        }

        void Environment(string id)
        {
            var itemsRpc = new List<RPC.Item>();
            lock (items)
            {
                foreach (var item in items.Values)
                {
                    var itemRpc = new RPC.Item(item.Id, item.Position);
                    itemsRpc.Add(itemRpc);
                }
            }

            var environmentRpc = new RPC.Environment(new EnvironmentPayload(itemsRpc));
            var environmentJson = JsonConvert.SerializeObject(environmentRpc);
            sendTo(environmentJson, id);
        }
    }
}
