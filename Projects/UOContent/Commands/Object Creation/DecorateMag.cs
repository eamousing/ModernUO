using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
using Server.Items;
using Server.Utilities;

namespace Server.Commands
{
  public static class DecorateMag
  {
    private static Mobile m_Mobile;
    private static int m_Count;

    public static void Initialize()
    {
      CommandSystem.Register("DecorateMag", AccessLevel.Administrator, DecorateMag_OnCommand);
    }

    [Usage("DecorateMag")]
    [Description("Generates world decoration.")]
    private static void DecorateMag_OnCommand(CommandEventArgs e)
    {
      m_Mobile = e.Mobile;
      m_Count = 0;

      m_Mobile.SendMessage("Generating world decoration, please wait.");

      Generate("Data/Decoration/RuinedMaginciaTram", Map.Trammel);
      Generate("Data/Decoration/RuinedMaginciaFel", Map.Felucca);

      m_Mobile.SendMessage("World generating complete. {0} items were generated.", m_Count);
    }

    public static void Generate(string folder, params Map[] maps)
    {
      if (!Directory.Exists(folder))
        return;

      string[] files = Directory.GetFiles(folder, "*.cfg");

      for (int i = 0; i < files.Length; ++i)
      {
        List<DecorationListMag> list = DecorationListMag.ReadAll(files[i]);

        for (int j = 0; j < list.Count; ++j)
          m_Count += list[j].Generate(maps);
      }
    }
  }

  public class DecorationListMag
  {
    private static readonly Type typeofStatic = typeof(Static);
    private static readonly Type typeofLocalizedStatic = typeof(LocalizedStatic);
    private static readonly Type typeofBaseDoor = typeof(BaseDoor);
    private static readonly Type typeofAnkhWest = typeof(AnkhWest);
    private static readonly Type typeofAnkhNorth = typeof(AnkhNorth);
    private static readonly Type typeofBeverage = typeof(BaseBeverage);
    private static readonly Type typeofLocalizedSign = typeof(LocalizedSign);
    private static readonly Type typeofMarkContainer = typeof(MarkContainer);
    private static readonly Type typeofWarningItem = typeof(WarningItem);
    private static readonly Type typeofHintItem = typeof(HintItem);
    private static readonly Type typeofCannon = typeof(Cannon);
    private static readonly Type typeofSerpentPillar = typeof(SerpentPillar);

    private static readonly Queue m_DeleteQueue = new Queue();

    private static readonly string[] m_EmptyParams = Array.Empty<string>();
    private List<DecorationEntryMag> m_Entries;
    private int m_ItemID;
    private string[] m_Params;
    private Type m_Type;

    public Item Construct()
    {
      if (m_Type == null)
        return null;

      Item item;

      try
      {
        if (m_Type == typeofStatic)
        {
          item = new Static(m_ItemID);
        }
        else if (m_Type == typeofLocalizedStatic)
        {
          int labelNumber = 0;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("LabelNumber"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
              {
                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                break;
              }
            }

          item = new LocalizedStatic(m_ItemID, labelNumber);
        }
        else if (m_Type == typeofLocalizedSign)
        {
          int labelNumber = 0;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("LabelNumber"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
              {
                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                break;
              }
            }

          item = new LocalizedSign(m_ItemID, labelNumber);
        }
        else if (m_Type == typeofAnkhWest || m_Type == typeofAnkhNorth)
        {
          bool bloodied = false;

          for (int i = 0; !bloodied && i < m_Params.Length; ++i)
            bloodied = m_Params[i] == "Bloodied";

          if (m_Type == typeofAnkhWest)
            item = new AnkhWest(bloodied);
          else
            item = new AnkhNorth(bloodied);
        }
        else if (m_Type == typeofMarkContainer)
        {
          bool bone = false;
          bool locked = false;
          Map map = Map.Malas;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i] == "Bone")
            {
              bone = true;
            }
            else if (m_Params[i] == "Locked")
            {
              locked = true;
            }
            else if (m_Params[i].StartsWith("TargetMap"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                map = Map.Parse(m_Params[i].Substring(++indexOf));
            }

          MarkContainer mc = new MarkContainer(bone, locked);

          mc.TargetMap = map;
          mc.Description = "strange location";

          item = mc;
        }
        else if (m_Type == typeofHintItem)
        {
          int range = 0;
          int messageNumber = 0;
          string messageString = null;
          int hintNumber = 0;
          string hintString = null;
          TimeSpan resetDelay = TimeSpan.Zero;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("Range"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("WarningString"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                messageString = m_Params[i].Substring(++indexOf);
            }
            else if (m_Params[i].StartsWith("WarningNumber"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("HintString"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                hintString = m_Params[i].Substring(++indexOf);
            }
            else if (m_Params[i].StartsWith("HintNumber"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                hintNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("ResetDelay"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
            }

          HintItem hi = new HintItem(m_ItemID, range, messageNumber, hintNumber);

          hi.WarningString = messageString;
          hi.HintString = hintString;
          hi.ResetDelay = resetDelay;

          item = hi;
        }
        else if (m_Type == typeofWarningItem)
        {
          int range = 0;
          int messageNumber = 0;
          string messageString = null;
          TimeSpan resetDelay = TimeSpan.Zero;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("Range"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("WarningString"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                messageString = m_Params[i].Substring(++indexOf);
            }
            else if (m_Params[i].StartsWith("WarningNumber"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("ResetDelay"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
            }

          WarningItem wi = new WarningItem(m_ItemID, range, messageNumber);

          wi.WarningString = messageString;
          wi.ResetDelay = resetDelay;

          item = wi;
        }
        else if (m_Type == typeofCannon)
        {
          CannonDirection direction = CannonDirection.North;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("CannonDirection"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                direction = (CannonDirection)Enum.Parse(typeof(CannonDirection),
                  m_Params[i].Substring(++indexOf), true);
            }

          item = new Cannon(direction);
        }
        else if (m_Type == typeofSerpentPillar)
        {
          string word = null;
          Rectangle2D destination = new Rectangle2D();

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("Word"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                word = m_Params[i].Substring(++indexOf);
            }
            else if (m_Params[i].StartsWith("DestStart"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                destination.Start = Point2D.Parse(m_Params[i].Substring(++indexOf));
            }
            else if (m_Params[i].StartsWith("DestEnd"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                destination.End = Point2D.Parse(m_Params[i].Substring(++indexOf));
            }

          item = new SerpentPillar(word, destination);
        }
        else if (m_Type.IsSubclassOf(typeofBeverage))
        {
          BeverageType content = BeverageType.Liquor;
          bool fill = false;

          for (int i = 0; !fill && i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("Content"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
              {
                content = (BeverageType)Enum.Parse(typeof(BeverageType), m_Params[i].Substring(++indexOf),
                  true);
                fill = true;
              }
            }

          if (fill)
            item = (Item)ActivatorUtil.CreateInstance(m_Type, content);
          else
            item = (Item)ActivatorUtil.CreateInstance(m_Type);
        }
        else if (m_Type.IsSubclassOf(typeofBaseDoor))
        {
          DoorFacing facing = DoorFacing.WestCW;

          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("Facing"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
              {
                facing = (DoorFacing)Enum.Parse(typeof(DoorFacing), m_Params[i].Substring(++indexOf), true);
                break;
              }
            }

          item = (Item)ActivatorUtil.CreateInstance(m_Type, facing);
        }
        else
        {
          item = (Item)ActivatorUtil.CreateInstance(m_Type);
        }
      }
      catch (Exception e)
      {
        throw new Exception($"Bad type: {m_Type}", e);
      }

      if (item is BaseAddon addon)
      {
        if (addon is MaabusCoffin coffin)
        {
          for (int i = 0; i < m_Params.Length; ++i)
            if (m_Params[i].StartsWith("SpawnLocation"))
            {
              int indexOf = m_Params[i].IndexOf('=');

              if (indexOf >= 0)
                coffin.SpawnLocation = Point3D.Parse(m_Params[i].Substring(++indexOf));
            }
        }
        else if (m_ItemID > 0)
        {
          List<AddonComponent> comps = addon.Components;

          for (int i = 0; i < comps.Count; ++i)
          {
            AddonComponent comp = comps[i];

            if (comp.Offset == Point3D.Zero)
              comp.ItemID = m_ItemID;
          }
        }
      }
      else if (item is BaseLight light)
      {
        bool unlit = false, unprotected = false;

        for (int i = 0; i < m_Params.Length; ++i)
        {
          if (!unlit && m_Params[i] == "Unlit")
            unlit = true;
          else if (!unprotected && m_Params[i] == "Unprotected")
            unprotected = true;

          if (unlit && unprotected)
            break;
        }

        if (!unlit)
          light.Ignite();
        if (!unprotected)
          light.Protected = true;

        if (m_ItemID > 0)
          light.ItemID = m_ItemID;
      }
      else if (item is Spawner sp)
      {
        sp.NextSpawn = TimeSpan.Zero;

        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("Spawn"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.AddEntry(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MinDelay"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.MinDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MaxDelay"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.MaxDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("NextSpawn"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.NextSpawn = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Count"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
            {
              sp.Count = Utility.ToInt32(m_Params[i].Substring(++indexOf));
              for (int se = 0; se < sp.Entries.Count; se++)
                sp.Entries[se].SpawnedMaxCount = sp.Count;
            }
          }
          else if (m_Params[i].StartsWith("Team"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.Team = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("HomeRange"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.HomeRange = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Running"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.Running = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Group"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              sp.Group = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
      }
      else if (item is RecallRune rune)
      {
        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("Description"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              rune.Description = m_Params[i].Substring(++indexOf);
          }
          else if (m_Params[i].StartsWith("Marked"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              rune.Marked = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("TargetMap"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              rune.TargetMap = Map.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Target"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              rune.Target = Point3D.Parse(m_Params[i].Substring(++indexOf));
          }
      }
      else if (item is SkillTeleporter st)
      {
        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("Skill"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.Skill = (SkillName)Enum.Parse(typeof(SkillName), m_Params[i].Substring(++indexOf), true);
          }
          else if (m_Params[i].StartsWith("RequiredFixedPoint"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.Required = Utility.ToInt32(m_Params[i].Substring(++indexOf)) * 0.1;
          }
          else if (m_Params[i].StartsWith("Required"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.Required = Utility.ToDouble(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MessageString"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.MessageString = m_Params[i].Substring(++indexOf);
          }
          else if (m_Params[i].StartsWith("MessageNumber"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.MessageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("PointDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MapDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Creatures"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SourceEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("DestEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SoundID"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Delay"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              st.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }

        if (m_ItemID > 0)
          st.ItemID = m_ItemID;
      }
      else if (item is KeywordTeleporter kt)
      {
        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("Substring"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.Substring = m_Params[i].Substring(++indexOf);
          }
          else if (m_Params[i].StartsWith("Keyword"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.Keyword = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Range"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.Range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("PointDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MapDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Creatures"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SourceEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("DestEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SoundID"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Delay"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              kt.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }

        if (m_ItemID > 0)
          kt.ItemID = m_ItemID;
      }
      else if (item is Teleporter tp)
      {
        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("PointDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("MapDest"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Creatures"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SourceEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("DestEffect"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("SoundID"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
          }
          else if (m_Params[i].StartsWith("Delay"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
          }

        if (m_ItemID > 0)
          tp.ItemID = m_ItemID;
      }
      else if (item is FillableContainer cont)
      {
        for (int i = 0; i < m_Params.Length; ++i)
          if (m_Params[i].StartsWith("ContentType"))
          {
            int indexOf = m_Params[i].IndexOf('=');

            if (indexOf >= 0)
              cont.ContentType = (FillableContentType)Enum.Parse(typeof(FillableContentType),
                m_Params[i].Substring(++indexOf), true);
          }

        if (m_ItemID > 0)
          cont.ItemID = m_ItemID;
      }
      else if (m_ItemID > 0)
      {
        item.ItemID = m_ItemID;
      }

      item.Movable = false;

      for (int i = 0; i < m_Params.Length; ++i)
        if (m_Params[i].StartsWith("Light"))
        {
          int indexOf = m_Params[i].IndexOf('=');

          if (indexOf >= 0)
            item.Light = (LightType)Enum.Parse(typeof(LightType), m_Params[i].Substring(++indexOf), true);
        }
        else if (m_Params[i].StartsWith("Hue"))
        {
          int indexOf = m_Params[i].IndexOf('=');

          if (indexOf >= 0)
          {
            int hue = Utility.ToInt32(m_Params[i].Substring(++indexOf));

            if (item is DyeTub tub)
              tub.DyedHue = hue;
            else
              item.Hue = hue;
          }
        }
        else if (m_Params[i].StartsWith("Name"))
        {
          int indexOf = m_Params[i].IndexOf('=');

          if (indexOf >= 0)
            item.Name = m_Params[i].Substring(++indexOf);
        }
        else if (m_Params[i].StartsWith("Amount"))
        {
          int indexOf = m_Params[i].IndexOf('=');

          if (indexOf >= 0)
          {
            // Must suppress stackable warnings

            bool wasStackable = item.Stackable;

            item.Stackable = true;
            item.Amount = Utility.ToInt32(m_Params[i].Substring(++indexOf));
            item.Stackable = wasStackable;
          }
        }

      return item;
    }

    private static bool FindItem(int x, int y, int z, Map map, Item srcItem)
    {
      int itemID = srcItem.ItemID;

      bool res = false;

      IPooledEnumerable<Item> eable;

      if (srcItem is BaseDoor)
      {
        eable = map.GetItemsInRange(new Point3D(x, y, z), 1);

        foreach (Item item in eable)
        {
          if (!(item is BaseDoor))
            continue;

          BaseDoor bd = (BaseDoor)item;
          Point3D p;
          int bdItemID;

          if (bd.Open)
          {
            p = new Point3D(bd.X - bd.Offset.X, bd.Y - bd.Offset.Y, bd.Z - bd.Offset.Z);
            bdItemID = bd.ClosedID;
          }
          else
          {
            p = bd.Location;
            bdItemID = bd.ItemID;
          }

          if (p.X != x || p.Y != y)
            continue;

          if (item.Z == z && bdItemID == itemID)
            res = true;
          else if (Math.Abs(item.Z - z) < 8)
            m_DeleteQueue.Enqueue(item);
        }
      }
      else if ((TileData.ItemTable[itemID & TileData.MaxItemValue].Flags & TileFlag.LightSource) != 0)
      {
        eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

        LightType lt = srcItem.Light;
        string srcName = srcItem.ItemData.Name;

        foreach (Item item in eable)
          if (item.Z == z)
          {
            if (item.ItemID == itemID)
            {
              if (item.Light != lt)
                m_DeleteQueue.Enqueue(item);
              else
                res = true;
            }
            else if ((item.ItemData.Flags & TileFlag.LightSource) != 0 && item.ItemData.Name == srcName)
            {
              m_DeleteQueue.Enqueue(item);
            }
          }
      }
      else if (srcItem is Teleporter || srcItem is FillableContainer || srcItem is BaseBook)
      {
        eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

        Type type = srcItem.GetType();

        foreach (Item item in eable)
          if (item.Z == z && item.ItemID == itemID)
          {
            if (item.GetType() != type)
              m_DeleteQueue.Enqueue(item);
            else
              res = true;
          }
      }
      else
      {
        eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

        if (eable.Any(item => item.Z == z && item.ItemID == itemID))
        {
          eable.Free();
          return true;
        }
      }

      eable.Free();

      while (m_DeleteQueue.Count > 0)
        ((Item)m_DeleteQueue.Dequeue()).Delete();

      return res;
    }

    public int Generate(Map[] maps)
    {
      int count = 0;

      Item item = null;

      for (int i = 0; i < m_Entries.Count; ++i)
      {
        DecorationEntryMag entry = m_Entries[i];
        Point3D loc = entry.Location;
        string extra = entry.Extra;

        for (int j = 0; j < maps.Length; ++j)
        {
          item ??= Construct();

          if (item == null)
            continue;

          if (FindItem(loc.X, loc.Y, loc.Z, maps[j], item))
          {
          }
          else
          {
            item.MoveToWorld(loc, maps[j]);
            ++count;

            if (item is BaseDoor door)
            {
              IPooledEnumerable<BaseDoor> eable = maps[j].GetItemsInRange<BaseDoor>(loc, 1);

              Type itemType = door.GetType();

              foreach (BaseDoor link in eable)
                if (link != item && link.Z == door.Z && link.GetType() == itemType)
                {
                  door.Link = link;
                  link.Link = door;
                  break;
                }

              eable.Free();
            }
            else if (item is MarkContainer markCont)
            {
              try
              {
                markCont.Target = Point3D.Parse(extra);
              }
              catch
              {
                // ignored
              }
            }

            item = null;
          }
        }
      }

      item?.Delete();

      return count;
    }

    public static List<DecorationListMag> ReadAll(string path)
    {
      using StreamReader ip = new StreamReader(path);
      List<DecorationListMag> list = new List<DecorationListMag>();

      DecorationListMag v;
      while ((v = Read(ip)) != null)
        list.Add(v);

      return list;
    }

    public static DecorationListMag Read(StreamReader ip)
    {
      string line;

      while ((line = ip.ReadLine()) != null)
      {
        line = line.Trim();

        if (line.Length > 0 && !line.StartsWith("#"))
          break;
      }

      if (string.IsNullOrEmpty(line))
        return null;

      DecorationListMag list = new DecorationListMag();

      int indexOf = line.IndexOf(' ');

      list.m_Type = AssemblyHandler.FindFirstTypeForName(line.Substring(0, indexOf++), true);

      if (list.m_Type == null)
        throw new ArgumentException($"Type not found for header: '{line}'");

      line = line.Substring(indexOf);
      indexOf = line.IndexOf('(');
      if (indexOf >= 0)
      {
        list.m_ItemID = Utility.ToInt32(line.Substring(0, indexOf - 1));

        string parms = line.Substring(++indexOf);

        if (line.EndsWith(")"))
          parms = parms.Substring(0, parms.Length - 1);

        list.m_Params = parms.Split(';');

        for (int i = 0; i < list.m_Params.Length; ++i)
          list.m_Params[i] = list.m_Params[i].Trim();
      }
      else
      {
        list.m_ItemID = Utility.ToInt32(line);
        list.m_Params = m_EmptyParams;
      }

      list.m_Entries = new List<DecorationEntryMag>();

      while ((line = ip.ReadLine()) != null)
      {
        line = line.Trim();

        if (line.Length == 0)
          break;

        if (line.StartsWith("#"))
          continue;

        list.m_Entries.Add(new DecorationEntryMag(line));
      }

      return list;
    }
  }

  public class DecorationEntryMag
  {
    public DecorationEntryMag(string line)
    {
      Pop(out string x, ref line);
      Pop(out string y, ref line);
      Pop(out string z, ref line);

      Location = new Point3D(Utility.ToInt32(x), Utility.ToInt32(y), Utility.ToInt32(z));
      Extra = line;
    }

    public Point3D Location { get; }

    public string Extra { get; }

    public void Pop(out string v, ref string line)
    {
      int space = line.IndexOf(' ');

      if (space >= 0)
      {
        v = line.Substring(0, space++);
        line = line.Substring(space);
      }
      else
      {
        v = line;
        line = "";
      }
    }
  }
}