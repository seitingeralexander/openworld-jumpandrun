using System.Collections.Generic;
using System.Linq;

namespace JumpAndRun.Simulation
{
    /// <summary>
    /// Represents an item in the game.
    /// </summary>
    public class Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsStackable { get; set; } = true;

        public Item(string id, string name, ItemType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }

    public enum ItemType
    {
        Consumable,
        Weapon,
        Armor,
        Accessory,
        QuestItem,
        Material
    }

    /// <summary>
    /// Player inventory for storing items.
    /// </summary>
    public class Inventory
    {
        public List<Item> Items { get; private set; } = new();
        public int Capacity { get; set; } = 20;

        public bool AddItem(Item item)
        {
            if (item.IsStackable)
            {
                var existing = Items.FirstOrDefault(i => i.Id == item.Id);
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                    return true;
                }
            }

            if (Items.Count >= Capacity) return false;
            
            Items.Add(item);
            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                Items.Remove(item);
            }
            return true;
        }

        public Item GetItem(string itemId)
        {
            return Items.FirstOrDefault(i => i.Id == itemId);
        }

        public int GetItemCount(string itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            return item?.Quantity ?? 0;
        }
    }
}
