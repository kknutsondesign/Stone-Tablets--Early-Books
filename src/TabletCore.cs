﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Soggylithe_Tablet
{
    public class ModSystemTablets: ModSystem
    {
        private ICoreServerAPI _sapi;
        private Dictionary<string, long> _scrollStart = new Dictionary<string, long>();
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("Loaded tablets.");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            _sapi = api;
            _sapi.Event.BeforeActiveSlotChanged += Scrolling;
            _sapi.Event.RegisterGameTickListener(OnTick, 50);
            _sapi.Event.PlayerJoin += OnJoin;
            //_sapi.Event.PlayerLeave += OnLeave;
        }

        private void OnTick(float ms)
        {

            for(int i = _scrollStart.Count-1; i >= 0;i--)
            {
                string key = _scrollStart.ElementAt(i).Key;
                long time = _scrollStart.ElementAt(i).Value;

                long current = _sapi.World.ElapsedMilliseconds;
                if(current - time > 100)
                {
                    _scrollStart.Remove(key);

                    IPlayer player = _sapi.World.PlayerByUid(key);

                    CheckHotbar(player, player.InventoryManager.ActiveHotbarSlotNumber);
                }
            }
        }
        public EnumHandling Scrolling(IPlayer player, ActiveSlotChangeEventArgs args)
        {
            if (!_scrollStart.ContainsKey(player.PlayerUID))
            {
                _scrollStart.Add(player.PlayerUID, _sapi.World.ElapsedMilliseconds);
                return EnumHandling.Handled;
            }
            _scrollStart[player.PlayerUID] = _sapi.World.ElapsedMilliseconds;

            return EnumHandling.Handled;
        }

        public void OnJoin(IPlayer player)
        {
            player.InventoryManager.GetHotbarInventory().SlotModified += delegate (int slot)
            {
                CheckHotbar(player, slot);
            };
        }

        //private Dictionary<string, Action<int>> _delegates = new Dictionary<string, Action<int>>();

        //public void OnJoin(IServerPlayer player)
        //{
        //    _delegates.Add(player.PlayerUID, delegate (int slot)
        //    {
        //        CheckHotbar(player, slot);
        //    });

        //    player.InventoryManager.GetHotbarInventory().SlotModified += _delegates[player.PlayerUID];
        //    };
        //}

        //public void OnLeave(IServerPlayer player)
        //{
        //    if (_delegates.ContainsKey(player.PlayerUID))
        //    {
        //        player.InventoryManager.GetHotbarInventory().SlotModified -= _delegates[player.PlayerUID];
        //        _delegates.Remove(player.PlayerUID);
        //    }
        //    else
        //    {
        //        _sapi.Logger.Error("Stone Tablets: Player leaving server had no UID registered in callback dictionary. Contact @soggylithe");
        //    }
        //}




        public void CheckHotbar(IPlayer player, int slot)
        {
            //Active slot was modified
            if (slot == player.InventoryManager.ActiveHotbarSlotNumber)
            {
                ItemSlot active = player.InventoryManager.ActiveHotbarSlot;
                ItemSlot offhand = player.Entity.LeftHandItemSlot;

                bool activeBook = !active.Empty && active.Itemstack.Collectible.Attributes != null ? active.Itemstack.Collectible.Attributes.IsTrue("editable") : false;
                bool offhandTool = !offhand.Empty && offhand.Itemstack.Collectible.Attributes != null ? offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool") : false;

                if (active.Empty && offhand.Empty) { return; }

                if (active.Empty && !offhand.Empty)
                {
                    if (offhandTool)
                        PutAwayWritingTool(player);
                    return;
                }

                if (!active.Empty && offhand.Empty)
                {
                    if (activeBook)
                        TakeOutWritingTool(player);
                    return;
                }

                if(!active.Empty && !offhand.Empty)
                {
                    if(!activeBook && offhandTool)
                        PutAwayWritingTool(player);
                }
            }
        }

        //public void ServerCheckForWritable(IPlayer player, ActiveSlotChangeEventArgs args)
        //{
        //    ItemStack fromItem = player.InventoryManager.GetHotbarItemstack(args.FromSlot);
        //    ItemStack toItem = player.InventoryManager.GetHotbarItemstack(args.ToSlot);

        //    bool wasEditing = false;
        //    bool willBeEditing = false;

        //    if (fromItem != null && fromItem.Collectible.Attributes != null)
        //        wasEditing = fromItem.Collectible.Attributes.IsTrue("editable");

        //    if (toItem != null && toItem.Collectible.Attributes != null)
        //        willBeEditing = toItem.Collectible.Attributes.IsTrue("editable");

        //    if (wasEditing && willBeEditing)
        //        return;

        //    if (willBeEditing)
        //    {
        //        TakeOutWritingTool(player);
        //    }

        //    if (wasEditing)
        //    {
        //        PutAwayWritingTool(player);
        //    }

        //    return;
        //}

        private void TakeOutWritingTool(IPlayer player)
        {
            ItemSlot hat;
            ItemSlot offhand;
            offhand = player.Entity.LeftHandItemSlot;
            hat = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName)[(int)EnumCharacterDressType.Neck];


            if (!offhand.Empty && 
                 offhand.Itemstack.Collectible.Attributes != null && 
                 offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                return;

            if (!hat.Empty &&
                hat.Itemstack.Collectible.Attributes != null &&
                hat.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                if (offhand.TryFlipWith(hat)) { offhand.MarkDirty(); hat.MarkDirty(); }
        }

        private void PutAwayWritingTool(IPlayer player)
        {
            ItemSlot hat;
            ItemSlot offhand;
            offhand = player.Entity.LeftHandItemSlot;
            hat = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName)[(int)EnumCharacterDressType.Neck];
            
            if (!offhand.Empty &&
                offhand.Itemstack.Collectible.Attributes != null &&
                offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                if (offhand.TryFlipWith(hat)) { offhand.MarkDirty(); hat.MarkDirty(); }
        }
    }
}
