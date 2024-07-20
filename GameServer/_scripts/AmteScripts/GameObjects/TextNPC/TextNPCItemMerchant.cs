using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using System.Runtime.CompilerServices;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Finance;
using DOL.GS.Quests;
using DOL.GS.Scripts;
using DOL.Territories;
using DOL.Language;

public class TextNPCItemMerchant : GameMerchant, ITextNPC, IAmteNPC
{
    public string MoneyKey
    {
        get => _moneyKey;
    }
    public TextNPCPolicy TextNPCData { get; set; }
    protected ItemTemplate m_itemTemplate = null;
    protected WorldInventoryItem m_moneyItem = null;

    public TextNPCPolicy GetTextNPCPolicy(GameLiving target = null)
    {
        return TextNPCData;
    }

    public TextNPCPolicy GetOrCreateTextNPCPolicy(GameLiving target = null)
    {
        return GetTextNPCPolicy(target);
    }

    private string _moneyKey;
    private AmteCustomParam _moneyItemParam => new AmteCustomParam("money_item",
        () => MoneyKey,
        v => _moneyKey = v,
        null
    );

    public TextNPCItemMerchant()
    {
        TextNPCData = new TextNPCPolicy(this);
        SetOwnBrain(new TextNPCBrain());
    }

    public void SayRandomPhrase()
    {
        TextNPCData.SayRandomPhrase();
    }

    public override bool Interact(GamePlayer player)
    {
        if (!TextNPCData.CanInteractWith(player) || !base.Interact(player))
            return false;

        if (TextNPCData.IsTerritoryLinked.HasValue && TextNPCData.IsTerritoryLinked.Value && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
        {
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }
        return TextNPCData.Interact(player);
    }

    public override bool WhisperReceive(GameLiving source, string str)
    {
        if (!(source is GamePlayer player) || !base.WhisperReceive(source, str))
            return false;

        if (TextNPCData.IsTerritoryLinked.HasValue && TextNPCData.IsTerritoryLinked.Value && !TerritoryManager.IsPlayerInOwnedTerritory(player, this))
        {
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "TextNPC.NotInOwnedTerritory"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            return true;
        }
        return TextNPCData.WhisperReceive(source, str);
    }

    public override bool ReceiveItem(GameLiving source, InventoryItem item)
    {
        return TextNPCData.ReceiveItem(source, item);
    }

    public override void LoadFromDatabase(DataObject obj)
    {
        base.LoadFromDatabase(obj);
        DBTextNPC textDB;
        try
        {
            textDB = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
        }
        catch
        {
            DBTextNPC.Init();
            textDB = GameServer.Database.SelectObject<DBTextNPC>(t => t.MobID == obj.ObjectId);
        }

        if (textDB != null)
            TextNPCData.LoadFromDatabase(textDB);
        DBBrainsParam[] data;
        if (!DBBrainsParam.MobXDBBrains.TryGetValue(obj.ObjectId, out data))
            data = new DBBrainsParam[0];
        _moneyItemParam.Value = data.FirstOrDefault(d => d.Param == _moneyItemParam.name)?.Value;

        if (!string.IsNullOrEmpty(MoneyKey))
        {
            //Currency.Item("SapphireSeal");
            m_itemTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>(MoneyKey);
            if (m_itemTemplate != null)
                m_moneyItem = WorldInventoryItem.CreateFromTemplate(m_itemTemplate);
        }
    }

    public override void SaveIntoDatabase()
    {
        base.SaveIntoDatabase();
        TextNPCData.SaveIntoDatabase();
        DBBrainsParam[] data;
        if (!DBBrainsParam.MobXDBBrains.TryGetValue(TextNPCData.TextDB.MobID, out data))
            data = new DBBrainsParam[0];
        var param = data.FirstOrDefault(d => d.Param == _moneyItemParam.name);
        if (param != null && param.Value != _moneyItemParam.Value)
        {
            param.Value = _moneyItemParam.Value;
            GameServer.Database.SaveObject(param);
        }
        else if (_moneyItemParam.Value != _moneyItemParam.defaultValue)
        {
            param = new DBBrainsParam
            {
                MobID = TextNPCData.TextDB.MobID,
                Param = _moneyItemParam.name,
                Value = _moneyItemParam.Value,
            };
            GameServer.Database.AddObject(param);
            if (data.Length == 0)
                DBBrainsParam.MobXDBBrains.Add(TextNPCData.TextDB.MobID, new[] { param });
            else
            {
                data = data.Concat(new[] { param }).ToArray();
                DBBrainsParam.MobXDBBrains[TextNPCData.TextDB.MobID] = data;
            }
        }
    }

    public override void DeleteFromDatabase()
    {
        base.DeleteFromDatabase();
        TextNPCData.DeleteFromDatabase();
        DBBrainsParam[] data;
        if (!DBBrainsParam.MobXDBBrains.TryGetValue(TextNPCData.TextDB.MobID, out data))
            data = new DBBrainsParam[0];
        var param = data.FirstOrDefault(d => d.Param == _moneyItemParam.name);
        if (param != null)
            GameServer.Database.DeleteObject(param);
        DBBrainsParam.MobXDBBrains.Remove(TextNPCData.TextDB.MobID);
    }

    public override eQuestIndicator GetQuestIndicator(GamePlayer player)
    {
        var result = base.GetQuestIndicator(player);
        if (result != eQuestIndicator.None)
            return result;

        foreach (var qid in QuestIdListToGive)
        {
            var quest = player.QuestList.OfType<PlayerQuest>().FirstOrDefault(pq => pq.QuestId == qid);
            if (quest == null)
                continue;
            if (quest.VisibleGoals.OfType<DataQuestJsonGoal.GenericDataQuestGoal>()
                .Any(g => g.Goal is EndGoal end && end.Target == this))
                return eQuestIndicator.Finish;
        }

        return TextNPCData.Condition.CanGiveQuest != eQuestIndicator.None && TextNPCData.Condition.CheckAccess(player)
            ? TextNPCData.Condition.CanGiveQuest
            : eQuestIndicator.None;
    }

    public AmteCustomParam GetCustomParam()
    {
        return _moneyItemParam;
    }

    public IList<string> DelveInfo()
    {
        return new List<string>
        {
            $"money_item: {m_moneyItem?.Item?.Name} (id_nb: {_moneyKey})",
        };
    }
}