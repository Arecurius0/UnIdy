﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using UnIdy.Utils;

namespace UnIdy
{
    public class UnIdy : BaseSettingsPlugin<Settings>
    {
        private IngameState _ingameState;
        private Vector2 _windowOffset;

        public UnIdy()
        {
        }

        public override bool Initialise()
        {
            base.Initialise();
            Name = "UnIdy";

            _ingameState = GameController.Game.IngameState;
            _windowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            return true;
        }

        public override void Render()
        {
            base.Render();

            var inventoryPanel = _ingameState.IngameUi.InventoryPanel;
            if (!inventoryPanel.IsVisible && Keyboard.IsKeyToggled(Settings.HotKey.Value))
            {
                Keyboard.KeyPress(Settings.HotKey.Value);
                return;
            }

            if (!Keyboard.IsKeyToggled(Settings.HotKey.Value))
            {
                return;
            }

            //DrawPluginImageAndText();      

            Identify();
        }

        /*
        private void DrawPluginImageAndText()
        {
            var inventoryPanel = _ingameState.IngameUi.InventoryPanel;
            var playerInventory = inventoryPanel[InventoryIndex.PlayerInventory];
            var pos = playerInventory.InventoryUiElement.GetClientRect().TopLeft;
            pos.Y -= 100;
            const int height = 35;
            const int width = 169;
            var rec = new RectangleF(pos.X, pos.Y, width, height);
            pos.Y += height;
            Graphics.DrawPluginImage($"{PluginDirectory}//img//logo.png", rec);
            Graphics.DrawText($"Is running\nPress {Settings.HotKey.Value} to stop.", 20, pos);
        }
        */

        private void Identify()
        {
            var inventoryPanel = _ingameState.IngameUi.InventoryPanel;
            var playerInventory = inventoryPanel[InventoryIndex.PlayerInventory];

            var scrollOfWisdom = GetItemWithBaseName("Scroll of Wisdom", playerInventory.VisibleInventoryItems);
            LogMessage(scrollOfWisdom.Text, 1);

            if (scrollOfWisdom == null)
            {
                Keyboard.KeyPress(Settings.HotKey.Value);
                return;
            }


            var normalInventoryItems = playerInventory.VisibleInventoryItems;


            if (Settings.IdentifyVisibleTabItems.Value && _ingameState.IngameUi.StashElement.IsVisible)
            {
                foreach (var normalStashItem in _ingameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems)
                {
                    normalInventoryItems.Insert(normalInventoryItems.Count,normalStashItem);
                }
            }

            var latency = (int)_ingameState.CurLatency;
            var listOfNormalInventoryItemsToIdentify = new List<NormalInventoryItem>();

            foreach (var normalInventoryItem in normalInventoryItems)
            {
                if (normalInventoryItem.Item.HasComponent<Mods>())
                {
                    var mods = normalInventoryItem.Item.GetComponent<Mods>();

                    if (mods.Identified)
                    {
                        continue;
                    }

                    switch (mods.ItemRarity)
                    {
                        case ItemRarity.Unique when !Settings.IdentifyUniques.Value:
                            continue;
                        case ItemRarity.Rare when !Settings.IdentifyRares.Value:
                            continue;
                        case ItemRarity.Magic when !Settings.IdentifyMagicItems.Value:
                            continue;
                        case ItemRarity.Normal:
                            continue;
                        default:
                            break;
                    }

                    var sockets = normalInventoryItem.Item.GetComponent<Sockets>();
                    if (!Settings.IdentifySixSockets.Value && sockets.NumberOfSockets == 6)
                    {
                        continue;
                    }

                    if (!Settings.IdentifyItemsWithRedGreenBlueLinks.Value && sockets.IsRGB)
                    {
                        continue;
                    }

                    var itemIsMap = normalInventoryItem.Item.HasComponent<Map>();
                    if (!Settings.IdentifyMaps.Value && itemIsMap)
                    {
                        continue;
                    }

                    listOfNormalInventoryItemsToIdentify.Add(normalInventoryItem);

                }
            }

            if (listOfNormalInventoryItemsToIdentify.Count == 0)
            {
                Keyboard.KeyPress(Settings.HotKey.Value);
                return;
            }

            Mouse.SetCursorPosAndRightClick(scrollOfWisdom.GetClientRect().Center, Settings.ExtraDelay, _windowOffset);
            Thread.Sleep(latency);
            Keyboard.KeyDown(Keys.LShiftKey);
            foreach (var normalInventoryItem in listOfNormalInventoryItemsToIdentify)
            {
                if (Settings.Debug.Value)
                {
                    //Graphics.DrawFrame(normalInventoryItem.GetClientRect(), 2, Color.AliceBlue);
                }

                Mouse.SetCursorPosAndLeftClick(normalInventoryItem.GetClientRect().Center, Settings.ExtraDelay.Value, _windowOffset);
                Thread.Sleep(Constants.WHILE_DELAY + Settings.ExtraDelay.Value);
            }
            Keyboard.KeyUp(Keys.LShiftKey);
            
            Keyboard.KeyPress(Settings.HotKey.Value);
            return;

        }

        private NormalInventoryItem GetItemWithBaseName(string baseName,
            IEnumerable<NormalInventoryItem> normalInventoryItems)
        {
            try
            {

                return normalInventoryItems.First(normalInventoryItem =>
                    GameController.Files.BaseItemTypes.Translate(normalInventoryItem.Item.Path).BaseName
                        .Equals(baseName));


                /*OLD Temporary fix, might need this sometime again
                return normalInventoryItems.First(normalInventoryItem => normalInventoryItem.Item.Path.Contains("Identification"));
                */
            }
            catch
            {
                return null;
            }
        }
    }
}
