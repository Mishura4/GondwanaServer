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

using DOL.Database;
using DOL.Database.Attributes;

/// <summary>
/// Stores the triggers with corresponding text for a mob, for example if the mob should say something when it dies.
/// </summary>
[DataTable(TableName = "MobXAmbientBehaviour", PreCache = true)]
public class MobXAmbientBehaviour : DataObject
{
    private string m_source;
    private string m_trigger;
    private ushort m_damageTypeRepeat;
    private int m_triggerTimer;
    private ushort m_nbUse;
    private ushort m_changeFlag;
    private string m_changeBrain;
    private int m_changeNPCTemplate;
    private int m_changeEffect;
    private ushort m_callAreaeffectID;
    private ushort m_playertoTPpoint;
    private ushort m_mobtoTPpoint;
    private int m_tPeffect;
    private ushort m_emote;
    private string m_text;
    private ushort m_chance;
    private string m_voice;
    private int m_spell;
    private ushort m_hp;
    private string m_responseTrigger;
    private int m_interactTimerDelay;
    private string m_walkToPath;
    private ushort m_yell;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">Mob's name</param>
    /// <param name="type">The type of trigger to act on (eAmbientTrigger)</param>
    /// <param name="text">The formatted text for the trigger. You can use [targetclass],[targetname],[sourcename]
    /// and supply formatting stuff: [b] for broadcast, [y] for yell</param>
    /// <param name="action">the desired emote</param>
    public MobXAmbientBehaviour()
    {
        m_source = string.Empty;
        m_trigger = string.Empty;
        m_emote = 0;
        m_text = string.Empty;
        m_chance = 0;
        m_voice = string.Empty;
        m_spell = 0;
        m_hp = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="trigger"></param>
    /// <param name="emote"></param>
    /// <param name="text"></param>
    /// <param name="chance"></param>
    /// <param name="voice"></param>
    /// <param name="spell"></param>
    /// <param name="hp"></param>
    /// <param name="changeBrain"></param>
    /// <param name="changeNPCTemplate"></param>
    /// <param name="callAreaeffectID"></param>
    /// <param name="playertoTPpoint"></param>
    /// <param name="mobtoTPpoint"></param>
    /// <param name="trigerTimer"></param>
    /// <param name="changeEffect"></param>
    /// <param name="tpEffect"></param>
    /// <param name="domageTypeRepeat"></param>
    /// <param name="nbUse"></param>
    /// <param name="changeFlag"></param>
    /// <param name="responseTrigger"></param>
    /// <param name="interactTimerDelay"></param>
    /// <param name="walkToPath"></param>
    /// <param name="yell"></param>
    public MobXAmbientBehaviour(string name, string trigger, ushort emote, string text, ushort chance, string voice, int spell, ushort hp, string changeBrain, int changeNPCTemplate, ushort callAreaeffectID, ushort playertoTPpoint, ushort mobtoTPpoint, int trigerTimer, int changeEffect, int tpEffect, ushort domageTypeRepeat, ushort nbUse, ushort changeFlag, string responseTrigger, int interactTimerDelay, string walkToPath, ushort yell)
    {
        m_source = name;
        m_trigger = trigger;
        m_emote = emote;
        m_text = text;
        m_chance = chance;
        m_voice = voice;
        m_spell = spell;
        m_hp = hp;
        m_changeBrain = changeBrain;
        m_changeNPCTemplate = changeNPCTemplate;
        m_callAreaeffectID = callAreaeffectID;
        m_playertoTPpoint = playertoTPpoint;
        m_mobtoTPpoint = mobtoTPpoint;
        m_triggerTimer = trigerTimer;
        m_changeEffect = changeEffect;
        m_tPeffect = tpEffect;
        m_nbUse = nbUse;
        m_damageTypeRepeat = domageTypeRepeat;
        m_changeFlag = changeFlag;
        m_responseTrigger = responseTrigger;
        m_interactTimerDelay = interactTimerDelay;
        m_walkToPath = walkToPath;
        m_yell = yell;
    }

    [DataElement(AllowDbNull = false, Index = true)]
    public string Source
    {
        get { return m_source; }
        set { m_source = value; }
    }

    [DataElement(AllowDbNull = false)]
    public string Trigger
    {
        get { return m_trigger; }
        set { m_trigger = value; }
    }

    [DataElement(AllowDbNull = false)]
    public ushort Emote
    {
        get { return m_emote; }
        set { m_emote = value; }
    }

    [DataElement(AllowDbNull = false)]
    public string Text
    {
        get { return m_text; }
        set { m_text = value; }
    }

    [DataElement(AllowDbNull = false)]
    public ushort Chance
    {
        get { return m_chance; }
        set { m_chance = value; }
    }

    [DataElement(AllowDbNull = true)]
    public string Voice
    {
        get { return m_voice; }
        set { m_voice = value; }
    }

    [DataElement(AllowDbNull = true)]
    public int Spell
    {
        get { return m_spell; }
        set { m_spell = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort HP
    {
        get { return m_hp; }
        set { m_hp = value; }
    }

    [DataElement(AllowDbNull = true)]
    public string ChangeBrain
    {
        get { return m_changeBrain; }
        set { m_changeBrain = value; }
    }

    [DataElement(AllowDbNull = true)]
    public int ChangeNPCTemplate
    {
        get { return m_changeNPCTemplate; }
        set { m_changeNPCTemplate = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort CallAreaeffectID
    {
        get { return m_callAreaeffectID; }
        set { m_callAreaeffectID = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort PlayertoTPpoint
    {
        get { return m_playertoTPpoint; }
        set { m_playertoTPpoint = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort MobtoTPpoint
    {
        get { return m_mobtoTPpoint; }
        set { m_mobtoTPpoint = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort DamageTypeRepeat
    {
        get { return m_damageTypeRepeat; }
        set { m_damageTypeRepeat = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort NbUse
    {
        get { return m_nbUse; }
        set { m_nbUse = value; }
    }

    [DataElement(AllowDbNull = true)]
    public ushort ChangeFlag
    {
        get { return m_changeFlag; }
        set { m_changeFlag = value; }
    }

    [DataElement(AllowDbNull = true)]
    public int TriggerTimer
    {
        get { return m_triggerTimer; }
        set { m_triggerTimer = value; }
    }

    [DataElement(AllowDbNull = true)]
    public int ChangeEffect
    {
        get { return m_changeEffect; }
        set { m_changeEffect = value; }
    }

    [DataElement(AllowDbNull = true)]
    public int TPeffect
    {
        get { return m_tPeffect; }
        set { m_tPeffect = value; }
    }

    [DataElement(AllowDbNull = true)]
    public string ResponseTrigger
    {
        get { return m_responseTrigger; }
        set { m_responseTrigger = value; }
    }
    [DataElement(AllowDbNull = true)]
    public int InteractTimerDelay
    {
        get { return m_interactTimerDelay; }
        set { m_interactTimerDelay = value; }
    }
    [DataElement(AllowDbNull = true)]
    public string WalkToPath
    {
        get { return m_walkToPath; }
        set { m_walkToPath = value; }
    }
    [DataElement(AllowDbNull = true)]
    public ushort Yell
    {
        get { return m_yell; }
        set { m_yell = value; }
    }
}