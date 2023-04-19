using System;
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
        private bool _registered;
        private ICoreClientAPI _capi;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("Loaded tablets.");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            _capi = api; 
            _capi.Event.AfterActiveSlotChanged += CheckForWritable;
            //TODO check for interacting with bookshelf - TryTake hook, inventory hook, event hook?
        }

        public void CheckHotbar(int slot)
        {
            //Active slot was modified
            if (slot == _capi.World.Player.InventoryManager.ActiveHotbarSlotNumber)
            {
                ItemSlot active = _capi.World.Player.InventoryManager.ActiveHotbarSlot;
                ItemSlot offhand = _capi.World.Player.Entity.LeftHandItemSlot;

                if(active.Empty && offhand.Empty) { return; }

                if(active.Empty && !offhand.Empty)
                {
                    if (offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                        PutAwayWritingTool();
                    return;
                }

                if(!active.Empty && offhand.Empty)
                {
                    if(active.Itemstack.Collectible.Attributes.IsTrue("editable"))
                        TakeOutWritingTool();
                    return;
                }

            }
        }

        public void CheckForWritable(ActiveSlotChangeEventArgs args)
        {
            if (!_registered) { _capi.World.Player.InventoryManager.GetHotbarInventory().SlotModified += CheckHotbar; _registered = true; }

            ItemStack fromItem = _capi.World.Player.InventoryManager.GetHotbarItemstack(args.FromSlot);
            ItemStack toItem = _capi.World.Player.InventoryManager.GetHotbarItemstack(args.ToSlot);

            bool wasEditing = fromItem != null ? fromItem.Collectible.Attributes.IsTrue("editable") : false;
            bool willBeEditing = toItem != null ? toItem.Collectible.Attributes.IsTrue("editable") : false;

            if (wasEditing && willBeEditing)
                return;

            if (willBeEditing)
            {
                TakeOutWritingTool(); 
            }

            if(wasEditing)
            {
                PutAwayWritingTool();
            }
        }

        private void TakeOutWritingTool()
        {
            ItemSlot hat;
            ItemSlot offhand;
            offhand = _capi.World.Player.Entity.LeftHandItemSlot;
            hat = _capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName)[(int)EnumCharacterDressType.Neck];


            if (!offhand.Empty && offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                return;

            if (!hat.Empty && hat.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                offhand.TryFlipWith(hat);
        }

        private void PutAwayWritingTool()
        {
            ItemSlot hat;
            ItemSlot offhand;
            offhand = _capi.World.Player.Entity.LeftHandItemSlot;
            hat = _capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName)[(int)EnumCharacterDressType.Neck];

            if (!offhand.Empty && offhand.Itemstack.Collectible.Attributes.IsTrue("writingTool"))
                offhand.TryFlipWith(hat);
        }
    }
}
