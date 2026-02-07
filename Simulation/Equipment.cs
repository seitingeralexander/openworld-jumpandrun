using System.Collections.Generic;

namespace JumpAndRun.Simulation
{
    /// <summary>
    /// Equipment slot types.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon,
        Shield,
        Head,
        Body,
        Legs,
        Feet,
        Accessory1,
        Accessory2
    }

    /// <summary>
    /// Manages equipped items and their stat bonuses.
    /// </summary>
    public class Equipment
    {
        private Dictionary<EquipmentSlot, Item> _equipped = new();

        public Item GetEquipped(EquipmentSlot slot)
        {
            return _equipped.TryGetValue(slot, out var item) ? item : null;
        }

        public Item Equip(EquipmentSlot slot, Item item)
        {
            Item previous = null;
            if (_equipped.TryGetValue(slot, out var existing))
            {
                previous = existing;
            }
            _equipped[slot] = item;
            return previous; // Return unequipped item to add back to inventory
        }

        public Item Unequip(EquipmentSlot slot)
        {
            if (_equipped.TryGetValue(slot, out var item))
            {
                _equipped.Remove(slot);
                return item;
            }
            return null;
        }

        public IEnumerable<KeyValuePair<EquipmentSlot, Item>> GetAllEquipped()
        {
            return _equipped;
        }
    }
}
