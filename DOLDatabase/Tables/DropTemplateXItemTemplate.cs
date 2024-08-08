/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// 
    /// </summary>
    [DataTable(TableName = "DropTemplateXItemTemplate")]
    public class DropTemplateXItemTemplate : LootTemplate
    {
        public DropTemplateXItemTemplate()
        {
            MinLevel = 0;
            MaxLevel = 50;
            HourMin = 0;
            HourMax = 24;
            QuestID = 0;
            QuestStepID = 0;
            ActiveEventId = string.Empty;
            IsRenaissance = false;
        }

        [PrimaryKey(AutoIncrement = true)]
        public long ID { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int MinLevel { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int MaxLevel { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int HourMin { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int HourMax { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int QuestID { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public int QuestStepID { get; set; }

        [DataElement(AllowDbNull = true, Unique = false)]
        public string ActiveEventId { get; set; }

        [DataElement(AllowDbNull = false, Unique = false)]
        public bool IsRenaissance { get; set; }
    }
}
